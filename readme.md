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
The game is considered stable enough that it can be played on Windows and other platforms.
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

See [the testing guide](docs/testing.md) for verification and listening checks,
[the game guide](docs/game-guide.md) for player instructions, and [AGENTS.md](AGENTS.md) for
repository-specific engineering rules and platform publish commands.
