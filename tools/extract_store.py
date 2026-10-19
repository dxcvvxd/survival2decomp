#!/usr/bin/env python3
"""Extract managed assemblies (+embedded PDBs) from a .NET Android assembly store
(libassemblies.<rid>.blob.so), store format v2 ('XABA' magic).

Format (from dotnet/android src/native/clr/include/xamarin-app.hh):
  HEADER   : magic u32, version u32, entry_count u32, index_entry_count u32, index_size u32
  INDEX    : index_entry_count * 12 {name_hash u32, ..., descriptor_index u32}
  DESCRIPT : entry_count * 28 {mapping, data_off, data_size, debug_off, debug_size, cfg_off, cfg_size}
  NAMES    : entry_count * {len u32, utf8}
  DATA     : assemblies, optionally prefixed with CompressedAssemblyHeader
             {magic u32 'XAZS', descriptor_index u32, uncompressed_length u32} + zlib/deflate/zstd

Falls back to probing when layout guesses fail; prints diagnostics.
"""
import json
import struct
import sys
import zlib
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from lz4block import lz4_decompress

XABA = 0x41424158
XAZS = 0x535A4158  # newer header: {magic, desc_index, uncompressed_length} + zstd/zlib
XALZ = 0x5A4C4158  # classic header: {magic, desc_index} + zlib (deflate w/ zlib wrapper)


def crc32(data: bytes) -> int:
    return zlib.crc32(data) & 0xFFFFFFFF


def try_decompress(body: bytes, uncompressed_length: int) -> bytes | None:
    """Try zlib/deflate/zstd in order; return decompressed bytes on success."""
    candidates = []
    # zlib wrapper
    try:
        candidates.append(zlib.decompress(body))
    except Exception:
        pass
    # raw deflate
    try:
        candidates.append(zlib.decompress(body, -15))
    except Exception:
        pass
    # zstd (py3.14 compression.zstd, else optional zstandard)
    try:
        from compression import zstd  # py3.14 stdlib
        candidates.append(zstd.decompress(body))
    except Exception:
        pass
    try:
        import zstandard  # optional
        candidates.append(zstandard.ZstdDecompressor().decompress(body))
    except Exception:
        pass
    for c in candidates:
        if len(c) == uncompressed_length:
            return c
    # accept any candidate that at least looks like a PE even on length mismatch
    for c in candidates:
        if c[:2] == b"MZ":
            return c
    return None


def looks_like_pe(b: bytes) -> bool:
    return len(b) > 0x40 and b[:2] == b"MZ" and b[0x3C:0x40] != b"\x00\x00\x00\x00"


def looks_like_pdb(b: bytes) -> bool:
    return b[:2] == b"BS" or b[:4] == b"BSJB"


