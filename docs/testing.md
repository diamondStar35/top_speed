# Testing Guide

## Unified test project
All automated tests now live under `top_speed_net/TopSpeed.Tests`.

## Build and run tests (Windows)
`dotnet build top_speed_net/TopSpeed.sln -c Debug`

`dotnet test top_speed_net/TopSpeed.Tests/TopSpeed.Tests.csproj -c Debug -f net10.0-windows --no-build`

The unified project covers shared, client, server, and updater behavior. Updater tests verify that
invalid packages cannot change installed files, custom drafts survive updates, and worker cleanup
does not remove unrelated files.

## Surface audio checks

`SurfaceAudioBehaviorTests` render audio through a silent backend, without using a physical audio
device. They compare surface positioning with the engine across the road, exercise all five shipped
surface recordings at driving pitch, and cover HRTF and stereo widening both enabled and disabled.
Surface playback is also checked with the engine stopped.

`SurfaceVolumeBehaviorTests` check that steering never quiets the surface below its normal level,
that the small steering boost fades in and out consistently across frame rates, and that surface
changes, braking, and volume settings preserve the cue.

For a listening check, drive left and right on each surface, then repeat while coasting with the
engine switched off. Check that the surface cue follows the car, including when changing surface
away from the center of the road. Repeat with HRTF and stereo widening enabled and disabled, and
confirm that the surface volume control and speed-dependent pitch still behave normally.
With progressive keyboard input or an analog controller, make small steering adjustments above
half the vehicle's top speed and check that the surface never drops out. Hold a turn, then release
it: the surface should become slightly louder and return smoothly to its normal level. Repeat
with ordinary keyboard input, while braking, and while coasting with the engine off.

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
