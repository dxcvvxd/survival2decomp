#!/usr/bin/env bash
#
# Survivalcraft 2 - one-shot installer: original Android APK -> installed Linux
# desktop app, one command, one argument.
#
#   ./install.sh <survivalcraft.apk>            1:1 port (game code = the APK's)
#   ./install.sh <apk> --fixes                  ... plus the desktop behaviour fixes
#   ./install.sh <apk> --stage <name>           ... plus an extra patch stage
#   ./install.sh <apk> --keep-build             keep build/ afterwards (debugging)
#
# Steps (./compile.sh validates the input: .apk, an unpacked APK directory, or
# libassemblies.<abi>.blob.so):
#
#   1. ./compile.sh <apk>   unpack + extract assemblies + decompile + apply the
#                        patch stages + build
#   2. make install      publish + launcher + app files + desktop entry + icon +
#                        config dir - system-wide through $SUDO (default: sudo),
#                        user-level (~/.local) when there is no sudo or SUDO=
#   3. rm -rf build/     the scratch directory is gone: unpacked APK, extracted
#                        assemblies, decompiler logs, obj/, bin/, publish/, tmp
#                        (TMPDIR of the whole run was build/tmp)
#
# build/ is created inside the repository and holds every build artifact of the
# run; it is removed once the installation succeeded. On a failure nothing is
# deleted (build/ is kept for inspection). The APK is only read from wherever it
# sits - it is never copied or moved - and decompiled/ + port/lib stay in the
# tree, so `make` and `make install` keep working afterwards (`make distclean`
# drops those too).
#
# Environment: SUDO=<cmd> changes the elevation command, SUDO= disables it.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=tools/lib.sh
. "$REPO_ROOT/tools/lib.sh"

KEEP_BUILD=0
ARGS=()

usage() {
	awk 'NR>2 { if ($0 !~ /^#/) exit; sub(/^# ?/, ""); print }' "${BASH_SOURCE[0]}"
	exit "${1:-0}"
}

while [ $# -gt 0 ]; do
	case "$1" in
	-k | --keep-build) KEEP_BUILD=1 ;;
	-h | --help) usage 0 ;;
	*) ARGS+=("$1") ;; # ./compile.sh arguments: APK path, --fixes, --stage NAME
	esac
	shift
done

[ "${#ARGS[@]}" -gt 0 ] || usage 1
[ -e "${ARGS[0]}" ] || die "input not found: ${ARGS[0]}"

info "1/3: decompile + patch + build"
"$REPO_ROOT/compile.sh" "${ARGS[@]}"

info "2/3: install"
if [ "$(id -u)" = 0 ]; then
	make -C "$REPO_ROOT" install
else
	sudo_cmd="${SUDO-sudo}"
	if [ -n "$sudo_cmd" ] && command -v "$sudo_cmd" >/dev/null 2>&1; then
		"$sudo_cmd" make -C "$REPO_ROOT" install
	else
		warn "no sudo available - installing user-level into ~/.local"
		make -C "$REPO_ROOT" install
	fi
fi

if [ "$KEEP_BUILD" = 1 ]; then
	info "3/3: keeping build/ (--keep-build)"
else
	info "3/3: removing the build directory"
	rm -rf "$REPO_ROOT/build" "$REPO_ROOT/publish"
	rm -rf "$REPO_ROOT"/port/*/bin "$REPO_ROOT"/port/*/obj
fi

printf '\n'
info "installed - run: survivalcraft2 (or the desktop shortcut)"
info "the APK was not touched; decompiled/ and port/lib stay (make distclean drops them)"
