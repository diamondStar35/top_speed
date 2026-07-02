using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static bool TryParseSections(string fullPath, List<VehicleTsvIssue> issues, out Dictionary<string, Section> sections)
        {
            sections = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
            string? currentSection = null;
            var lineNo = 0;

            foreach (var raw in File.ReadLines(fullPath))
            {
                lineNo++;
                var line = StripInlineComment(raw).Trim();
                if (line.Length == 0)
                    continue;

                if (TryParseSectionHeader(line, out var sectionName))
                {
                    if (!s_allowedKeys.ContainsKey(sectionName))
                    {
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Unknown section [{0}].", sectionName)));
                        currentSection = null;
                        continue;
                    }

                    if (sections.ContainsKey(sectionName))
                    {
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Duplicate section [{0}] is not allowed.", sectionName)));
                        currentSection = null;
                        continue;
                    }

                    sections[sectionName] = new Section(sectionName, lineNo);
                    currentSection = sectionName;
                    continue;
                }

                if (!TryParseKeyValue(line, out var rawKey, out var rawValue))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Invalid line. Expected [section] or key=value.")));
                    continue;
                }

                if (currentSection is null)
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Top-level key '{0}' is not supported.", rawKey.Trim())));
                    continue;
                }

                var key = NormalizeKey(rawKey);
                if (!IsAllowedKey(currentSection, key))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Unknown key '{0}' in section [{1}].", key, currentSection)));
                    continue;
                }

                var section = sections[currentSection];
                if (section.Entries.ContainsKey(key))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, Localized("Duplicate key '{0}' in section [{1}] is not allowed.", key, currentSection)));
                    continue;
                }

                section.Entries[key] = new Entry(rawValue.Trim(), lineNo);
            }

            for (var i = 0; i < s_requiredSections.Length; i++)
            {
                var required = s_requiredSections[i];
                if (!sections.ContainsKey(required))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, Localized("Missing required section [{0}].", required)));
            }

            return !HasErrors(issues);
        }
    }
}

