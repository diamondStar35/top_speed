using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Localization
{
    internal sealed class ClientLanguage
    {
        public ClientLanguage(string code, string englishName, string nativeName, bool isOriginal = false)
        {
            Code = string.IsNullOrWhiteSpace(code) ? "en" : code;
            EnglishName = string.IsNullOrWhiteSpace(englishName) ? Code : englishName;
            NativeName = string.IsNullOrWhiteSpace(nativeName) ? EnglishName : nativeName;
            IsOriginal = isOriginal;
        }

        public string Code { get; }
        public string EnglishName { get; }
        public string NativeName { get; }
        public bool IsOriginal { get; }

        public string ListLabel
        {
            get
            {
                if (IsOriginal)
                    return LocalizationService.Translate(LocalizationService.Mark("English (Original)"));
                if (string.Equals(EnglishName, NativeName, StringComparison.CurrentCultureIgnoreCase))
                    return EnglishName;
                return $"{EnglishName} ({NativeName})";
            }
        }

        public string SettingsLabel => IsOriginal
            ? LocalizationService.Translate(LocalizationService.Mark("English (Original)"))
            : NativeName;
    }

    internal static class ClientLanguages
    {
        private const string DefaultCode = "en";

        public static IReadOnlyList<ClientLanguage> Load()
        {
            var languages = new List<ClientLanguage>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var root = LocalizationBootstrap.ResolveLanguagesRoot(LocalizationBootstrap.ClientCatalogGroup);
            if (Directory.Exists(root))
            {
                foreach (var directory in Directory.GetDirectories(root))
                {
                    var code = NormalizeCode(Path.GetFileName(directory));
                    if (string.IsNullOrWhiteSpace(code))
                        continue;
                    if (!File.Exists(Path.Combine(directory, "messages.mo")))
                        continue;
                    if (string.Equals(code, DefaultCode, StringComparison.OrdinalIgnoreCase))
                    {
                        seen.Add(DefaultCode);
                        continue;
                    }
                    if (!seen.Add(code))
                        continue;
                    languages.Add(BuildLanguage(code));
                }
            }

            if (!seen.Contains(DefaultCode))
                seen.Add(DefaultCode);
            languages.Add(BuildOriginalEnglishLanguage());

            languages.Sort(CompareLanguages);
            return languages;
        }

        public static string ResolveCode(string? languageCode, IReadOnlyList<ClientLanguage>? availableLanguages)
        {
            var match = ResolveLanguage(languageCode, availableLanguages);
            if (match != null)
                return match.Code;

            return DefaultCode;
        }

        public static ClientLanguage? ResolveLanguage(string? languageCode, IReadOnlyList<ClientLanguage>? availableLanguages)
        {
            if (availableLanguages == null || availableLanguages.Count == 0)
                return null;

            var normalized = LanguageCode.Normalize(languageCode);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var exact = FindByCode(availableLanguages, normalized);
                if (exact != null)
                    return exact;

                var parent = LanguageCode.ParentOf(normalized);
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    var parentMatch = FindByCode(availableLanguages, parent);
                    if (parentMatch != null)
                        return parentMatch;
                }
            }

            var defaultMatch = FindByCode(availableLanguages, DefaultCode);
            if (defaultMatch != null)
                return defaultMatch;

            return availableLanguages[0];
        }

        public static string ResolveSettingsLabel(string? languageCode, IReadOnlyList<ClientLanguage>? availableLanguages)
        {
            var language = ResolveLanguage(languageCode, availableLanguages);
            if (language != null)
                return language.SettingsLabel;

            return BuildOriginalEnglishLanguage().SettingsLabel;
        }

        private static ClientLanguage? FindByCode(IReadOnlyList<ClientLanguage> languages, string languageCode)
        {
            for (var i = 0; i < languages.Count; i++)
            {
                var language = languages[i];
                if (string.Equals(language.Code, languageCode, StringComparison.OrdinalIgnoreCase))
                    return language;
            }

            return null;
        }

        private static int CompareLanguages(ClientLanguage left, ClientLanguage right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left == null)
                return -1;
            if (right == null)
                return 1;

            var byEnglish = string.Compare(left.EnglishName, right.EnglishName, StringComparison.OrdinalIgnoreCase);
            if (byEnglish != 0)
                return byEnglish;
            return string.Compare(left.Code, right.Code, StringComparison.OrdinalIgnoreCase);
        }

        private static ClientLanguage BuildLanguage(string languageCode)
        {
            var code = LanguageCode.Normalize(languageCode);
            var englishName = code;
            var nativeName = code;

            var culture = LanguageCode.TryResolveCulture(code);
            if (culture != null)
            {
                englishName = string.IsNullOrWhiteSpace(culture.EnglishName) ? code : culture.EnglishName;
                nativeName = string.IsNullOrWhiteSpace(culture.NativeName) ? englishName : culture.NativeName;
            }

            return new ClientLanguage(code, englishName, nativeName);
        }

        private static ClientLanguage BuildOriginalEnglishLanguage()
        {
            return new ClientLanguage(DefaultCode, "English", "English", isOriginal: true);
        }

        private static string NormalizeCode(string? languageCode) => LanguageCode.Normalize(languageCode);
    }
}

