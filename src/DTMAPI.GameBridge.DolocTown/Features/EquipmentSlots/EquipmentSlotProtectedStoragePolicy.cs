using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class EquipmentSlotProtectedStoragePolicy
    {
        public const int SchemaVersion = 2;
        private const long SaveClockRegressionToleranceSeconds = 300;

        public static string BuildSaveScopeKey(int archiveIndex)
        {
            return archiveIndex >= 0
                ? "slot-" + archiveIndex.ToString(CultureInfo.InvariantCulture)
                : "slot-unknown";
        }

        public static string MakeSafePathSegment(string value)
        {
            value ??= string.Empty;
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        }

        public static int GetTailIndexFromEnd(int index, int totalSlots)
        {
            if (index < 0 || totalSlots <= 0)
                return -1;
            return Math.Max(0, totalSlots - 1 - index);
        }

        public static IEnumerable<T> OrderTailFirst<T>(IEnumerable<T> entries, Func<T, int> getIndex)
        {
            if (entries == null)
                return Array.Empty<T>();
            return entries.OrderByDescending(entry => getIndex(entry));
        }

        public static bool IsStorageCompatible(
            int documentArchiveIndex,
            string documentPlayerName,
            string documentCustomPlayerName,
            long documentTotalGameSeconds,
            int currentArchiveIndex,
            string currentPlayerName,
            string currentCustomPlayerName,
            long currentTotalGameSeconds,
            out string reason)
        {
            reason = string.Empty;
            if (currentArchiveIndex < 0)
            {
                reason = "missing-current-archive";
                return false;
            }

            if (documentArchiveIndex >= 0 && documentArchiveIndex != currentArchiveIndex)
            {
                reason = "archive-index-mismatch document=" + documentArchiveIndex.ToString(CultureInfo.InvariantCulture) + " current=" + currentArchiveIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            string documentName = FirstText(documentCustomPlayerName, documentPlayerName);
            string currentName = FirstText(currentCustomPlayerName, currentPlayerName);
            if (!string.IsNullOrWhiteSpace(documentName) &&
                !string.IsNullOrWhiteSpace(currentName) &&
                !string.Equals(documentName, currentName, StringComparison.OrdinalIgnoreCase))
            {
                reason = "player-name-mismatch document=" + documentName + " current=" + currentName;
                return false;
            }

            if (documentTotalGameSeconds >= 0 &&
                currentTotalGameSeconds >= 0 &&
                currentTotalGameSeconds + SaveClockRegressionToleranceSeconds < documentTotalGameSeconds)
            {
                reason = "total-game-seconds-regressed document=" + documentTotalGameSeconds.ToString(CultureInfo.InvariantCulture) + " current=" + currentTotalGameSeconds.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            return true;
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }
    }
}
