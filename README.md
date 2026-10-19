# Survivalcraft 2 — Linux desktop port

The Android release of **Survivalcraft 2** (v2.4.10.8, a .NET/Xamarin app) decompiled
and rebuilt as a native Linux desktop game. The game logic is the original compiled
code, decompiled back to C#; the Android platform layer (window, input, storage,
audio device handling) is replaced by Linux implementations. The result is built with
the .NET SDK and runs on GLFW + OpenGL ES + OpenAL.

```
install.sh                ./install.sh <apk> — APK -> built, installed, build/ dropped
compile                   APK -> decompiled + patched + buildable tree (one command)
Makefile                  make / make install / make uninstall / make clean / make distclean
Directory.Build.props     sends every dotnet obj/bin into build/
port/
  Engine.Linux/           project that builds Engine.dll
  Survivalcraft.Linux/    project that builds the game executable
  shims/                  hand-written Linux replacements for the Android layer
  lib/                    APK-extracted managed assemblies (generated, not tracked)
patches/                  the port itself, as patch stages (see patches/README.md)
tools/                    assembly-store extractor, patch regeneration, stage toggles
install/                  install.sh machinery for make install (+ APK icon)
build/                    the one scratch directory (gitignored, generated)
```

Nothing from the game is stored here: `compile` pulls the managed assemblies out
of the APK you pass, decompiles them, applies `patches/` and builds. `decompiled/`,
`port/lib/`, `install/survivalcraft2.png` and `build/` are generated and ignored.

`build/` holds every build artifact of a run — the unpacked APK, the extracted
assemblies, decompiler/extractor logs, `obj/`, `bin/`, `publish/` and `tmp/`
(TMPDIR for the whole build, so nothing is written to `/tmp`). `make clean` is a
single `rm -rf build`.

## Building and installing

```sh
./install.sh <apk>        # APK -> compiled, built, installed, build/ removed (do this first)
make                     # rebuild (dotnet build) from the already-existing tree
sudo make install        # publish + install binary, app files, desktop shortcut, icon
sudo make uninstall      # remove everything that was installed
make clean               # drop the build directory (all build artifacts)
make distclean           # also drop everything generated from the APK (patcher-only tree)
```

`./install.sh` is the one-shot: it runs `./compile <apk>` (decompile + patch +
build) and `make install`, then deletes `build/`. It elevates through `$SUDO`
(default `sudo`) so the app lands in `/usr/local`; with `SUDO=` (or no sudo) it
installs user-level into `~/.local`. The APK is only read — it stays wherever you
put it, and `decompiled/` + `port/lib` stay in the tree so `make` keeps working
(`make distclean` drops them too). Nothing is deleted if a step fails, so `build/`
survives for inspection.

`make` alone needs a tree from a previous `./install.sh <apk>` or `./compile <apk>`
(it says so if there is none).

`make install` does the whole job itself: it publishes, copies the app to
`$PREFIX/lib/survivalcraft2`, writes the launcher to `$PREFIX/bin/survivalcraft2`,
installs the desktop entry + icon into the *invoking user's* home and hands
root-created files back to that user.

| install as | binary / app files | config, saves, desktop entry, icon |
| --- | --- | --- |
| normal user | `~/.local/bin`, `~/.local/lib/survivalcraft2` | `~/.config/survivalcraft2`, `~/.local/share/...` |
| `sudo` | `/usr/local/bin`, `/usr/local/lib/survivalcraft2` | still the invoking user's home |

Game data (worlds, texture packs, settings, logs) lives in
`$SURVIVALCRAFT2_HOME`, defaulting to `~/.config/survivalcraft2`. On first run the
Android-era data directories are migrated there if they were left in the tree.

### Requirements

* build: .NET SDK 10, `make`, `patch`, `python3`, `unzip`
* only for re-deriving from an APK: `ilspycmd` 11.0.0.9375 (`./compile` installs it
  as a global dotnet tool if missing)
* runtime: `libglfw.so.3`, `libGLESv2.so.2` (Mesa), `libopenal.so.1`, and the
  unversioned `libglfw.so` / `libGLESv2.so` / `libopenal.so` symlinks that come
  with the `-dev` packages (`libglfw3-dev`, `libgles-dev`/`libgl1-mesa-dev`,
  `libopenal-dev`) because the engine imports them by those names

## Re-deriving from the original APK

