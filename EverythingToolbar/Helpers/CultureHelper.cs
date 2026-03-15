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
        /// Dynamically gets supported language codes by scanning for satellite assemblies.
        /// </summary>
        private static string[] GetSupportedLanguageCodes()
        {
            try
            {
                var assembly = typeof(CultureHelper).Assembly;
                var baseDir = Path.GetDirectoryName(assembly.Location);
                if (string.IsNullOrEmpty(baseDir)) return Array.Empty<string>();

                var assemblyName = "EverythingToolbar.resources.dll";

                return Directory.GetDirectories(baseDir)
                    .Select(Path.GetFileName)
                    .Where(name => 
                    {
                        if (name == null) return false;
                        try
                        {
                            _ = CultureInfo.GetCultureInfo(name);
                            return File.Exists(Path.Combine(baseDir, name, assemblyName));
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .OrderBy(c => c)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Gets list of available languages as display-friendly KeyValuePairs.
        /// </summary>
        public static List<KeyValuePair<string, string>> GetAvailableLanguages()
        {
            var languages = new List<KeyValuePair<string, string>>
            {
                new(Properties.Resources.SettingsUseSystemLanguage, "")
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
