#!/usr/bin/env bash
#
# Maintainer tool: (re)generate patches/<stage>/*.patch by diffing a pristine
# decompile of the APK against the current decompiled/ tree.
#
#   tools/regen-patches.sh <survivalcraft.apk>              # all stages
#   tools/regen-patches.sh --stage base <apk>               # one stage
#   tools/regen-patches.sh --pristine-dir build/pristine/dec <anything>
#
# Which files belong to a stage is declared in patches/<stage>/patched-files.txt;
# a file may be patched by exactly one stage. Run this after editing anything
# under decompiled/src, then commit the refreshed patches (an existing patch's
# comment header is preserved). It also reports, for the maintainer:
#   - changes in the tree that belong to no stage (add them to a manifest)
#   - files missing from the tree but present in the APK (patches/<stage>/removed-files.txt)
#   - files in the tree that the APK does not contain (should not happen)
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=tools/lib.sh
. "$REPO_ROOT/tools/lib.sh"

WORK="$REPO_ROOT/build/pristine"
PRISTINE=""
INPUT=""
STAGES=""

while [ $# -gt 0 ]; do
	case "$1" in
	--stage)
		[ -n "${2:-}" ] || die "--stage needs a stage name"
		STAGES="${STAGES:+$STAGES }$2"
		shift
		;;
	--pristine-dir)
		[ -n "${2:-}" ] || die "--pristine-dir needs a directory"
		PRISTINE="$(readlink -f "$2")"
		shift
		;;
	-h | --help)
		awk 'NR>2 { if ($0 !~ /^#/) exit; sub(/^# ?/, ""); print }' "$0"
		exit 0
		;;
	-*) die "unknown option: $1" ;;
	*)
		[ -z "$INPUT" ] || die "only one input path is supported"
		INPUT="$1"
		;;
	esac
	shift
done

if [ -z "$PRISTINE" ]; then
	[ -n "$INPUT" ] || die "usage: tools/regen-patches.sh [--stage NAME] <apk>"
	ensure_ilspycmd
	extract_assemblies "$(readlink -f "$INPUT")" "$WORK"
	for proj in Engine Survivalcraft; do
		decompile_project "$WORK/assemblies" "$proj.dll" "$WORK/dec/$proj" "$WORK"
	done
	PRISTINE="$WORK/dec"
fi
[ -n "$STAGES" ] || STAGES="$(list_stages)"

# --- sanity: a file may only be claimed by one stage ------------------------------
for stage in $STAGES; do
	[ -f "$(stage_dir "$stage")/patched-files.txt" ] \
		|| warn "stage $stage has no patched-files.txt"
done
dupes="$(cat "$REPO_ROOT"/patches/*/patched-files.txt 2>/dev/null | sed 's/#.*//' | xargs -n1 2>/dev/null \
	| grep -v '^$' | sort | uniq -d)"
[ -z "$dupes" ] || die "these files are claimed by more than one stage:$(printf ' %s' $dupes)"

# --- regenerate the patches of each stage -----------------------------------------
for stage in $STAGES; do
	manifest="$(stage_dir "$stage")/patched-files.txt"
	[ -f "$manifest" ] || continue
	while IFS= read -r rel; do
		rel="${rel%%#*}"
		rel="$(printf '%s' "$rel" | xargs)"
		[ -n "$rel" ] || continue
		proj="${rel%%/*}"
		src="$PRISTINE/$rel"
		tre="$REPO_ROOT/decompiled/src/$rel"
		patchfile="$(stage_dir "$stage")/$rel.patch"
		[ -f "$src" ] || { warn "stage $stage: not in the APK decompile: $rel"; continue; }
		[ -f "$tre" ] || { warn "stage $stage: missing from the tree: $rel"; continue; }
		if cmp -s "$src" "$tre"; then
			warn "stage $stage: $rel matches the APK source, leaving its patch untouched"
			continue
		fi
		mkdir -p "$(dirname "$patchfile")"
		header=""
		[ -f "$patchfile" ] && header="$(awk '/^--- /{exit} /^#/{print}' "$patchfile")"
		{
			[ -n "$header" ] && printf '%s\n' "$header"
			diff -u --label "a/decompiled/src/$rel" --label "b/decompiled/src/$rel" "$src" "$tre" || true
		} >"$patchfile"
		info "patched: patches/$stage/$rel.patch"
	done <"$manifest"
done

# --- report anything the manifests do not account for -----------------------------
claimed=" $(cat "$REPO_ROOT"/patches/*/patched-files.txt 2>/dev/null | sed 's/#.*//' | xargs -n1 2>/dev/null | grep -v '^$' | tr '\n' ' ') "
removed=" $(cat "$REPO_ROOT"/patches/*/removed-files.txt 2>/dev/null | sed 's/#.*//' | xargs -n1 2>/dev/null | grep -v '^$' | tr '\n' ' ') "
unmapped="" gone="" extra=""
for proj in Engine Survivalcraft; do
	pr="$PRISTINE/$proj"
	tre="$REPO_ROOT/decompiled/src/$proj"
	[ -d "$pr" ] || die "pristine decompile of $proj not found: $pr"
	while IFS= read -r rel; do
		rel="$proj/$rel"
		if [ ! -f "$REPO_ROOT/decompiled/src/$rel" ]; then
			case "$removed" in *" $rel "*) ;; *) gone="$gone $rel" ;; esac
		elif ! cmp -s "$pr/${rel#"$proj"/}" "$REPO_ROOT/decompiled/src/$rel"; then
			case "$claimed" in *" $rel "*) ;; *) unmapped="$unmapped $rel" ;; esac
		fi
	done < <(cd "$pr" && find . -type f ! -name '*.csproj' ! -name '*.sln' -printf '%P\n' | sort)
	while IFS= read -r rel; do
		[ -f "$pr/$rel" ] || extra="$extra $proj/$rel"
	done < <(cd "$tre" && find . -type f -printf '%P\n' | sort)
done

[ -n "$unmapped" ] && warn "changes not claimed by any stage (add to a patched-files.txt, then rerun):$unmapped"
[ -n "$gone" ] && warn "missing from the tree but not listed in a removed-files.txt:$gone"
[ -n "$extra" ] && warn "in the tree but not in the APK decompile:$extra"
info "done"