```sh
./compile /path/to/Survivalcraft-2-...-full-apkvision.apk     # 1:1 port, then build
./compile <apk> --fixes                                      # ... plus the behaviour fixes
./compile <apk> --fixes --install                            # ... and install it
make from-apk APK=<apk> [ARGS=--fixes]                       # same, via make
```

`./compile` unpacks the APK, extracts the managed assemblies from the .NET Android
assembly store (`libassemblies.<abi>.blob.so`, `XABA`/`XALZ` + LZ4), decompiles
`Engine.dll` and `Survivalcraft.dll` with ilspycmd, applies the patch stages, takes
the launcher icon from the APK and builds. The decompiler output is
byte-reproducible for the pinned ilspycmd version, so the port can be carried as
plain patch files and a rerun reproduces the tree exactly (`git status` stays
clean).

## The port in layers

**Platform layer (always).** `port/shims/**` replaces the deleted Android files:
	a GLFW window and game loop, mouse/keyboard/gamepad input, application storage
	(`SURVIVALCRAFT2_HOME`), a no-op touch layer, and no-op/stub device, screen
	resolution, screen capture and web browser managers. GL calls and OpenAL are the
	engine's own, resolved against the system libraries.

**`patches/base` — what the APK needs to compile and run on Linux.** Deletes the 16
	Android-coupled sources and patches 4 files, without changing how the game plays:

	| patch | why |
	| --- | --- |
	| `Engine.Content/ContentCache.cs` | type scan must survive assemblies with unresolvable (Android-era) references, otherwise startup dies in `ModelWidget` |
	| `Engine.Serialization/HumanReadableConverter.cs` | same, for the converter scan |
	| `Game/ExternalContentManager.cs` | stops registering the deleted Android SD-card provider |
	| `Game/WebManager.cs` | drops `Android.Net`; reports "online" instead of asking `ConnectivityManager` |

**`patches/fixes` — optional desktop behaviour, on top of the 1:1 port.** Not part of
	`./compile` unless `--fixes` is given, and toggled in an existing tree with
	`make fixes` / `make unfixes`:

	| patch | why |
	| --- | --- |
	| `Game/ComponentInput.cs` | upstream defaults `IsControlledByTouch = true` until the first touch; with no touch hardware that means the mobile move/look pads stay on screen and the crosshair is suppressed until the mouse moves. Default to mouse/keyboard instead |
	| `Game/ComponentAimingSights.cs` | the aiming "sights" sprite is placed from an aim ray captured before the camera applies the frame's rotation, so it wobbles while turning; keep the plain crosshair while aiming and leave the sights to touch input |
	| `Game/SettingsManager.cs` | upstream cannot save/load enum settings (`HumanReadableConverter` has no enum converter), so `CommunityContentMode`, `ScreenLayout1..4` were dropped; parse/format enums locally rather than changing the archive format |

See `patches/README.md` for how patches are written and regenerated.

## Maintaining the port

```sh
# after editing decompiled/src (or port/shims) - refresh the patch files
tools/regen-patches.sh <apk>              # all stages
tools/regen-patches.sh --stage fixes <apk>

# toggle the optional layers in the existing tree
tools/patch-stages.sh apply fixes
tools/patch-stages.sh revert fixes
```

A file may be patched by exactly one stage; `patches/<stage>/patched-files.txt`
declares which files a stage owns and `patches/<stage>/removed-files.txt` which
files it deletes. `regen-patches.sh` reports changes that no stage claims, files
missing from the tree but not declared as removals, and files in the tree that the
APK does not contain.

## Notes and known gaps

* `port/lib` and `install/survivalcraft2.png` are produced by `./compile`: the two
  managed assemblies the game links against (`EntitySystem.Android.Net.dll` and
  `_Microsoft.Android.Resource.Designer.dll`, the latter replacing the deleted
  `Resource.cs`) plus the launcher icon are taken straight out of the APK, because
  no platform-neutral variant exists and they are the game's own binaries.
* Touch input is never synthesized on desktop, so the 1:1 build behaves like an
  Android device with a mouse: mobile pads appear until the mouse is moved.
* `ScreenCaptureManager` (screenshot key) is a stub; `WebBrowserManager` opens no
  browser; `DeviceManager` (vibration, status bar) is a no-op.
* `WebManager.IsInternetConnectionAvailable()` returns `true` without checking.
* Logs: `~/.config/survivalcraft2/Logs/Game.log`.
