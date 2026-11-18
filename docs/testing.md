# Testing Guide

## Unified test project
All automated tests now live under `top_speed_net/TopSpeed.Tests`.

## Build and run tests (Windows)
`dotnet build top_speed_net/TopSpeed.sln -c Debug`

`dotnet test top_speed_net/TopSpeed.Tests/TopSpeed.Tests.csproj -c Debug -f net10.0-windows --no-build`

The unified project covers shared, client, server, and updater behavior. Updater tests verify that
invalid packages cannot change installed files, custom drafts survive updates, and worker cleanup
does not remove unrelated files.

## Updater process checks

The process harness uses disposable installations and a small test application, never the real game.
It checks replacement, restart, temporary-worker cleanup, and recovery from invalid archives. On
Windows it also holds an installed file open to check failure recovery. On Unix it checks fresh
inodes, executable permissions, and the macOS bundle payload layout.

Windows, after a Debug solution build:

```powershell
python top_speed_net/scripts/build/verify_updater.py --updater top_speed_net/Updater/bin/Debug/net472/TopSpeedUpdater.exe
```

Linux, from a checkout with the .NET SDK and Python 3:

```bash
dotnet publish top_speed_net/Updater/Updater.csproj -c Debug -f net10.0 -r linux-x64 -p:UseTrimmedUpdater=true -o top_speed_net/Updater/bin/Smoke/linux-x64
python3 top_speed_net/scripts/build/verify_updater.py --updater top_speed_net/Updater/bin/Smoke/linux-x64/TopSpeedUpdater
```

On macOS, run the same process harness against the updater published for that Mac. A real Mac test
is still required to verify code-signing behavior; the bundle-layout check on Linux is not a substitute.
The harness prints where it retained test installations and logs for inspection.
