#!/bin/sh
# Survivalcraft 2 Linux port — user-level installer.
#
#   install.sh               install (default)
#   install.sh --uninstall   remove everything: app files, launcher,
#                            desktop entry, icon, config/saves/content
#
# Environment:
#   PREFIX       install root for binary + app files
#                (default: ~/.local as user, /usr/local as root/sudo)
#   PUBLISH_DIR  dotnet publish output (default: <repo>/build/publish)
#   APP_HOME     config/data dir (default: $XDG_CONFIG_HOME/survivalcraft2
#                or ~/.config/survivalcraft2) — settings, worlds, logs,
#                texture/furniture packs, downloaded content
set -eu

MODE=install
for arg in "$@"; do
    case "$arg" in
        --uninstall) MODE=uninstall ;;
        -h|--help)   sed -n '2,13p' "$0"; exit 0 ;;
        *) echo "unknown argument: $arg" >&2; exit 2 ;;
    esac
done

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_DIR=$(dirname "$SCRIPT_DIR")

# Target user's home (sudo keeps the real user's home, not root's)
if [ -n "${SUDO_USER:-}" ] && [ "$(id -u)" = 0 ]; then
    HOME_DIR=$(getent passwd "$SUDO_USER" | cut -d: -f6)
    [ -n "$HOME_DIR" ] || { echo "cannot resolve home of $SUDO_USER" >&2; exit 1; }
else
    HOME_DIR=$HOME
fi

# Default prefix: system-wide under sudo/root, user-level otherwise
if [ -z "${PREFIX:-}" ]; then
    if [ "$(id -u)" = 0 ]; then
        PREFIX=/usr/local
    else
        PREFIX="$HOME_DIR/.local"
    fi
fi
PUBLISH_DIR=${PUBLISH_DIR:-"$REPO_DIR/build/publish"}
XDG_CONFIG_HOME=${XDG_CONFIG_HOME:-"$HOME_DIR/.config"}
APP_HOME=${APP_HOME:-"$XDG_CONFIG_HOME/survivalcraft2"}

BIN_DIR=$PREFIX/bin
LIB_DIR=$PREFIX/lib/survivalcraft2
LAUNCHER=$BIN_DIR/survivalcraft2
DESKTOP_DIR=$HOME_DIR/.local/share/applications
DESKTOP_FILE=$DESKTOP_DIR/survivalcraft2.desktop
ICON_DIR=$HOME_DIR/.local/share/icons/hicolor/144x144/apps
ICON_FILE=$ICON_DIR/survivalcraft2.png

die() { echo "error: $*" >&2; exit 1; }

uninstall() {
    FAILED=0
    # remove from every known prefix (current PREFIX, user-level, system)
    for pfx in "$PREFIX" "$HOME_DIR/.local" /usr/local; do
        { [ -e "$pfx/bin/survivalcraft2" ] || [ -e "$pfx/lib/survivalcraft2" ]; } || continue
        if rm -f "$pfx/bin/survivalcraft2" 2>/dev/null && rm -rf "$pfx/lib/survivalcraft2" 2>/dev/null; then
            echo "removed: $pfx (launcher + app files)"
        else
            echo "FAILED: cannot remove from $pfx (needs sudo)" >&2
            FAILED=1
        fi
    done
    rm -f "$DESKTOP_FILE" 2>/dev/null || { echo "FAILED: $DESKTOP_FILE (needs sudo)" >&2; FAILED=1; }
    rm -f "$ICON_FILE" 2>/dev/null || { echo "FAILED: $ICON_FILE (needs sudo)" >&2; FAILED=1; }
    rm -rf "$APP_HOME" 2>/dev/null || { echo "FAILED: $APP_HOME (needs sudo)" >&2; FAILED=1; }
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database "$DESKTOP_DIR" >/dev/null 2>&1 || true
    fi
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -t "$HOME_DIR/.local/share/icons/hicolor" >/dev/null 2>&1 || true
    fi
    echo "removed: desktop entry, icon"
    echo "removed: config/saves/content at $APP_HOME"
    [ "$FAILED" = 0 ] || exit 1
}

