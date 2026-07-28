using System;
using System.Collections.Generic;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Parsing;
using TopSpeed.Localization;

namespace TopSpeed.Core
{
    internal sealed class VehicleSource : SourceBase<CustomVehicleInfo>
    {
        public VehicleSource()
            : base("Vehicles", "*.tsv")
        {
        }

        protected override string GetKey(CustomVehicleInfo info)
        {
            return info.Key;
        }

        protected override string GetDisplay(CustomVehicleInfo info)
        {
            return info.Display;
        }

        protected override CustomVehicleInfo WithDisplay(CustomVehicleInfo info, string display)
        {
            return new CustomVehicleInfo(info.Key, display, info.Version, info.Description);
        }

        protected override (bool Success, CustomVehicleInfo Value) ParseCore(string file)
        {
            if (!VehicleTsvParser.TryLoadFromFile(file, out var parsed, out var issues))
            {
                AppendIssues(file, issues);
                return (false, default);
            }

            if (issues != null && issues.Count > 0)
                AppendIssues(file, issues);

            // The .tsv parsed, but it may reference sound files that are not on disk (a common packaging
            // mistake: the .tsv ships without its .wav). Surface that here, up front, instead of letting the
            // race loader hit it later. Both required and optional missing sounds are warnings only: the car
            // stays selectable and playable because the loader substitutes a built-in default for a missing
            // required sound and simply skips a missing optional one. The line states which it is.
            var builtinRoot = System.IO.Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var soundIssues = Vehicles.Loader.Sound.ValidateCustomSounds(parsed.Sounds, builtinRoot, parsed.SourceDirectory);
            if (soundIssues.Count > 0)
            {
                AddFileIssue(file);
                var warning = LocalizationService.Translate(LocalizationService.Mark("Warning"));
                for (var i = 0; i < soundIssues.Count; i++)
                {
                    var issue = soundIssues[i];

                    // When it is a missing file we name the path; for a malformed/unsafe reference we show
                    // the reason instead. Either way we state what the game will do about it.
                    string body;
                    if (!string.IsNullOrEmpty(issue.MissingPath))
                        body = issue.Required
                            ? LocalizationService.Format(LocalizationService.Mark("required sound not found, will use built in fallback: {0}"), issue.MissingPath)
                            : LocalizationService.Format(LocalizationService.Mark("optional sound not found, no sound will play: {0}"), issue.MissingPath);
                    else
                        body = issue.Required
                            ? LocalizationService.Format(LocalizationService.Mark("required sound problem, will use built in fallback: {0}"), issue.Message)
                            : LocalizationService.Format(LocalizationService.Mark("optional sound problem, no sound will play: {0}"), issue.Message);

                    AddIssue(LocalizationService.Format(LocalizationService.Mark("{0}: {1}"), warning, body));
                }
            }

            var info = new CustomVehicleInfo(
                file,
                string.IsNullOrWhiteSpace(parsed.Meta.Name) ? LocalizationService.Mark("Custom vehicle") : parsed.Meta.Name,
                parsed.Meta.Version ?? string.Empty,
                parsed.Meta.Description ?? string.Empty);
            return (true, info);
        }

        private void AppendIssues(string file, IReadOnlyList<VehicleTsvIssue> issues)
        {
            AddFileIssue(file);

            if (issues == null || issues.Count == 0)
            {
                AddIssue(LocalizationService.Mark("Failed to load this vehicle file."));
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                var label = issues[i].Severity == VehicleTsvIssueSeverity.Error
                    ? LocalizationService.Translate(LocalizationService.Mark("Error"))
                    : LocalizationService.Translate(LocalizationService.Mark("Warning"));
                AddIssue(LocalizationService.Format(LocalizationService.Mark("{0}: {1}"), label, issues[i].ToString()));
            }
        }
    }
}

