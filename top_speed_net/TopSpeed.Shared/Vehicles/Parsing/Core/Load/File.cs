using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        public static bool TryLoadFromFile(string path, out CustomVehicleTsvData data)
        {
            return TryLoadFromFile(path, out data, out _);
        }

        public static bool TryLoadFromFile(string path, out CustomVehicleTsvData data, out IReadOnlyList<VehicleTsvIssue> issues)
        {
            data = null!;
            var issueList = new List<VehicleTsvIssue>();
            issues = issueList;

            if (string.IsNullOrWhiteSpace(path))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, Localized("Vehicle file path is empty.")));
                return false;
            }

            if (!File.Exists(path))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, Localized("Vehicle file not found: {0}", path)));
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (!string.Equals(Path.GetExtension(fullPath), ".tsv", StringComparison.OrdinalIgnoreCase))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, Localized("Custom vehicle file must use .tsv extension.")));
                return false;
            }

            if (!TryParseSections(fullPath, issueList, out var sections))
                return false;

            return TryBuild(fullPath, sections, issueList, out data);
        }
    }
}

