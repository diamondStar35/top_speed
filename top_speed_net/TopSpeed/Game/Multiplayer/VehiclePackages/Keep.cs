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

            var folderName = BuildKeptVehicleFolderName(pkg.DisplayName, normalizedHash);
            var destination = Path.Combine(GetClientVehiclesFolder(), folderName);
            if (TryWriteVehiclePackageFiles(destination, pkg.Payload, out _))
                InvalidateLocalVehicleIndex();
        }

        private static string BuildKeptVehicleFolderName(string displayName, string hash)
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? "custom-vehicle" : displayName;
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            name = name.Trim().Trim('.');
            if (name.Length == 0)
                name = "custom-vehicle";
            if (name.Length > 64)
                name = name.Substring(0, 64);

            var suffix = hash.Length >= 8 ? hash.Substring(0, 8) : hash;
            return name + "_" + suffix;
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
