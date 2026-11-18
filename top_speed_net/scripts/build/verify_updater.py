"""Exercise a built updater against disposable installs, without launching Top Speed."""

import argparse
import ctypes
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import tempfile
import time
import uuid
import zipfile


def run(command, **kwargs):
    if os.name == "nt":
        kwargs["creationflags"] = subprocess.CREATE_NO_WINDOW
    return subprocess.run(command, capture_output=True, text=True, timeout=30, **kwargs)


def fixture_game(directory, version):
    output = directory / (version + (".exe" if os.name == "nt" else ""))
    if os.name == "nt":
        source = output.with_suffix(".cs")
        source.write_text(
            'using System; using System.IO; class FakeGame { static void Main() {'
            'string d=AppDomain.CurrentDomain.BaseDirectory;'
            'File.WriteAllText(Path.Combine(d,"workers-at-restart.txt"),'
            'Directory.GetDirectories(d,".topspeed-worker-*").Length.ToString());'
            'File.WriteAllText(Path.Combine(d,"restarted.txt"),"' + version + '"); }}',
            encoding="utf-8",
        )
        compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319/csc.exe"
        result = run([str(compiler), "/nologo", "/target:winexe", "/out:" + str(output), str(source)])
        if result.returncode:
            raise RuntimeError(result.stdout + result.stderr)
    else:
        output.write_text('#!/bin/sh\nprintf ' + version + ' > "$(dirname "$0")/restarted.txt"\n')
        output.chmod(0o755)
    return output


def add(archive, name, data, mode=0o644):
    entry = zipfile.ZipInfo(name)
    entry.create_system = 3
    entry.external_attr = (stat.S_IFREG | mode) << 16
    archive.writestr(entry, data)


def verify(updater, sandbox, case, old_game, new_game):
    windows = os.name == "nt"
    target = sandbox / case / ("TestGame.app/Contents/MacOS" if case == "bundle" else "install with spaces")
    target.mkdir(parents=True)
    names = [updater.name] + ["TopSpeedUpdater" + suffix for suffix in (".exe.config", ".dll", ".deps.json", ".runtimeconfig.json")]
    payload = [updater.parent / name for name in names if (updater.parent / name).is_file()]
    for source in payload:
        shutil.copy2(source, target / source.name)
    game_name = "TestGame.exe" if windows else "TestGame"
    shutil.copy2(old_game, target / game_name)
    before_inode = (target / game_name).stat().st_ino
    for name, text in (("notes", "original"), ("notes.new", "draft"), ("notes.superseded", "backup")):
        (target / name).write_text(text)
    package = target.parent / "update.zip"
    if case == "corrupt":
        package.write_bytes(b"invalid ZIP archive")
    else:
        prefix = "TestGame.app/Contents/MacOS/" if case == "bundle" else ""
        with zipfile.ZipFile(package, "w") as archive:
            for source in payload:
                data = source.read_bytes() + (b"\0" if windows and source == updater else b"")
                if case == "invalid-updater" and source == updater:
                    data = b"MZ invalid executable"
                add(archive, prefix + source.name, data, stat.S_IMODE(source.stat().st_mode))
            if case == "locked":
                add(archive, "blocked.dat", b"replacement")
            add(archive, prefix + game_name, new_game.read_bytes(), 0o755)

    handle = None
    if case == "locked":
        (target / "blocked.dat").write_bytes(b"original")
        kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel.CreateFileW.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
        kernel.CreateFileW.restype = ctypes.c_void_p
        kernel.CloseHandle.argtypes = [ctypes.c_void_p]
        handle = kernel.CreateFileW(str(target / "blocked.dat"), 0x80000000, 0, None, 3, 0, None)
        if handle == ctypes.c_void_p(-1).value:
            raise ctypes.WinError(ctypes.get_last_error())
    try:
        result = run([str(target / updater.name), "--pid", "2147483647", "--zip", str(package), "--dir", str(target) + os.sep, "--game", "TestGame", "--log"])
        expected = "old" if case in ("corrupt", "locked") else "new"
        marker = target / "restarted.txt"
        if windows or expected == "new":
            deadline = time.monotonic() + 15
            while not marker.exists() and time.monotonic() < deadline:
                time.sleep(0.05)
            if not marker.exists() or marker.read_text() != expected:
                log = target / "updater.log"
                raise RuntimeError(result.stderr + (log.read_text() if log.exists() else "No updater log"))
        elif result.returncode == 0 or (target / game_name).stat().st_ino != before_inode:
            raise AssertionError("Invalid archive changed the Unix installation")
        if windows:
            assert (target / "workers-at-restart.txt").read_text() == "0"
            if expected == "old" or case == "invalid-updater":
                assert (target / updater.name).read_bytes() == updater.read_bytes()
        elif expected == "new":
            assert (target / game_name).stat().st_ino != before_inode
            assert stat.S_IMODE((target / game_name).stat().st_mode) == 0o755
        assert (target / "notes.new").read_text() == "draft"
        assert (target / "notes.superseded").read_text() == "backup"
        assert not list(target.glob(".topspeed-worker-*"))
        assert not list(target.glob(".topspeed-update-*"))
        assert package.exists() == (expected == "old")
        return {"case": case, "result": "passed", "restarted": expected if marker.exists() else False}
    finally:
        if handle is not None:
            kernel.CloseHandle(handle)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--updater", type=Path, required=True, help="Built updater executable (not its DLL)")
    args = parser.parse_args()
    updater = args.updater.resolve(strict=True)
    repository = Path(__file__).resolve().parents[3]
    if os.name == "nt":
        sandbox = repository / "top_speed_net/Updater/obj" / ("process-check-" + uuid.uuid4().hex[:8])
        sandbox.mkdir(parents=True)
    else:
        # Exercise Unix inode and permission semantics on its native temporary filesystem.
        sandbox = Path(tempfile.mkdtemp(prefix="top-speed-updater-check-"))
    old_game = fixture_game(sandbox, "old")
    new_game = fixture_game(sandbox, "new")
    cases = ["success", "corrupt", "locked", "invalid-updater"] if os.name == "nt" else ["success", "corrupt", "bundle"]
    print(json.dumps([verify(updater, sandbox, case, old_game, new_game) for case in cases], indent=2))
    print("Logs and disposable installs:", sandbox)


if __name__ == "__main__":
    main()
