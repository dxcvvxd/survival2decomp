# Survivalcraft 2 — Linux port build & install
#
# Cycle:
#   make clean            wipe build outputs
#   make                  build (same as: make build)
#   (sudo) make install   publish + place binary + desktop shortcut (all of it)
#   (sudo) make uninstall remove everything installed
#
# install without sudo → everything under ~/.local + ~/.config/survivalcraft2
# install with sudo     → binary/app under /usr/local, desktop entry + config
#                         still in the invoking user's home (SUDO_USER)
#
# Installed paths:
#   launcher   $PREFIX/bin/survivalcraft2        (PREFIX=~/.local or /usr/local)
#   app files  $PREFIX/lib/survivalcraft2
#   config     ~/.config/survivalcraft2          (settings, saves, downloads)
#   desktop    ~/.local/share/applications/survivalcraft2.desktop  (user only)
#   icon       ~/.local/share/icons/hicolor/144x144/apps/survivalcraft2.png

CONFIG        ?= Release
PUBLISH_DIR   ?= publish
PROJECT       := port/Survivalcraft.Linux/Survivalcraft.Linux.csproj
INSTALL_SH    := install/install.sh

.PHONY: build install uninstall clean

build:
	dotnet build $(PROJECT) -c $(CONFIG) --nologo

install:
	dotnet publish $(PROJECT) -c $(CONFIG) -o $(PUBLISH_DIR) --nologo
	@if [ "`id -u`" = 0 ] && [ -n "$$SUDO_USER" ]; then \
		chown -R "$$SUDO_USER" "$(PUBLISH_DIR)" \
			port/Engine.Linux/bin port/Engine.Linux/obj \
			port/Survivalcraft.Linux/bin port/Survivalcraft.Linux/obj; \
	fi
	PUBLISH_DIR="$(abspath $(PUBLISH_DIR))" $(INSTALL_SH)

uninstall:
	PUBLISH_DIR="$(abspath $(PUBLISH_DIR))" $(INSTALL_SH) --uninstall

clean:
	rm -rf $(PUBLISH_DIR) \
		port/Engine.Linux/bin port/Engine.Linux/obj \
		port/Survivalcraft.Linux/bin port/Survivalcraft.Linux/obj
