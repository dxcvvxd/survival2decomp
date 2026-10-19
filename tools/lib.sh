# shellcheck shell=bash
# Shared helpers for the Survivalcraft 2 -> Linux desktop port tooling.
# Sourced by ./compile, tools/regen-patches.sh and tools/patch-stages.sh
# (all of which set REPO_ROOT first).

# ilspycmd version the checked-in patches were generated against. The decompiler
# output is byte-reproducible for a given version, which is what lets the port be
# carried as plain patch files; another version may format the C# differently.
PINNED_ILSPY_VERSION="11.0.0.9375"

# Patch stages, applied in this order. "base" is what makes the APK compile and
# run on Linux; everything else is an optional behaviour change on top.
BASE_STAGE="base"

info() { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33mwarning:\033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31merror:\033[0m %s\n' "$*" >&2; exit 1; }

require_cmd() {
	command -v "$1" >/dev/null 2>&1 || die "required command not found: $1"
}

stage_dir() { printf '%s/patches/%s' "$REPO_ROOT" "$1"; }

list_stages() {
	find "$REPO_ROOT/patches" -mindepth 1 -maxdepth 1 -type d -printf '%f\n' 2>/dev/null | sort
}

# Make sure a usable ilspycmd is on PATH, installing the pinned version if absent.
ensure_ilspycmd() {
	export PATH="$PATH:$HOME/.dotnet/tools"
	if ! command -v ilspycmd >/dev/null 2>&1; then
		require_cmd dotnet
		info "ilspycmd not found, installing $PINNED_ILSPY_VERSION as a global dotnet tool"
		dotnet tool install --global ilspycmd --version "$PINNED_ILSPY_VERSION" >/dev/null \
			|| die "failed to install ilspycmd $PINNED_ILSPY_VERSION"
	fi
	local have
	have="$(ilspycmd --version 2>/dev/null | head -1 | sed 's/^ilspycmd: *//')"
	if [ "$have" != "$PINNED_ILSPY_VERSION" ]; then
		warn "ilspycmd $have is installed but the patches were generated with $PINNED_ILSPY_VERSION"
		warn "decompiler output (and therefore patch application) may differ"
	fi
}

# extract_assemblies <apk | unpacked-apk-dir | libassemblies.so> <workdir>
# Unpacks the input and extracts the managed assemblies from the .NET Android
# assembly store into <workdir>/assemblies (plus <workdir>/apk for app resources).
extract_assemblies() {
	local input="$1" work="$2" blob=""
	require_cmd python3
	mkdir -p "$work"
	case "$input" in
	*.apk)
		require_cmd unzip
		rm -rf "$work/apk"
		info "unpacking $(basename "$input")"
		unzip -q -o "$input" 'lib/*' 'res/drawable*' -d "$work/apk" \
			|| die "could not unpack $input"
		;;
	*)
		if [ -d "$input" ]; then
			rm -rf "$work/apk"
			mkdir -p "$work/apk"
			local p
			for p in lib res; do
				[ -e "$input/$p" ] && cp -r "$input/$p" "$work/apk/"
			done
		else
			blob="$input"
		fi
		;;
	esac
	if [ -z "$blob" ]; then
		blob="$(find "$work/apk/lib" -name 'libassemblies*.blob.so' 2>/dev/null | sort | head -1)"
	fi
	[ -n "$blob" ] && [ -e "$blob" ] || die "no libassemblies*.blob.so found in input: $input"
	rm -rf "$work/assemblies"
	info "extracting managed assemblies from $(basename "$blob")"
	if ! python3 "$REPO_ROOT/tools/extract_store.py" "$blob" "$work/assemblies" \
			>"$work/extract_store.log" 2>&1; then
		tail -20 "$work/extract_store.log" >&2
		die "assembly store extraction failed (see $work/extract_store.log)"
	fi
	local n
	n="$(find "$work/assemblies" -name '*.dll' | wc -l)"
	[ "$n" -gt 0 ] || die "no assemblies were extracted"
	info "extracted $n managed assemblies"
}