install_app() {
    # 1. app files (Engine.dll, Survivalcraft.dll, deps, apphost…)
    [ -f "$PUBLISH_DIR/Survivalcraft.dll" ] || die "publish output missing: $PUBLISH_DIR/Survivalcraft.dll (run: make install)"
    [ -f "$PUBLISH_DIR/Engine.dll" ] || die "publish output missing: $PUBLISH_DIR/Engine.dll"

    # single install at a time: drop a stale copy from the other default prefix
    if [ "$(id -u)" = 0 ]; then
        if [ -e "$HOME_DIR/.local/bin/survivalcraft2" ] || [ -e "$HOME_DIR/.local/lib/survivalcraft2" ]; then
            rm -f "$HOME_DIR/.local/bin/survivalcraft2"
            rm -rf "$HOME_DIR/.local/lib/survivalcraft2"
            echo "removed stale user-level copy (~/.local)"
        fi
    elif [ -e /usr/local/bin/survivalcraft2 ] || [ -e /usr/local/lib/survivalcraft2 ]; then
        if rm -f /usr/local/bin/survivalcraft2 2>/dev/null && rm -rf /usr/local/lib/survivalcraft2 2>/dev/null; then
            echo "removed stale system-wide copy (/usr/local)"
        else
            echo "note: system-wide copy exists at /usr/local (needs sudo); user-level copy added alongside" >&2
        fi
    fi

    # config dir must end up writable by the real user (sudo runs create it as root otherwise)
    if [ "$(id -u)" != 0 ] && [ -d "$APP_HOME" ] && [ "$(stat -c %u "$APP_HOME")" != "$(id -u)" ]; then
        die "$APP_HOME is owned by another user; run: sudo chown -R $(id -u) \"$APP_HOME\""
    fi

    mkdir -p "$LIB_DIR"
    rm -rf "${LIB_DIR:?}/"*
    if [ "$(id -u)" = 0 ]; then
        cp -R "$PUBLISH_DIR/." "$LIB_DIR/"
        chown root:root "$LIB_DIR" 2>/dev/null || true
    else
        cp -a "$PUBLISH_DIR/." "$LIB_DIR/"
    fi

    # 2. launcher on PATH
    mkdir -p "$BIN_DIR"
    cat > "$LAUNCHER" <<EOF
#!/bin/sh
# Survivalcraft 2 launcher (generated by deploy/install.sh)
DOTNET=\$(command -v dotnet 2>/dev/null || true)
[ -n "\$DOTNET" ] || DOTNET=\$HOME/.dotnet/dotnet
[ -x "\$DOTNET" ] || { echo "dotnet runtime not found in PATH" >&2; exit 127; }
exec "\$DOTNET" "$LIB_DIR/Survivalcraft.dll" "\$@"
EOF
    chmod 755 "$LAUNCHER"

    # 3. config/data directory (settings, worlds, logs, downloaded content)
    mkdir -p "$APP_HOME"

    # 4. icon (144x144, from APK res/drawable-xxhdpi-v4/androidicon.png)
    [ -f "$SCRIPT_DIR/survivalcraft2.png" ] || die "missing $SCRIPT_DIR/survivalcraft2.png"
    mkdir -p "$ICON_DIR"
    cp -f "$SCRIPT_DIR/survivalcraft2.png" "$ICON_FILE"

    # 5. desktop entry — user-level only, never system locations
    mkdir -p "$DESKTOP_DIR"
    cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Survivalcraft 2
GenericName=Sandbox Survival Game
Comment=Survivalcraft 2 (Linux port)
Exec=$LAUNCHER
Icon=survivalcraft2
Terminal=false
Categories=Game;AdventureGame;
Keywords=survival;craft;sandbox;game;
StartupNotify=true
EOF
    chmod 644 "$DESKTOP_FILE"
    if command -v desktop-file-validate >/dev/null 2>&1; then
        desktop-file-validate "$DESKTOP_FILE" || die "desktop entry failed validation"
    fi

    # sudo install: files inside the user's home must belong to the real user,
    # otherwise later user-level runs (and the game itself) cannot touch them
    if [ "$(id -u)" = 0 ] && [ -n "${SUDO_USER:-}" ]; then
        chown "$SUDO_USER" "$DESKTOP_FILE" "$ICON_FILE" 2>/dev/null || true
        chown -R "$SUDO_USER" "$APP_HOME" 2>/dev/null || true
    fi

    # 6. refresh desktop/icon caches (best effort)
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database "$DESKTOP_DIR" >/dev/null 2>&1 || true
    fi
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -t "$HOME_DIR/.local/share/icons/hicolor" >/dev/null 2>&1 || true
    fi

    echo "installed:"
    echo "  launcher : $LAUNCHER"
    echo "  app files: $LIB_DIR"
    echo "  config   : $APP_HOME"
    echo "  desktop  : $DESKTOP_FILE"
    echo "  icon     : $ICON_FILE"
    case ":$PATH:" in
        *":$BIN_DIR:"*) ;;
        *) echo "note: $BIN_DIR is not in your PATH; desktop entry works regardless" ;;
    esac
    echo "run: 'survivalcraft2' or the desktop shortcut"
}

if [ "$MODE" = uninstall ]; then
    uninstall
else
    install_app
fi