def main() -> int:
    blob_path = Path(sys.argv[1] if len(sys.argv) > 1 else
                     "extracted/lib/arm64-v8a/libassemblies.arm64-v8a.blob.so")
    out_dir = Path(sys.argv[2] if len(sys.argv) > 2 else "decompiled/assemblies")
    out_dir.mkdir(parents=True, exist_ok=True)

    raw = blob_path.read_bytes()
    # payload lives in the ELF 'payload' section at file offset 0x4000 (verified via readelf)
    # locate by magic instead of hardcoding when possible
    poff = raw.find(struct.pack("<I", XABA))
    if poff < 0:
        print("FATAL: XABA magic not found")
        return 1
    # ELF section 'payload' offset (readelf): prefer exact section start if magic at 0x4000
    if raw[0x4000:0x4004] == struct.pack("<I", XABA):
        poff = 0x4000
    payload = raw[poff:]
    print(f"payload at file offset 0x{poff:x}, size {len(payload)}")

    magic, version, entry_count, index_entry_count, index_size = struct.unpack_from("<5I", payload, 0)
    print(f"magic=0x{magic:08X} version=0x{version:08X} entries={entry_count} "
          f"index_entries={index_entry_count} index_size={index_size}")

    index_start = 20
    # stride must satisfy index_entry_count * stride == index_size
    stride = index_size // index_entry_count if index_entry_count else 0
    print(f"index stride = {stride} (exact: {index_entry_count * stride == index_size})")

    index = []
    for i in range(index_entry_count):
        off = index_start + i * stride
        name_hash, desc = payload[off], None  # placeholder to keep structure clear
        rec = struct.unpack_from("<2I", payload, off)
        index.append((rec[0], rec[1]))

    desc_start = index_start + index_size
    # descriptor size candidates: 28 (7 u32, v3 layout) — probe alternatives if needed
    desc_size = 28
    names_start = desc_start + entry_count * desc_size

    # --- parse names, with fallback probing ---
    def parse_names(start):
        names = []
        pos = start
        for _ in range(entry_count):
            if pos + 4 > len(payload):
                return None
            (ln,) = struct.unpack_from("<I", payload, pos)
            pos += 4
            if ln == 0 or ln > 4096 or pos + ln > len(payload):
                return None
            try:
                names.append(payload[pos:pos + ln].decode("utf-8"))
            except UnicodeDecodeError:
                return None
            pos += ln
        return names, pos

    parsed = parse_names(names_start)
    if parsed is None:
        # probe descriptor sizes until names parse
        for cand in (24, 32, 36, 20, 16):
            cand_names_start = desc_start + entry_count * cand
            p2 = parse_names(cand_names_start)
            if p2 is not None:
                desc_size, parsed = cand, p2
                break
    if parsed is None:
        print("FATAL: names table not found at expected locations")
        return 1
    names, data_start = parsed
    print(f"desc_size={desc_size} names_start=0x{names_start:x} data_start=0x{data_start:x}")
    print(f"first names: {names[:5]} ... {names[-3:]}")

    # --- validate index against crc32 of known names ---
    by_hash = {}
    for i, (h, d) in enumerate(index):
        by_hash.setdefault(h, []).append(d)
    ok = fail = 0
    for n in names[:20]:
        h = crc32(n.encode())
        if h in by_hash:
            ok += 1
        else:
            fail += 1
    print(f"crc32 index validation on first 20 names: ok={ok} miss={fail}")

    # --- descriptors ---
    manifest = []
    for i in range(entry_count):
        off = desc_start + i * desc_size
        mapping, d_off, d_size, p_off, p_size, c_off, c_size = struct.unpack_from("<7I", payload, off)
        name = names[i]
        data = payload[d_off:d_off + d_size]
        comp = "raw"
        if len(data) >= 8:
            dmagic = struct.unpack_from("<I", data, 0)[0]
            if dmagic == XALZ and len(data) >= 12:
                # header: {magic, descriptor_index, uncompressed_length} + LZ4 block @12
                cmagic, cidx, uncomp_len = struct.unpack_from("<3I", data, 0)
                try:
                    out = lz4_decompress(data[12:], max_out=uncomp_len + 64)
                    if len(out) == uncomp_len:
                        data = out
                        comp = f"XALZ(lz4,cidx={cidx},uncomp={uncomp_len})"
                    else:
                        manifest.append({"name": name, "status": "lz4-length-mismatch",
                                         "got": len(out), "want": uncomp_len})
                        continue
                except Exception as e:
                    manifest.append({"name": name, "status": "lz4-failed", "err": str(e)})
                    continue
            elif dmagic == XAZS and len(data) >= 12:
                cmagic, cidx, uncomp_len = struct.unpack_from("<3I", data, 0)
                out = try_decompress(data[12:], uncomp_len)
                if out is None:
                    manifest.append({"name": name, "status": "decompress-failed",
                                     "magic": hex(cmagic), "cidx": cidx, "uncomp_len": uncomp_len})
                    continue
                comp = f"XAZS(cidx={cidx},uncomp={uncomp_len},got={len(out)})"
                data = out
        # save
        if looks_like_pe(data):
            (out_dir / name).write_bytes(data)
        else:
            ext = ".bin"
            (out_dir / (name + ext)).write_bytes(data)
            comp += " [NOT-PE]"
        entry = {"name": name, "size": d_size, "offset": d_off, "compression": comp,
                 "pe": looks_like_pe(data)}
        # embedded portable PDB
        if p_size > 0:
            pdb = payload[p_off:p_off + p_size]
            if len(pdb) >= 12 and struct.unpack_from("<I", pdb, 0)[0] == XAZS:
                cmagic, cidx, uncomp_len = struct.unpack_from("<3I", pdb, 0)
                dec = try_decompress(pdb[12:], uncomp_len)
                if dec is not None:
                    pdb = dec
            pdb_name = name.rsplit(".", 1)[0] + ".pdb"
            (out_dir / pdb_name).write_bytes(pdb)
            entry["pdb"] = p_size
        manifest.append(entry)

    failed = [m for m in manifest if m.get("status")]
    pe_count = sum(1 for m in manifest if m.get("pe"))
    print(f"extracted {pe_count}/{entry_count} assemblies as PE; failures={len(failed)}")
    for m in failed[:10]:
        print("  FAIL:", m)
    (out_dir / "store-manifest.json").write_text(json.dumps(manifest, indent=2))
    print(f"wrote {out_dir}/store-manifest.json")
    return 0 if pe_count else 1


if __name__ == "__main__":
    sys.exit(main())
