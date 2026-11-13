using System;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleText
    {
        private readonly ITranslationHelper translation;

        internal DebugConsoleText(ITranslationHelper translation)
        {
            this.translation = translation;
        }

        internal string Language => translation.Language;

        internal string Get(string key, string fallback)
        {
            string translated = translation.Get(
                key,
                string.Empty);
            return string.IsNullOrWhiteSpace(translated) ||
                translated.Equals(
                    key,
                    StringComparison.OrdinalIgnoreCase)
                ? fallback
                : translated;
        }

        internal string Failure(string reason)
        {
            switch (reason)
            {
                case "no-current-save-session":
                    return Get("debug.error.save", "Load a save first.");
                case "unknown-item":
                case "unknown-animal-card":
                case "unknown-monster":
                    return Get("debug.error.content", "This content is not available in the current game.");
                case "animal-postcondition-failed":
                    return Get("debug.animal.incomplete", "The animal was created but could not be fully prepared. See diagnostics.");
                case "native-host-unavailable":
                case "native-animal-home-unavailable":
                    return Get("debug.error.room", "The current room cannot perform this action.");
                case "partial-inventory-full":
                case "inventory-full":
                    return Get("debug.items.inventoryFull", "The backpack is full.");
                default:
                    return Get("debug.error.failed", "The action could not be completed. See diagnostics.");
            }
        }
    }
}
