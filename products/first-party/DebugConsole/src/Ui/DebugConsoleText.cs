using System;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleText
    {
        private readonly ITranslationHelper translation;

        internal DebugConsoleText(ITranslationHelper translation)
        {
            this.translation = translation ??
                throw new ArgumentNullException(nameof(translation));
        }

        internal string Language => translation.Language ?? "english";

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
    }
}
