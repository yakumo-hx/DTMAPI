using System;
using System.Collections.Generic;

namespace Yuuka.DTMAPI.ActionSpeed;

internal interface IAnimationClock { float Speed { get; set; } bool IsAlive { get; } }

// One lease per bound Animator; reused across actions without per-frame allocation.
internal sealed class SpeedLease
{
    private readonly IAnimationClock clock;
    private float original, written;
    private bool owned;
    internal SpeedLease(IAnimationClock clock) => this.clock = clock;
    internal void Apply(float factor)
    {
        Restore();
        if (!clock.IsAlive || factor <= 1) return;
        original = clock.Speed;
        written = original * factor;
        owned = true;
        clock.Speed = written;
    }
    internal void Restore()
    {
        if (!owned) return;
        if (clock.IsAlive && clock.Speed.Equals(written)) clock.Speed = original;
        // A failed getter/setter must leave this lease available for the next cleanup.
        owned = false;
    }

    internal static void RestoreAll(IEnumerable<SpeedLease> clocks)
    {
        List<Exception>? failures = null;
        foreach (var clock in clocks)
            try { clock.Restore(); }
            catch (Exception ex) { (failures ??= new List<Exception>()).Add(ex); }
        if (failures != null) throw new AggregateException("ActionSpeed animation restoration failed; retry remains available.", failures);
    }
}
