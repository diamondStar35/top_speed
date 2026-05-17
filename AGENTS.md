# Repository Guidelines

Top Speed is an audio-only racing game written in C# (.NET 10). The repo hosts a desktop/mobile client, a dedicated multiplayer server, shared protocol/data libraries, audio bindings, a self-updater, and a unified test project. This document is the contract for agents working in this repository: keep it in sync with the actual code.

## Project Structure & Module Organization

The .NET solution lives at [`top_speed_net/TopSpeed.sln`](top_speed_net/TopSpeed.sln). All paths below are relative to the repository root.

### Solution projects

- `top_speed_net/TopSpeed/` — client game. Conditional `TargetFramework`: `net10.0-windows` on Windows, `net10.0` on Linux/macOS, `Library` for mobile host builds (`AndroidHostBuild=true` / `IOSHostBuild=true`).
- `top_speed_net/TopSpeed.Server/` — dedicated server, `net10.0`. Publish profiles are selected via `-p:ServerPublishProfile=...` (see [Build, test & publish](#build-test--publish)).
- `top_speed_net/TopSpeed.Shared/` — shared protocol, localization, vehicle/track data, physics, runtime helpers. Targets `netstandard2.0` so it can be referenced from every client/server/mobile target without conditional compilation.
- `top_speed_net/TopSpeed.Tests/` — unified xUnit test project (`net10.0-windows`) covering shared, server, and client logic. Uses xUnit + FluentAssertions + FsCheck + Verify. Tests are part of the solution and must build clean.
- `top_speed_net/TopSpeed.Android/` — Android host (`net10.0-android`). Builds via [`top_speed_net/scripts/build/android.sh`](top_speed_net/scripts/build/android.sh).
- `top_speed_net/TopSpeed.iOS/` — iOS host (`net10.0-ios`). Not in the .sln; built directly via `dotnet publish` or release CI.
- `top_speed_net/TS.Audio/` — audio engine wrapper (mixer, buses, effects, DSP) on `net10.0`.
- `top_speed_net/TS.Sdl/` — SDL bindings used by the client/test harness (`net10.0`).
- `top_speed_net/SteamAudio.NET/` — Steam Audio bindings + native loader (managed `net10.0`).
- `top_speed_net/Updater/` — standalone updater. `TargetFrameworks=net472;net10.0` so the legacy `net472` updater can run on Windows hosts that lack newer runtimes.
- `top_speed_net/SoundFlow/` — git submodule providing the audio engine source (`https://github.com/diamondStar35/SoundFlow`).

### Server architecture (`top_speed_net/TopSpeed.Server/Network/`)

`RaceServer` (defined in `Network/Server.cs`) is the host/facade; behavior is split under `Network/Services/`:

- `Services/Session/` — handshake, packet ingress, disconnect lifecycle, recovery, session-level messaging.
- `Services/Room/` — membership, host actions, readiness, numbering, bots, lobby state, track/vehicle/track-package selection.
- `Services/Race/` — prepare/start/stop, simulation, snapshots, bots, participant finish, race completion.
- `Services/Notify/` — outbound room/race packet emission and journaling.
- `Services/Runtime.cs` — network/runtime scheduling.
- `Services/Chat.cs`, `Live.cs`, `Media.cs` — side channels; not room/race authority.

Rule: room ownership stays in `Room`, race ownership stays in `Race`, packet emission stays in `Notify`. Do not reintroduce room/race logic into `RaceServer`/`Server.cs` or into `Program.cs`. Partial files for `RaceServer` are acceptable (the class is already `partial`); use them only to keep the host/facade thin.

### Client multiplayer (`top_speed_net/TopSpeed/`)

Multiplayer is split into state, dispatch, runtime, and UI reactions:

- `Game/Multiplayer/Dispatch/` — packet registration, decoding, and routing.
- `Game/Multiplayer/Race/` — multiplayer race runtime and binding state.
- `Core/Multiplayer/Coordinator/State/Rooms/` — authoritative room store and room drafts.
- `Core/Multiplayer/Coordinator/RoomSync/Handlers/` — packet-to-store reducers.
- `Core/Multiplayer/Coordinator/RoomUi/` — room UI/menu/speech reactions after store changes.
- `Core/Multiplayer/Domain/Rooms/` and `Domain/Online/` — multiplayer domain models.

Rule: packet handlers update store/runtime state first; menu/speech effects happen second. Do not mix room state mutation with UI side effects in the same method.

### Menu, dialog, and localization

- Menu items and screen titles must be fed through `LocalizationService.Mark(...)` at the source (see `TopSpeed.Shared/Localization/LocalizationService.cs`).
- Shared announcement/rendering helpers translate user-facing text centrally; do not require callers to pre-translate.
- Input/control display text goes through shared formatters, not raw enum `ToString()` output.
- Translatable strings are extracted into `top_speed_net/languages/client/messages.pot` and `.../server/messages.pot` and synced via Crowdin (see `crowdin.yml`).

### Assets, data, and other top-level paths

- `top_speed_net/Sounds/`, `top_speed_net/Tracks/`, `top_speed_net/Vehicles/` — checked-in game content.
- `top_speed_net/languages/{client,server}/` — gettext `.pot` templates and per-locale `.po` translations.
- `top_speed_net/scripts/build/` — per-platform release scripts (`windows.ps1`, `linux.sh`, `macos.sh`, `android.sh`, `verify_docs.py`).
- `.github/workflows/release-build.yml` — CI release pipeline (Windows/Linux/macOS/Android/iOS); uses `.github/actions/setup-build-env` to provision .NET 10, Python 3, and (optionally) Java/Temurin.
- `.github/workflows/{crowdin-sync,locale-auto-merge,discussion-telegram}.yml` — localization + community automation.
- `.githooks/` — repo-managed git hooks (see [Git hooks](#git-hooks)).
- `docs/` — player-facing guides (`game-guide.md`, `track-creation-guide.md`, `vehicle-physics-and-creation-guide.md`, `testing.md`, `changes.md`).
- `installer/` — Inno Setup script (`TSPEED.ISS`) and installer art used by Windows release packaging.
- `original/` — preserved upstream C++ source (Visual Studio 2008). Reference only — do not extend.
- `info.json` — release metadata: `version`, `serverVersion`, and human-readable `changes` / `serverChanges` arrays consumed by the release workflow.

## Build, test & publish

All commands assume the repository root unless noted.

### Build & test

```bash
dotnet restore top_speed_net/TopSpeed.sln
dotnet build   top_speed_net/TopSpeed.sln -c Debug
dotnet build   top_speed_net/TopSpeed.sln -c Release
dotnet test    top_speed_net/TopSpeed.Tests/TopSpeed.Tests.csproj -c Debug --no-build
```

Notes:
- The test project targets `net10.0-windows` and therefore only runs on Windows. The release CI's Windows job (`top_speed_net/scripts/build/windows.ps1`) runs `dotnet test ... --framework net10.0-windows` as part of the build gate.
- `top_speed_net/Directory.Build.props` excludes `obj/**` from default item discovery; do not assume it appears in glob results.
- `top_speed_net/NuGet.Config` clears Visual Studio fallback package folders. Use the standard NuGet feed; do not check in package binaries.

### Publish — server

```bash
dotnet publish top_speed_net/TopSpeed.Server/TopSpeed.Server.csproj \
    -c Release -p:ServerPublishProfile=ReleaseWinX64
```

Supported `ServerPublishProfile` values (defined in `TopSpeed.Server.csproj`): `ReleaseWinX64`, `ReleaseLinuxX64`, `ReleaseLinuxArm64`, `ReleaseLinuxArm32`, `ReleaseLinuxMuslX64`, `ReleaseLinuxMuslArm64`, `ReleaseLinuxX86FrameworkDependent`, `ReleaseMacX64`, `ReleaseMacArm64`. Output goes to `TopSpeed.Server/bin/Publish/<rid>/`.

### Publish — desktop client

```bash
# Windows
dotnet publish top_speed_net/TopSpeed/TopSpeed.csproj -c Release -f net10.0-windows -r win-x64
# Linux
dotnet publish top_speed_net/TopSpeed/TopSpeed.csproj -c Release -f net10.0          -r linux-x64
# macOS (arm64)
dotnet publish top_speed_net/TopSpeed/TopSpeed.csproj -c Release -f net10.0          -r osx-arm64 -p:UseNativeAotUpdater=true
```

`-p:UseTrimmedUpdater=true` and `-p:UseNativeAotUpdater=true` toggle which updater build is shipped with the client; release scripts in `top_speed_net/scripts/build/` document the per-platform combinations.

### Publish — mobile

- Android: `top_speed_net/scripts/build/android.sh <version> <android_version_code> <apk_arm64> <apk_arm32>`.
- iOS: `dotnet publish top_speed_net/TopSpeed.iOS/TopSpeed.iOS.csproj -c Release -f net10.0-ios -r ios-arm64`. Codesigning is driven by the `IOS_CODESIGN_KEY` / `IOS_CODESIGN_PROVISION` environment variables.

### Native audio backends (`top_speed_net/SoundFlow/`)

The audio engine is the `SoundFlow` git submodule pinned to a specific commit on `https://github.com/diamondStar35/SoundFlow`. Managed C# code lives under `top_speed_net/SoundFlow/Src/` and `top_speed_net/SoundFlow/Codecs/`; native code lives under `top_speed_net/SoundFlow/Native/{ffmpeg-codec,miniaudio-backend,webrtc-audio-processing}/` and is shipped as pre-built per-RID binaries in `top_speed_net/SoundFlow/Codecs/SoundFlow.Codecs.FFMpeg/runtimes/<rid>/native/` and `top_speed_net/SoundFlow/Src/Backends/SoundFlow.Backends.MiniAudio/runtimes/<rid>/native/`.

Rules when fixing audio bugs:
- Fix the root cause in the SoundFlow submodule (managed `.cs` under `Src/` / `Codecs/`, or native `.c` / `CMakeLists.txt` under `Native/`). Do not patch around it in `top_speed_net/TopSpeed/...` or `top_speed_net/TS.Audio/...`.
- Land the SoundFlow change as a PR against `https://github.com/diamondStar35/SoundFlow` (`master`). Once merged, the **separate** runtimes-refresh step (below) is what makes the fix visible on disk for non-managed changes.
- Bump the SoundFlow submodule pointer in `top_speed_net` in a separate commit on the consuming branch; the existing pre-commit version-sync hook still applies.

Refreshing the bundled native runtimes (only required when `Native/**` changed):
- The build is **not** part of `dotnet build` — `Codecs/SoundFlow.Codecs.FFMpeg/SoundFlow.Codecs.FFMpeg.csproj` and the MiniAudio backend csproj just glob the `runtimes/<rid>/native/*` files into the package output.
- Source of truth for the FFmpeg native lib is `top_speed_net/SoundFlow/Native/ffmpeg-codec/CMakeLists.txt` (downloads FFmpeg 8.0 + LAME 3.100 via `ExternalProject_Add`, requires CMake ≥ 3.24, NASM, YASM, and a working host toolchain). The output `libsoundflow-ffmpeg.{so,dll,dylib}` lands in `build/runtimes/<rid>/native/` and must be copied into `Codecs/SoundFlow.Codecs.FFMpeg/runtimes/<rid>/native/` before commit.
- Per-RID rebuilds for all officially supported runtimes are produced by the `Build FFmpeg Codec Integration` workflow (`SoundFlow/.github/workflows/build-ffmpeg.yml`, `workflow_dispatch` only). Trigger it from the SoundFlow Actions tab against the branch carrying the native fix, download the artifacts, and commit them to the same SoundFlow branch as a separate "Update runtimes" commit.
- Do not commit only one platform's binary alongside a `Native/**` source change — the binaries must stay consistent across RIDs. If a partial refresh is unavoidable, call it out explicitly in the PR description.

### Localization templates

Regenerate `.pot` files after touching translatable strings:

```bash
pwsh -NoLogo -NoProfile -File top_speed_net/languages/Generate-Templates.ps1
```

The script requires GNU gettext (`xgettext`) on `PATH`. It walks every `*.cs` in the client and server trees (excluding `bin/` and `obj/`).

### Localization catalogs (`.po` -> `.mo`)

- Runtime translation loading uses compiled gettext catalogs (`messages.mo`) under `top_speed_net/languages/{client,server}/<locale>/`.
- Source-of-truth translations are `.po` files; do not commit generated `.mo` binaries.
- `TopSpeed.csproj` and `TopSpeed.Server.csproj` compile `.po` into `.mo` automatically before `Build`/`Publish` using GNU gettext `msgfmt`.
- Project targets also copy generated `.mo` catalogs explicitly into `OutDir` / `PublishDir` so first-build outputs include translations even when no `.mo` exists in source.
- If `msgfmt` is missing or a `.po` file is invalid, the build logs warnings and continues; affected locales fall back to source language at runtime.
- CI release flow compiles translations in a dedicated job and shares them as an artifact; release build jobs consume that artifact and pass `-p:SkipTranslationCompilation=true`.

## Coding style & naming

- Language: C#, 4-space indentation, `Nullable=enable`, `LangVersion=latest`. Match the surrounding file's style — do not reformat unrelated code.
- Naming:
  - `PascalCase` for types, methods, properties.
  - `camelCase` for locals and parameters.
  - `_camelCase` for private fields.
- Prefer narrow ownership by folder/module over large mixed-purpose files. Never use large files unless it's absolutely necessary and unavoidable.
- Inside a responsibility folder, prefer short responsibility-based type names (e.g. `Services/Room/Membership.cs`, not `Services/Room/RoomMembershipService.cs`).
- Do not use dotted filenames like `RoomStore.Apply.cs`. Use folders (`State/Rooms/Apply.cs`) instead.
- Partial classes are acceptable when they are already the local pattern and the split is coherent. Do not use partials to hide unrelated behavior.
- Place imports/`using`s at the top of files; do not nest them inside types or methods.
- Prefer shared formatting/translation helpers for repeated user-facing text paths.
- Do not use `dynamic`, reflection, or string-keyed lookups to dodge the type system; understand the type and access it directly.

## Testing guidelines

- **Minimum gate:** `dotnet build top_speed_net/TopSpeed.sln -c Debug` must build with 0 errors.
- **Preferred gate for logic changes (Windows):** also run `dotnet test top_speed_net/TopSpeed.Tests/TopSpeed.Tests.csproj -c Debug --no-build`.
- Test layout under `top_speed_net/TopSpeed.Tests/`:
  - `Behavior/{Shared,Server,Client}` — feature-level behavior tests.
  - `Invariants/{Shared,Architecture}` — protocol and architectural invariants.
  - `Regression/{Shared,Client}` — locked-in fixes for past bugs.
  - `Harness/{Shared,Client}` — fakes, builders, in-memory transports.
- For gameplay/network changes, runtime smoke test on a real client+server:
  - menu navigation and spoken output,
  - race loop behavior,
  - multiplayer connect/join/leave,
  - room prepare/start/finish/result flow,
  - disconnect/quit cleanup.
- For localization changes, regenerate `.pot` templates and verify the affected `msgid` entries in `top_speed_net/languages/client/messages.pot` or `.../server/messages.pot`.

## Git hooks

`.githooks/pre-commit` enforces a **client version sync** check: it parses `top_speed_net/TopSpeed.Shared/Protocol/VersionInfo.cs` (`ReleaseVersionInfo.{ClientYear,ClientMonth,ClientDay,ClientRevision}`) and requires `info.json`'s `version` field to equal `Year.Month.Day.Revision`. It also strict-parses every staged `*.json` file via `System.Text.Json` (no comments, no trailing commas).

The hook requires PowerShell 7 (`pwsh`) or Windows PowerShell on `PATH`. Enable hooks once per clone with **either** of:

```bash
git config core.hooksPath .githooks
# or
ln -sf ../../.githooks/pre-commit .git/hooks/pre-commit && chmod +x .git/hooks/pre-commit
```

When you bump `ReleaseVersionInfo`, also bump `info.json#version` (and add a `changes` entry); when you bump `ReleaseServerVersionInfo`, bump `info.json#serverVersion` (and add a `serverChanges` entry).

## Versioning & release metadata

- Version format is `YYYY.M.D.R` for both client and server (`R` is a same-day revision counter). The release workflow rejects any other format.
- `info.json#changes` and `info.json#serverChanges` are arrays of human-readable bullets surfaced in release notes; keep them user-visible (no internal jargon).
- `docs/changes.md` is the long-form changelog; keep new releases prepended at the top.
- Pushing to `main`/`master` triggers `.github/workflows/release-build.yml`, which builds and uploads per-platform artifacts named `TopSpeed-<rid>-Release-v-<version>.{zip,apk,ipa}`.

## Commit & pull request guidelines

- Use focused imperative subjects, e.g.:
  - `Fix dialog title localization`
  - `Refactor multiplayer room store`
  - `Add localized input display text`
- Keep one concern per commit.
- When behavior changes, include the user-visible effect in the commit body or PR description.
- PRs should describe:
  - what changed,
  - why it changed,
  - verification commands run,
  - runtime checks when relevant.

## Security & configuration notes

- Never commit secrets, tokens, signing keys, or machine-specific configuration. The `.gitignore` already excludes `top_speed.p12` and `signpass.txt` under `TopSpeed.Android/`; keep new credentials outside the tree as well.
- Never commit build or publish output (`bin/`, `obj/`, `Publish/`, `Release/`, `*.binlog`, etc.).
- Keep custom content inside approved asset folders (`Sounds/`, `Tracks/`, `Vehicles/`, `languages/`).
- Prefer project-relative paths and checked-in assets over hard-coded local filesystem dependencies.
- Do not extend `original/`; treat it as a read-only historical reference.
