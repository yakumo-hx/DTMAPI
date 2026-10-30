using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static void SpawnBoundaryChecks()
    {
        var points = Enumerable.Range(0, 6).Select(i => ("Air/region", (float)i * 3, 20f, i, 6)).ToArray();
        var middle = SpawnSlotAssignment.Resolve(points, 3, 20, 0, 0, 1);
        var end = SpawnSlotAssignment.Resolve(points, 12, 20, 0, 0, 1);
        Check(middle.Slots.SequenceEqual(new[] { "Air/region:1" }), "A shuffled native allocation occupies its actual non-first slot.");
        Check(end.Slots.SequenceEqual(new[] { "Air/region:4" }), "Noncontiguous native occupancy does not consume earlier free slots.");
        var wide = SpawnSlotAssignment.Resolve(points, 6.5f, 19, .5f, -1, 2);
        Check(wide.Slots.SequenceEqual(new[] { "Air/region:2", "Air/region:3" }), "Native Volume consumes consecutive slots at the actual offset-adjusted start.");
        Check(SpawnSlotAssignment.Resolve(points, 15, 20, 0, 0, 2).Diagnostic != null, "A volume cannot extend beyond its native group.");
        Check(SpawnSlotAssignment.Resolve(points.Concat(points), 3, 20, 0, 0, 1).Diagnostic != null, "Ambiguous region/position mappings are diagnosed, not guessed.");
        Check(SpawnSlotAssignment.Resolve(points, 3.5f, 20, 0, 0, 1).Slots.Length == 0, "Moved or unmatched coordinates never become a nearest-slot allocation.");
        var ledger = new EncounterLedger();
        ledger.TryBegin("A"); ledger.TryBegin("B"); long original = ledger.Generation;
        ledger.BeginRebuild();
        Check(ledger.Generation == original && !ledger.TryBegin("C"), "Requesting regeneration preserves the old generation and blocks all creation.");
        ledger.EndRebuild(false);
        Check(ledger.Generation == original && !ledger.TryBegin("A") && !ledger.TryBegin("B"), "Failure before clearing preserves both owners' consumed attempts.");
        ledger.BeginRebuild(); ledger.ConfirmClear();
        Check(ledger.Generation == original + 1 && !ledger.TryBegin("A"), "A confirmed clear advances once without opening an unfinished batch.");
        ledger.EndRebuild(false);
        Check(ledger.RecoveryPending && !ledger.TryBegin("A"), "Failure after clearing cannot be resumed by ready/render.");
        ledger.BeginRebuild(); ledger.EndRebuild(true);
        Check(ledger.RecoveryPending, "A no-op success cannot clear an incomplete rebuild.");
        ledger.BeginRebuild(); ledger.ConfirmClear(); ledger.BeginRebuild(); ledger.ConfirmClear(); ledger.EndRebuild(true);
        Check(ledger.Generation == original + 2 && !ledger.TryBegin("A"), "Nested native clears share a generation; inner Render remains blocked.");
        ledger.EndRebuild(true);
        Check(!ledger.RecoveryPending && ledger.TryBegin("A") && ledger.TryBegin("B") && !ledger.TryBegin("A"), "Successful outer completion grants one batch per owner.");
        ledger.BeginRebuild(); ledger.ConfirmClear(); ledger.EndRebuild(true);
        Check(ledger.Generation == original + 3 && ledger.TryBegin("A"), "Confirmed empty-list refresh is also one real rebuild opportunity.");
        ledger.BeginRebuild(); ledger.BeginRebuild(); ledger.ConfirmClear(); ledger.EndRebuild(false); ledger.EndRebuild(true);
        Check(ledger.RecoveryPending && !ledger.TryBegin("A"), "Caught nested failures cannot silently reopen the room.");
    }
}
