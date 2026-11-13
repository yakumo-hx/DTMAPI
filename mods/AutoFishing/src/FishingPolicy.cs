namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingPhase { Other, Idle, Ready, Cast, Wait, Battle, Pull }
    internal enum NoteKind { Delay, Stable, Bonus }

    internal readonly struct FishingInput
    {
        public FishingInput(bool press, bool hold) { Press = press; Hold = hold; }
        public bool Press { get; }
        public bool Hold { get; }
    }

    internal sealed class NoteInputPolicy
    {
        private int clickedBonus = -1;
        public void Reset() => clickedBonus = -1;

        public FishingInput Read(NoteKind kind, int index, float time, float start, float end)
        {
            if (kind == NoteKind.Delay || time < start || time > end) return default;
            if (kind == NoteKind.Stable) return new FishingInput(false, true);
            if (index == clickedBonus) return default;
            clickedBonus = index;
            return new FishingInput(true, false);
        }
    }

    // Completion is a receipt from native NextState, not an inference from Idle.
    internal sealed class FishingLoop
    {
        public const float CastInterval = 0.18f;
        public bool Enabled { get; private set; }
        public bool InCast { get; private set; }
        private float nextCast;

        public void Enable(FishingPhase phase, float now)
        {
            Enabled = phase != FishingPhase.Other;
            InCast = Enabled && phase != FishingPhase.Idle;
            nextCast = now;
        }

        public void Stop() { Enabled = false; InCast = false; }
        public void Started() => InCast = true;
        public void Completed(float now) { InCast = false; nextCast = now + CastInterval; }

        public bool Observe(FishingPhase phase, float now)
        {
            if (!Enabled) return false;
            if (phase == FishingPhase.Other || (InCast && phase == FishingPhase.Idle))
            {
                Stop();
                return false;
            }
            return !InCast && phase == FishingPhase.Idle && now >= nextCast;
        }
    }
}
