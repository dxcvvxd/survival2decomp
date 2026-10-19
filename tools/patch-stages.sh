#!/usr/bin/env bash
#
# Apply or revert a patch stage on the already-decompiled tree in decompiled/src,
# without re-running the decompiler. This is how the optional layers (patches/fixes)
# are toggled on top of the 1:1 base port for A/B testing:
#
#   tools/patch-stages.sh apply fixes
#   tools/patch-stages.sh revert fixes
#
# File removals (patches/<stage>/removed-files.txt) are only performed by ./compile
# on a fresh decompile; a stage applied here must only contain .patch files.
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=tools/lib.sh
. "$REPO_ROOT/tools/lib.sh"

usage() {
	awk 'NR>2 { if ($0 !~ /^#/) exit; sub(/^# ?/, ""); print }' "$0"
	exit "${1:-0}"
}

[ $# -ge 2 ] || usage 1
ACTION="$1"
shift

case "$ACTION" in
apply | revert) ;;
-h | --help) usage 0 ;;
*) die "unknown action: $ACTION (expected apply or revert)" ;;
esac

for s in "$@"; do
	dir="$(stage_dir "$s")"
	[ -d "$dir" ] || die "unknown stage: $s (available: $(list_stages | tr '\n' ' '))"
	if [ -f "$dir/removed-files.txt" ]; then
		warn "stage $s deletes files; those removals only happen during ./compile (skipped here)"
	fi
	case "$ACTION" in
	apply) apply_stage_patches "$dir" ;;
	revert) reverse_stage_patches "$dir" ;;
	esac
done
