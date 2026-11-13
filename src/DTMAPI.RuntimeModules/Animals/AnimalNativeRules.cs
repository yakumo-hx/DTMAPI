using System;
using System.Text;
namespace DTMAPI.RuntimeModules.Animals
{
    internal static class AnimalNativeRules
    {
        internal static string MapSprite(string templateSpritePrefix, string customSpritePrefix, string oldSpriteName)
        {
            string source = oldSpriteName.Trim();
            string prefix = templateSpritePrefix + "_";
            if (!source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string suffix = source.Substring(prefix.Length);
            if (suffix.IndexOf("_jump_ready_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int jumpIndex = suffix.IndexOf("_jump_ready_", StringComparison.OrdinalIgnoreCase);
                return customSpritePrefix + "_" + suffix.Substring(0, jumpIndex) + "_jump_0";
            }

            if (suffix.EndsWith("_jump_ready", StringComparison.OrdinalIgnoreCase))
                return customSpritePrefix + "_" + suffix.Substring(0, suffix.Length - "_jump_ready".Length) + "_jump_0";

            return customSpritePrefix + "_" + ReplaceOrdinalIgnoreCase(suffix, "child_", "young_");
        }

        private static string ReplaceOrdinalIgnoreCase(string text, string oldValue, string newValue)
        {
            int index = text.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return text;

            var builder = new StringBuilder();
            int start = 0;
            while (index >= 0)
            {
                builder.Append(text, start, index - start);
                builder.Append(newValue);
                start = index + oldValue.Length;
                index = text.IndexOf(oldValue, start, StringComparison.OrdinalIgnoreCase);
            }

            builder.Append(text, start, text.Length - start);
            return builder.ToString();
        }

    }
}
