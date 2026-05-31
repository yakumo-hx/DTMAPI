using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed class TranslationService : ITranslationHelper
    {
        private readonly Dictionary<string, string> primary;
        private readonly Dictionary<string, string> english;

        public TranslationService(string modRootPath, string language)
        {
            Language = NormalizeLanguage(language);
            string i18nPath = Path.Combine(modRootPath, "i18n");
            primary = ReadDictionary(Path.Combine(i18nPath, Language + ".json"));
            english = Language.Equals("english", StringComparison.OrdinalIgnoreCase)
                ? primary
                : ReadDictionary(Path.Combine(i18nPath, "english.json"));
        }

        public string Language { get; }

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;
            if (primary.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return value;
            if (english.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                return value;
            return string.IsNullOrWhiteSpace(fallback) ? key : fallback;
        }

        public static string DetectLanguage()
        {
            string requested = Environment.GetEnvironmentVariable("DTMAPI_LANGUAGE") ??
                Environment.GetEnvironmentVariable("DTMAPI_UI_LANGUAGE") ??
                string.Empty;
            if (!string.IsNullOrWhiteSpace(requested))
                return NormalizeLanguage(requested);

            CultureInfo culture = CultureInfo.CurrentUICulture;
            if (culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "english";
            return "schinese";
        }

        public static string NormalizeLanguage(string language)
        {
            language = (language ?? string.Empty).Trim().Replace('-', '_').ToLowerInvariant();
            if (language == "en" || language == "en_us" || language == "en_gb" || language == "english")
                return "english";
            return "schinese";
        }

        private static Dictionary<string, string> ReadDictionary(string path)
        {
            if (!File.Exists(path))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings
                    {
                        UseSimpleDictionaryFormat = true
                    });
                    return serializer.ReadObject(stream) is Dictionary<string, string> values
                        ? new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
