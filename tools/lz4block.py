#!/usr/bin/env python3
"""Minimal LZ4 block-format decoder (enough for assembly-store XALZ payloads)."""
import struct


def lz4_decompress(src: bytes, max_out: int = 1 << 30) -> bytes:
    out = bytearray()
    i = 0
    n = len(src)
    while i < n:
        token = src[i]
        i += 1
        # literals
        lit = token >> 4
        if lit == 15:
            while True:
                if i >= n:
                    raise ValueError("truncated literal length")
                b = src[i]
                i += 1
                lit += b
                if b != 255:
                    break
        if i + lit > n:
            raise ValueError("literal run overruns input")
        out += src[i:i + lit]
        i += lit
        if i >= n:
            break  # last sequence has no match
        if i + 2 > n:
            raise ValueError("truncated offset")
        off = src[i] | (src[i + 1] << 8)
        i += 2
        if off == 0 or off > len(out):
            raise ValueError(f"bad offset {off} (out={len(out)})")
        mlen = (token & 15) + 4
        if (token & 15) == 15:
            while True:
                if i >= n:
                    raise ValueError("truncated match length")
                b = src[i]
                i += 1
                mlen += b
                if b != 255:
                    break
        if len(out) + mlen > max_out:
            raise ValueError("output overrun")
        # copy with overlap
        pos = len(out)
        for k in range(mlen):
            out.append(out[pos + k - off])
    return bytes(out)


if __name__ == "__main__":
    import sys
    data = open(sys.argv[1], "rb").read()
    start = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    expect = int(sys.argv[3]) if len(sys.argv) > 3 else None
    try:
        out = lz4_decompress(data[start:], max_out=(expect or 1 << 30) + 64)
        print(f"OK: {len(out)} bytes (expected {expect})")
        print(out[:16].hex())
        open(sys.argv[1] + ".lz4out", "wb").write(out)
    except Exception as e:
        print(f"FAIL: {e}")
