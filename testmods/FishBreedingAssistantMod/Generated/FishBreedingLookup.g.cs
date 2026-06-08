using System;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    internal sealed class FishRoeInfo
    {
        public string FishId { get; set; } = string.Empty;

        public string FishTitle { get; set; } = string.Empty;

        public string RoeTitle { get; set; } = string.Empty;

        public string IncubateText { get; set; } = string.Empty;

        public string GrowText { get; set; } = string.Empty;

        public string ParentSummary { get; set; } = string.Empty;
    }

    internal static class FishBreedingLookup
    {
        public static bool TryGet(string fishId, out FishRoeInfo info)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                info = Empty;
                return false;
            }

            info = Empty;
            return false;
        }

        private static readonly FishRoeInfo Empty = new FishRoeInfo();
    }
}
