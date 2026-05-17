using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal static class HelpDocumentResolver
    {
        private const string DefaultLanguageFolder = "en";

        public static IReadOnlyList<string> BuildCandidateRelativePaths(string fileName, string? languageCode)
        {
            var candidates = new List<string>();
            AddLocalizedCandidates(candidates, fileName, languageCode);
            AddUnique(candidates, CombinePath(DefaultLanguageFolder, fileName));
            AddUnique(candidates, fileName);
            return candidates;
        }

        private static void AddLocalizedCandidates(List<string> candidates, string fileName, string? languageCode)
        {
            var exactFolder = GetLocaleFolderName(languageCode);
            if (string.IsNullOrWhiteSpace(exactFolder))
                return;

            AddUnique(candidates, CombinePath(exactFolder, fileName));

            var parentFolder = GetParentLocaleFolderName(exactFolder);
            if (!string.IsNullOrWhiteSpace(parentFolder))
                AddUnique(candidates, CombinePath(parentFolder, fileName));
        }

        private static string CombinePath(string folderName, string fileName)
        {
            return folderName + "/" + fileName;
        }

        private static string GetLocaleFolderName(string? languageCode)
        {
            var culture = LanguageCode.TryResolveCulture(languageCode ?? string.Empty);
            if (culture != null && !string.IsNullOrWhiteSpace(culture.Name))
                return culture.Name;

            var normalized = LanguageCode.Normalize(languageCode);
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            return CanonicalizeLocaleName(normalized);
        }

        private static string GetParentLocaleFolderName(string localeFolder)
        {
            if (string.IsNullOrWhiteSpace(localeFolder))
                return string.Empty;

            var normalized = LanguageCode.Normalize(localeFolder);
            var parent = LanguageCode.ParentOf(normalized);
            return string.IsNullOrWhiteSpace(parent)
                ? string.Empty
                : CanonicalizeLocaleName(parent);
        }

        private static string CanonicalizeLocaleName(string normalizedLanguageCode)
        {
            var parts = normalizedLanguageCode.Split('-', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return string.Empty;

            parts[0] = parts[0].ToLowerInvariant();
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 2 || parts[i].Length == 3)
                    parts[i] = parts[i].ToUpperInvariant();
                else
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
            }

            return string.Join("-", parts);
        }

        private static void AddUnique(List<string> candidates, string relativePath)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], relativePath, System.StringComparison.OrdinalIgnoreCase))
                    return;
            }

            candidates.Add(relativePath);
        }
    }
}
