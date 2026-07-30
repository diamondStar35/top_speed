using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private Queue<string>? _pendingVehicleKeepPrompts;

        // Vehicles that could not be saved, reported together once the prompts are done. Speaking a
        // failure as it happens does not work: the next prompt (or the return to the menu) announces
        // itself immediately afterwards and talks over it.
        private List<string>? _pendingVehicleKeepFailures;

        // Set when the server reports the race completed; the actual keep prompt is shown once the
        // race has exited back to the menu (showing it mid-finish loses it to the result dialog and
        // the menu transition, since the race loop does not service question dialogs).
        private bool _pendingVehicleKeepPromptAfterRace;

        // Called when a race finishes: offer to keep each vehicle downloaded this race (deduped).
        private void PromptKeepDownloadedVehicles()
        {
            if (_multiplayerVehiclePackagesSeenThisRace.Count == 0)
                return;

            // Rebuild the picture of what is already saved before deciding what to ask about. The
            // index is built once and only refreshed when this code saves something itself, so a
            // player who deleted or edited a vehicle on disk while the game was running would
            // otherwise be judged against how their Vehicles folder looked earlier in the session:
            // a vehicle they deleted still counts as saved and is never offered again.
            InvalidateLocalVehicleIndex();

            var toAsk = new List<string>();
            foreach (var hash in _multiplayerVehiclePackagesSeenThisRace)
            {
                if (!IsVehiclePackageKept(hash))
                    toAsk.Add(hash);
            }
            _multiplayerVehiclePackagesSeenThisRace.Clear();

            // Setting off => never persist (the package stays in the in-memory session cache only).
            if (!_settings.KeepDownloadedVehiclesPrompt || toAsk.Count == 0)
                return;

            _pendingVehicleKeepPrompts = new Queue<string>(toAsk);
            _pendingVehicleKeepFailures = null;
            ShowNextVehicleKeepPrompt();
        }

        private const int VehicleKeepBothChoiceId = 4101;
        private const int VehicleKeepReplaceChoiceId = 4102;
        private const int VehicleKeepMineChoiceId = 4103;

        private void ShowNextVehicleKeepPrompt()
        {
            if (_pendingVehicleKeepPrompts == null || _pendingVehicleKeepPrompts.Count == 0)
            {
                _pendingVehicleKeepPrompts = null;
                ShowVehicleKeepFailureReport();
                return;
            }

            var hash = _pendingVehicleKeepPrompts.Dequeue();
            var hasPackage = _multiplayerVehiclePackageCache.TryGetValue(hash, out var pkg) && pkg != null;
            var name = hasPackage && !string.IsNullOrWhiteSpace(pkg!.DisplayName)
                ? pkg!.DisplayName
                : LocalizationService.Mark("Custom vehicle");

            // Saving would land on a folder a DIFFERENT vehicle already occupies (a kept copy of
            // this same vehicle is filtered out before prompting), so offer the choice rather than
            // silently saving a second copy under an invented name.
            if (hasPackage && TryFindKeptVehicleConflict(pkg!, out var existingFolder, out var existingFolderName))
            {
                ShowVehicleKeepConflictPrompt(hash, name, existingFolder, existingFolderName);
                return;
            }

            var question = new Question(
                LocalizationService.Mark("Keep custom vehicle?"),
                LocalizationService.Format(LocalizationService.Mark("Keep the downloaded vehicle \"{0}\" so it does not need downloading again next time?"), name),
                QuestionId.No,
                resultId =>
                {
                    if (resultId == QuestionId.Yes && !KeepVehiclePackageOnDisk(hash))
                        AnnounceVehicleKeepFailed(name);
                    ShowNextVehicleKeepPrompt();
                },
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, keep it")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No"), flags: QuestionButtonFlags.Default));

            _multiplayerCoordinator.Questions.Show(question);
        }

        private void ShowVehicleKeepConflictPrompt(string hash, string name, string existingFolder, string existingFolderName)
        {
            var question = new Question(
                LocalizationService.Mark("Keep custom vehicle?"),
                LocalizationService.Format(
                    LocalizationService.Mark("You already have a vehicle saved as \"{0}\". What do you want to do with the downloaded \"{1}\"?"),
                    existingFolderName,
                    name),
                // Escaping keeps what the player already has: the only destructive option here is
                // replace, so it must never be what happens by accident.
                VehicleKeepMineChoiceId,
                resultId =>
                {
                    if (resultId == VehicleKeepBothChoiceId)
                    {
                        if (!KeepVehiclePackageOnDisk(hash))
                            AnnounceVehicleKeepFailed(name);
                        ShowNextVehicleKeepPrompt();
                        return;
                    }

                    if (resultId == VehicleKeepReplaceChoiceId)
                    {
                        ConfirmReplaceKeptVehicle(hash, name, existingFolder, existingFolderName);
                        return;
                    }

                    ShowNextVehicleKeepPrompt();
                },
                new QuestionButton(VehicleKeepBothChoiceId, LocalizationService.Mark("Keep both")),
                new QuestionButton(VehicleKeepReplaceChoiceId, LocalizationService.Mark("Replace mine with the downloaded one")),
                new QuestionButton(VehicleKeepMineChoiceId, LocalizationService.Mark("Keep mine"), flags: QuestionButtonFlags.Default));

            _multiplayerCoordinator.Questions.Show(question);
        }

        // Replacing deletes a vehicle the player may have built themselves, and it is the only step
        // in this flow that cannot be undone, so it is confirmed separately.
        private void ConfirmReplaceKeptVehicle(string hash, string name, string existingFolder, string existingFolderName)
        {
            var question = new Question(
                LocalizationService.Mark("Replace vehicle?"),
                LocalizationService.Format(
                    LocalizationService.Mark("This permanently deletes the vehicle saved as \"{0}\" and puts the downloaded \"{1}\" there instead. Are you sure?"),
                    existingFolderName,
                    name),
                QuestionId.No,
                resultId =>
                {
                    if (resultId == QuestionId.Yes && !ReplaceKeptVehicleOnDisk(hash, existingFolder))
                        AnnounceVehicleKeepFailed(name);
                    ShowNextVehicleKeepPrompt();
                },
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, replace it")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, keep mine"), flags: QuestionButtonFlags.Default));

            _multiplayerCoordinator.Questions.Show(question);
        }

        // True when the folder this vehicle would be saved into is already taken. Reports the full
        // path (for deleting) and the folder name relative to the Vehicles folder (for speaking).
        private bool TryFindKeptVehicleConflict(DownloadedVehiclePackage pkg, out string existingFolder, out string existingFolderName)
        {
            existingFolder = string.Empty;
            existingFolderName = string.Empty;

            var vehiclesFolder = GetClientVehiclesFolder();
            var folderName = ResolveKeptVehicleFolderName(pkg);
            var candidate = Path.Combine(vehiclesFolder, folderName);
            if (!IsInsideVehiclesFolder(candidate, vehiclesFolder) || !Directory.Exists(candidate))
                return false;

            existingFolder = candidate;
            existingFolderName = folderName.Replace(Path.DirectorySeparatorChar, '/');
            return true;
        }

        private bool ReplaceKeptVehicleOnDisk(string hash, string existingFolder)
        {
            var vehiclesFolder = GetClientVehiclesFolder();
            if (!IsInsideVehiclesFolder(existingFolder, vehiclesFolder))
                return false;

            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (!_multiplayerVehiclePackageCache.TryGetValue(normalizedHash, out var pkg) || pkg == null)
                return false;
            if (string.IsNullOrWhiteSpace(pkg.Payload?.TsvText))
                return false;

            // The old vehicle is moved aside rather than deleted outright, so if writing the
            // replacement fails part way through it can be put back instead of leaving the player
            // with neither. The whole folder goes, not just the files being overwritten, so sound
            // files belonging to the replaced vehicle cannot linger and be picked up by the new one.
            if (!TryReserveReplacedVehicleBackupPath(existingFolder, out var backupFolder))
                return false;

            try
            {
                Directory.Move(existingFolder, backupFolder);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            if (TryWriteVehiclePackageFiles(existingFolder, pkg.Payload, out _))
            {
                TryDeleteDirectory(backupFolder);
                InvalidateLocalVehicleIndex();
                return true;
            }

            // Writing failed: discard the partial copy and restore what the player had.
            TryDeleteDirectory(existingFolder);
            try
            {
                Directory.Move(backupFolder, existingFolder);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        private static bool TryReserveReplacedVehicleBackupPath(string existingFolder, out string backupFolder)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var candidate = existingFolder + ".replacing" + (attempt == 0
                    ? string.Empty
                    : attempt.ToString(CultureInfo.InvariantCulture));
                if (Directory.Exists(candidate))
                    continue;

                backupFolder = candidate;
                return true;
            }

            backupFolder = string.Empty;
            return false;
        }

        private static void TryDeleteDirectory(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // "Kept" = present in the client's own Vehicles folder (also usable offline), matched by
        // content hash so we never re-download a vehicle we already have.
        private bool IsVehiclePackageKept(string hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return false;
            EnsureLocalVehicleIndex();
            return _localVehicleIndex!.ContainsKey(normalizedHash);
        }

        // Saves the downloaded vehicle into the client's Vehicles folder as a real .tsv + sound
        // files, so it is available offline (time trial / single race) and reused on future
        // servers without re-downloading. Creates the Vehicles folder if it does not exist.
        private bool KeepVehiclePackageOnDisk(string hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return false;
            if (!_multiplayerVehiclePackageCache.TryGetValue(normalizedHash, out var pkg) || pkg == null)
                return false;
            if (string.IsNullOrWhiteSpace(pkg.Payload?.TsvText))
                return false;

            var vehiclesFolder = GetClientVehiclesFolder();
            var folderName = ResolveKeptVehicleFolderName(pkg);
            var destination = Path.Combine(vehiclesFolder, folderName);
            if (!IsInsideVehiclesFolder(destination, vehiclesFolder))
                return false;

            // A kept copy of THIS vehicle is excluded before prompting, so an existing folder of the
            // same name means a different vehicle already claimed it and the player chose to keep
            // both; save alongside it under the next free number.
            if (Directory.Exists(destination) && !TryResolveFreeNumberedFolder(vehiclesFolder, folderName, out destination))
                return false;

            if (!TryWriteVehiclePackageFiles(destination, pkg.Payload, out _))
                return false;

            InvalidateLocalVehicleIndex();
            return true;
        }

        // Saving touches the filesystem and can fail for reasons the player can act on (the folder
        // being read only, or a file still open), so a failure has to say so rather than look like
        // the choice was ignored.
        private void AnnounceVehicleKeepFailed(string name)
        {
            _pendingVehicleKeepFailures ??= new List<string>();
            _pendingVehicleKeepFailures.Add(name);
        }

        // One dialog for every vehicle that could not be saved, shown after the last prompt so it
        // is not talked over, and dismissible so the player can take it in rather than catch it in
        // passing. Only appears when something actually failed.
        private void ShowVehicleKeepFailureReport()
        {
            var failures = _pendingVehicleKeepFailures;
            _pendingVehicleKeepFailures = null;
            if (failures == null || failures.Count == 0)
                return;

            var caption = failures.Count == 1
                ? LocalizationService.Format(
                    LocalizationService.Mark("Could not save the vehicle \"{0}\". Its folder may be read only, or a file in it may still be in use."),
                    failures[0])
                : LocalizationService.Format(
                    LocalizationService.Mark("Could not save these vehicles: {0}. Their folders may be read only, or files in them may still be in use."),
                    string.Join(", ", failures));

            _multiplayerCoordinator.Questions.Show(new Question(
                LocalizationService.Mark("Vehicle not saved"),
                caption,
                QuestionId.Ok,
                _ => { },
                new QuestionButton(QuestionId.Ok, LocalizationService.Mark("OK"), flags: QuestionButtonFlags.Default)));
        }

        // Reproduce the source folder path ("NASCAR/cup car dodge") so a kept vehicle matches the
        // server's on-disk layout, and two identically named folders from different packs do not
        // collapse onto each other locally. The manifest value comes from the server and is
        // untrusted, so it is normalised the same way sound asset keys are (which rejects "..",
        // rooted paths and drive letters) and every segment is sanitised individually. Falls back to
        // the display name / .tsv basename when the source .tsv sat directly in the Vehicles root
        // (whose leaf directory name is just "Vehicles").
        private static string ResolveKeptVehicleFolderName(DownloadedVehiclePackage pkg)
        {
            var folder = (pkg.Payload?.Manifest?.FolderName ?? string.Empty).Trim();
            var normalized = VehiclePackageCodec.NormalizeAssetKey(folder);
            if (!string.IsNullOrWhiteSpace(normalized)
                && !string.Equals(normalized, "Vehicles", StringComparison.OrdinalIgnoreCase))
            {
                var segments = normalized.Split('/');
                var safeSegments = new List<string>(segments.Length);
                for (var i = 0; i < segments.Length; i++)
                {
                    var sanitized = SanitizeVehicleFolderSegment(segments[i]);
                    if (sanitized.Length > 0)
                        safeSegments.Add(sanitized);
                }

                if (safeSegments.Count > 0)
                    return string.Join(Path.DirectorySeparatorChar.ToString(), safeSegments);
            }

            var fallback = !string.IsNullOrWhiteSpace(pkg.DisplayName)
                ? pkg.DisplayName
                : Path.GetFileNameWithoutExtension(pkg.Payload?.Manifest?.TsvFileName ?? string.Empty);
            var fallbackName = SanitizeVehicleFolderSegment(fallback);
            return fallbackName.Length == 0 ? "custom-vehicle" : fallbackName;
        }

        // "Chevy Laguna 2", then 3, and so on. The folder name is read aloud when two vehicles share
        // a name, so a plain number beats the content hash this used to append.
        private static bool TryResolveFreeNumberedFolder(string vehiclesFolder, string folderName, out string destination)
        {
            destination = string.Empty;
            for (var number = 2; number <= 99; number++)
            {
                var candidate = Path.Combine(
                    vehiclesFolder,
                    folderName + " " + number.ToString(CultureInfo.InvariantCulture));
                if (!IsInsideVehiclesFolder(candidate, vehiclesFolder))
                    return false;
                if (Directory.Exists(candidate))
                    continue;

                destination = candidate;
                return true;
            }

            return false;
        }

        // Makes a single path segment safe to create on disk. Returns an empty string when nothing
        // usable survives, so the caller can drop the segment rather than create a junk folder.
        private static string SanitizeVehicleFolderSegment(string value)
        {
            var name = value ?? string.Empty;
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            name = name.Trim().Trim('.');
            if (name.Length == 0)
                return string.Empty;
            if (name.Length > 64)
                name = name.Substring(0, 64);
            return name;
        }

        // Defence in depth: the folder path is server-supplied, so confirm the resolved destination
        // really sits inside the client's own Vehicles folder before creating or writing anything.
        private static bool IsInsideVehiclesFolder(string candidate, string vehiclesFolder)
        {
            var root = Path.GetFullPath(vehiclesFolder);
            var full = Path.GetFullPath(candidate);
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
                return false;

            var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Wipes session-only downloaded vehicles left over from a previous run. Kept vehicles
        // live in a separate directory and survive. Call once on startup.
        private static void WipeVehiclePackageSessionCache()
        {
            try
            {
                var root = GetVehiclePackageSessionRoot();
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
