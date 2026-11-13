using System;
using System.Globalization;

namespace DTMAPI.Internal
{
    // Shared source: preparation remains independent of the Runtime contract.
    internal static class AudioVolume
    {
        internal static bool Valid(float db) => !float.IsNaN(db) && db != float.PositiveInfinity &&
            db <= 20 * Math.Log10(float.MaxValue);
        internal static float Gain(float declarationDb, float offsetDb = 0)
        {
            if (!Valid(declarationDb) || !Valid(offsetDb)) throw new ArgumentOutOfRangeException("volumeDb");
            if (float.IsNegativeInfinity(declarationDb) || float.IsNegativeInfinity(offsetDb)) return 0;
            float gain = (float)Math.Pow(10, ((double)declarationDb + offsetDb) / 20);
            if (float.IsInfinity(gain)) throw new ArgumentOutOfRangeException("volumeDb", "Combined volume exceeds the audio engine's numeric range.");
            return gain;
        }
        internal static float Parse(string text)
        {
            if (text == "-inf") return float.NegativeInfinity;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float db) ||
                float.IsInfinity(db) || !Valid(db)) throw new ArgumentException("Volume must be a dB number or -inf.", "volumeDb");
            return db;
        }
        internal static string Json(float db)
        {
            if (!Valid(db)) throw new ArgumentOutOfRangeException("volumeDb");
            return float.IsNegativeInfinity(db) ? "\"-inf\"" : db.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