# decompile_project <assemblies-dir> <Assembly.dll> <output-dir> [logdir]
# ilspycmd project mode, matching the layout of the checked-in sources.
decompile_project() {
	local assemblies="$1" dll="$2" out="$3" logdir="${4:-.}"
	[ -f "$assemblies/$dll" ] || die "assembly not found: $assemblies/$dll"
	local log="$logdir/ilspy-${dll%.dll}.log"
	rm -rf "$out"
	mkdir -p "$out" "$logdir"
	info "decompiling $dll"
	if ! ilspycmd -p -o "$out" -r "$assemblies" --disable-updatecheck "$assemblies/$dll" \
			>"$log" 2>&1; then
		tail -20 "$log" >&2
		die "decompiling $dll failed (see $log)"
	fi
	# ilspycmd project mode emits its own project file; the port uses port/*/*.csproj
	rm -f "$out"/*.csproj "$out"/*.sln
	find "$out" -name '*.cs' -print -quit | grep -q . || die "no sources decompiled from $dll"
}

# materialize_support_assemblies <assemblies-dir> <dest-dir>
# Copies the prebuilt managed assemblies the port links against out of the APK
# into the tree. There is no platform-neutral variant of either, so they are not
# part of the repository and always come from the APK being compiled.
materialize_support_assemblies() {
	local src="$1" dest="$2" f
	mkdir -p "$dest"
	for f in EntitySystem.Android.Net.dll _Microsoft.Android.Resource.Designer.dll; do
		[ -f "$src/$f" ] || die "managed assembly missing from the APK: $f"
		cp -f "$src/$f" "$dest/$f"
	done
	info "support assemblies from the APK -> ${dest#"$REPO_ROOT"/}"
}

# extract_icon <unpacked-apk-dir> <destination-png>
extract_icon() {
	local apkdir="$1" dest="$2" src="" c
	for c in res/drawable-xxhdpi-v4/androidicon.png \
	         res/drawable-xhdpi-v4/androidicon.png \
	         res/drawable/androidicon.png; do
		if [ -f "$apkdir/$c" ]; then src="$apkdir/$c"; break; fi
	done
	if [ -z "$src" ]; then
		warn "no launcher icon in APK, keeping existing ${dest#"$REPO_ROOT"/}"
		return 0
	fi
	mkdir -p "$(dirname "$dest")"
	cp "$src" "$dest"
	info "icon from $c"
}

# apply_stage_removals <stage-dir>
# Deletes the Android-coupled decompiled files a stage drops
# (patches/<stage>/removed-files.txt).
apply_stage_removals() {
	local dir="$1" line rel n=0
	[ -f "$dir/removed-files.txt" ] || return 0
	while IFS= read -r line; do
		line="${line%%#*}"
		line="$(printf '%s' "$line" | xargs)"
		[ -n "$line" ] || continue
		rel="$REPO_ROOT/decompiled/src/$line"
		if [ -e "$rel" ]; then
			rm -f "$rel"
			n=$((n + 1))
		else
			warn "removal target not found: $line"
		fi
	done <"$dir/removed-files.txt"
	info "removed $n Android-coupled source files"
}

# apply_stage_patches <stage-dir>
# Applies every patch of a stage (patch paths are relative to the repo root).
apply_stage_patches() {
	require_cmd patch
	local dir="$1" p n=0
	while IFS= read -r p; do
		info "applying ${p#"$REPO_ROOT"/}"
		if ! patch -p1 -N --batch --no-backup-if-mismatch -d "$REPO_ROOT" -i "$p" >/dev/null; then
			die "patch does not apply: ${p#"$REPO_ROOT"/} (unsupported APK version, or the stage below is missing)"
		fi
		n=$((n + 1))
	done < <(find "$dir" -name '*.patch' | sort)
	info "applied $n patch(es) from $(basename "$dir")"
}

# revert_stage_patches <stage-dir>
reverse_stage_patches() {
	require_cmd patch
	local dir="$1" p n=0
	while IFS= read -r p; do
		info "reverting ${p#"$REPO_ROOT"/}"
		if ! patch -p1 -R -N --batch --no-backup-if-mismatch -d "$REPO_ROOT" -i "$p" >/dev/null; then
			die "cannot revert: ${p#"$REPO_ROOT"/} (tree does not have this stage applied?)"
		fi
		n=$((n + 1))
	done < <(find "$dir" -name '*.patch' | sort -r)
	info "reverted $n patch(es) from $(basename "$dir")"
}
