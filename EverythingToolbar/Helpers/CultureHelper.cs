using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace EverythingToolbar.Helpers
{
    public static class CultureHelper
    {
        /// <summary>
        /// Dynamically gets supported language codes by scanning for .resx files.
        /// </summary>
        private static string[] GetSupportedLanguageCodes()
        {
            try
            {
                // Get the Properties directory where .resx files are located
                string propertiesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Properties");
                
                if (!Directory.Exists(propertiesDir))
                {
                    // Fallback to app directory if Properties doesn't exist
                    propertiesDir = AppDomain.CurrentDomain.BaseDirectory;
                }

                // Find all Resources.*.resx files (excluding base Resources.resx)
                var resx = Directory.GetFiles(propertiesDir, "Resources.*.resx")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Select(f => f.Replace("Resources.", ""))
                    .Where(f => !string.IsNullOrEmpty(f))
                    .OrderBy(f => f)
                    .ToArray();

                return resx.Length > 0 ? resx : GetFallbackLanguages();
            }
            catch
            {
                // If scanning fails, use fallback list
                return GetFallbackLanguages();
            }
        }

        /// <summary>
        /// Fallback list of languages if file scanning fails.
        /// </summary>
        private static string[] GetFallbackLanguages()
        {
            return new[]
            {
                "af", "ar", "ca", "cs", "da", "de", "el", "es", "fa", "fi", 
                "fr", "he", "hu", "it", "ja", "ko-KR", "nl", "no", "pl", "pt", 
                "pt-BR", "ro", "ru", "sr", "sv", "tr", "ug", "uk", "uz", "vi", 
                "zh", "zh-Hans"
            };
        }

        /// <summary>
        /// Gets list of available languages as display-friendly KeyValuePairs.
        /// </summary>
        public static List<KeyValuePair<string, string>> GetAvailableLanguages()
        {
            var languages = new List<KeyValuePair<string, string>>
            {
                new("Use System Language", "")
            };

            // Always include English first
            var englishCulture = GetCultureInfo("en");
            if (englishCulture != null)
            {
                languages.Add(new("English", "en"));
            }

            foreach (var code in GetSupportedLanguageCodes())
            {
                // Skip English since we already added it
                if (code.Equals("en", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cultureInfo = GetCultureInfo(code);
                if (cultureInfo != null)
                {
                    // Display name: "English (English)" or "Deutsch (German)" for non-English
                    var displayName = cultureInfo.NativeName;
                    if (!string.Equals(cultureInfo.NativeName, cultureInfo.EnglishName, StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = $"{cultureInfo.NativeName} ({cultureInfo.EnglishName})";
                    }

                    languages.Add(new(displayName, code));
                }
            }

            return languages;
        }

        /// <summary>
        /// Gets a CultureInfo for the given language code, or null if invalid.
        /// </summary>
        public static CultureInfo? GetCultureInfo(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return null;
            }

            try
            {
                return CultureInfo.GetCultureInfo(languageCode);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Applies the UI language based on the saved setting.
        /// If empty/system, uses current system UI culture.
        /// </summary>
        public static void ApplyUILanguage(string? languageCode)
        {
            CultureInfo culture;

            if (string.IsNullOrEmpty(languageCode))
            {
                // Use system culture
                culture = CultureInfo.CurrentUICulture;
            }
            else
            {
                culture = GetCultureInfo(languageCode);
                if (culture == null)
                {
                    // Fallback to system culture if invalid
                    culture = CultureInfo.CurrentUICulture;
                }
            }

            // Apply culture to current thread
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
