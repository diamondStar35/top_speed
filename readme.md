# Top Speed Remake

This project revives the audio‑only racing game **Top Speed**, originally created by *Playing in the Dark*.
The last known update to the original codebase was around **2013**.

## Background
I found the original GitHub repository, confirmed it was open source, and started a full C# rewrite.
The original project was written in C++ for Visual Studio 2008 and relied on libraries that are now outdated.

## Current Direction
This is no longer a direct port. Most systems have been rebuilt from scratch.
The goal is a **reimagined** game with modern, well‑designed systems and expanded content.

## Status
The game is in **early alpha** and not ready for a public release.
Prebuilt test builds are generated automatically for each commit on GitHub and may break at any time.

## Feedback & Testing
If you want to help test or provide feedback, check the commit history for builds.
A Telegram group is available for discussions and bug reports:

https://t.me/top_speed_remake

## Contact
- Telegram: https://t.me/diamondStar35
- WhatsApp: https://wa.me/201067573360
- Email: ramymaherali55@gmail.com

## Development on Windows

Install the .NET 10 SDK, Python 3, and GNU gettext (`msgfmt`). Initialize the checked-in SoundFlow
submodule when cloning:

```powershell
git submodule update --init --recursive
cd top_speed_net
dotnet build -c Release
dotnet test TopSpeed.Tests/TopSpeed.Tests.csproj -c Release --no-build
```

`TopSpeed.sln` is the default desktop/server development solution. Its projects are grouped into
Applications, Libraries, Dependencies, Tools, and Tests. Both Debug and Release use the requested
configuration throughout, including the server. The default build and restore require no mobile
workloads. SoundFlow is built automatically through the audio library's project references.

The server's ordinary build is framework-dependent. Release packaging is a separate, explicit step:

```powershell
dotnet publish TopSpeed.Server/TopSpeed.Server.csproj -c Release -p:ServerPublishProfile=ReleaseWinX64
dotnet publish TopSpeed/TopSpeed.csproj -c Release -f net10.0-windows -r win-x64
```

Publish profiles select the server's runtime, bundled .NET runtime, updater, and package directory.
Per-platform release scripts live in `top_speed_net/scripts/build/`.

## Mobile development

Android and iOS hosts are built explicitly from their projects, independently of the default solution.
They reuse the game as a library. Install the workload and platform tools for the host being built.

```powershell
# From top_speed_net; Android SDK and android workload required.
dotnet build TopSpeed.Android/TopSpeed.Android.csproj -c Debug
```

Build the iOS project on a Mac with Xcode, the `ios` workload, and device code-signing configured.
The platform publish commands and signing variables are documented in [AGENTS.md](AGENTS.md).

## Source map

Paths below are relative to `top_speed_net/`.

| Location | Responsibility |
| --- | --- |
| `TopSpeed/` | Game client, menus, input, speech, and platform integration |
| `TopSpeed/Multiplayer/` | Client multiplayer coordination, room state, game integration, racing, and transport |
| `TopSpeed/Vehicles/Car/` | Player-car definition and runtime fields |
| `TopSpeed/Vehicles/Powertrain/` | Engine, torque, and transmission runtime |
| `TopSpeed/Vehicles/Loading/` | Official and custom vehicle loading |
| `TopSpeed.Shared/` | Protocol, shared physics, bots, and content formats used by client and server |
| `TopSpeed.Server/` | Dedicated multiplayer server |
| `TopSpeed.Tests/` | Behavior, regression, and architecture tests |
| `TS.Audio/`, `TS.Sdl/` | Audio integration and SDL bindings |
| `Updater/` | Standalone updater |
| `TopSpeed.Android/`, `TopSpeed.iOS/` | Optional mobile hosts |
| `SoundFlow/`, `SteamAudio.NET/` | Audio engine source and Steam Audio bindings |

Client multiplayer has one source home, with these ownership areas:

- `Coordinator/`, `Domain/`, `Rooms/`, and `Connection/` manage state and room/connection behavior.
- `Application/` integrates multiplayer with the game, including packet dispatch and race bindings.
- `Racing/` implements the multiplayer driving session.
- `Network/` owns transport and packet serialization.

Solution folders organize projects in the IDE; source folders organize responsibilities inside a
project. Shared code remains in libraries used by the relevant applications and hosts.

See [the testing guide](docs/testing.md) for verification and listening checks,
[the game guide](docs/game-guide.md) for player instructions, and [AGENTS.md](AGENTS.md) for
repository-specific engineering rules and platform publish commands.
