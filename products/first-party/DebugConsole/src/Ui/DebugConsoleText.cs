using System;
using System.Globalization;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleText
    {
        private readonly ITranslationHelper translation;

        internal DebugConsoleText(
            ITranslationHelper translation,
            string requestedLanguage = "")
        {
            this.translation = translation ??
                throw new ArgumentNullException(nameof(translation));
            Language = ResolveLanguage(
                requestedLanguage,
                translation.Language);
        }

        internal string Language { get; }

        internal string Get(string key, string fallback)
        {
            string translated = translation.Get(
                key ?? string.Empty,
                string.Empty);
            return string.IsNullOrWhiteSpace(translated) ||
                translated.Equals(
                    key ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                ? fallback ?? string.Empty
                : translated;
        }

        private static string ResolveLanguage(
            string requested,
            string current)
        {
            string normalized =
                (requested ?? string.Empty)
                    .Trim()
                    .Replace('-', '_')
                    .ToLowerInvariant();
            if (normalized == "english" ||
                normalized == "en" ||
                normalized.StartsWith(
                    "en_",
                    StringComparison.Ordinal))
            {
                return "english";
            }
            if (normalized == "schinese" ||
                normalized == "zh" ||
                normalized == "zh_cn" ||
                normalized == "chinese")
            {
                return "schinese";
            }
            string detected =
                (current ?? CultureInfo.CurrentUICulture.Name)
                    .Trim()
                    .Replace('-', '_')
                    .ToLowerInvariant();
            return detected.StartsWith(
                "zh",
                StringComparison.Ordinal)
                ? "schinese"
                : "english";
        }
    }
}
