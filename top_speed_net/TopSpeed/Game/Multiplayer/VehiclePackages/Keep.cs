using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private Queue<string>? _pendingVehicleKeepPrompts;

        // Set when the server reports the race completed; the actual keep prompt is shown once the
        // race has exited back to the menu (showing it mid-finish loses it to the result dialog and
        // the menu transition, since the race loop does not service question dialogs).
        private bool _pendingVehicleKeepPromptAfterRace;

        // Called when a race finishes: offer to keep each vehicle downloaded this race (deduped).
        private void PromptKeepDownloadedVehicles()
        {
            if (_multiplayerVehiclePackagesDownloadedThisRace.Count == 0)
                return;

            var toAsk = new List<string>();
            foreach (var hash in _multiplayerVehiclePackagesDownloadedThisRace)
            {
                if (!IsVehiclePackageKept(hash))
                    toAsk.Add(hash);
            }
            _multiplayerVehiclePackagesDownloadedThisRace.Clear();

            // Setting off => never persist (the package stays in the in-memory session cache only).
            if (!_settings.KeepDownloadedVehiclesPrompt || toAsk.Count == 0)
                return;

            _pendingVehicleKeepPrompts = new Queue<string>(toAsk);
            ShowNextVehicleKeepPrompt();
        }

        private void ShowNextVehicleKeepPrompt()
        {
            if (_pendingVehicleKeepPrompts == null || _pendingVehicleKeepPrompts.Count == 0)
            {
                _pendingVehicleKeepPrompts = null;
                return;
            }

            var hash = _pendingVehicleKeepPrompts.Dequeue();
            var name = _multiplayerVehiclePackageCache.TryGetValue(hash, out var pkg) && !string.IsNullOrWhiteSpace(pkg.DisplayName)
                ? pkg.DisplayName
                : LocalizationService.Mark("Custom vehicle");

            var question = new Question(
                LocalizationService.Mark("Keep custom vehicle?"),
                LocalizationService.Format(LocalizationService.Mark("Keep the downloaded vehicle \"{0}\" so it does not need downloading again next time?"), name),
                QuestionId.No,
                resultId =>
                {
                    if (resultId == QuestionId.Yes)
                        KeepVehiclePackageOnDisk(hash);
                    ShowNextVehicleKeepPrompt();
                },
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, keep it")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No"), flags: QuestionButtonFlags.Default));

            _multiplayerCoordinator.Questions.Show(question);
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
        private void KeepVehiclePackageOnDisk(string hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return;
            if (!_multiplayerVehiclePackageCache.TryGetValue(normalizedHash, out var pkg) || pkg == null)
                return;
            if (string.IsNullOrWhiteSpace(pkg.Payload?.TsvText))
                return;

            var vehiclesFolder = GetClientVehiclesFolder();
            var folderName = ResolveKeptVehicleFolderName(pkg);
            var destination = Path.Combine(vehiclesFolder, folderName);
            if (!IsInsideVehiclesFolder(destination, vehiclesFolder))
                return;

            // A kept copy of THIS vehicle is excluded before prompting, so an existing folder of the
            // same name means a different vehicle already claimed it; disambiguate with a hash suffix.
            if (Directory.Exists(destination))
            {
                var suffix = normalizedHash.Length >= 8 ? normalizedHash.Substring(0, 8) : normalizedHash;
                destination = Path.Combine(vehiclesFolder, folderName + "_" + suffix);
                if (!IsInsideVehiclesFolder(destination, vehiclesFolder))
                    return;
            }

            if (TryWriteVehiclePackageFiles(destination, pkg.Payload, out _))
                InvalidateLocalVehicleIndex();
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
