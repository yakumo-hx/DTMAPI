using System;
using System.Collections.Generic;
using System.Linq;
using Yuuka.DTMAPI.AnimalHusbandryProgress;

namespace DTMAPI.UnitTests
{
    internal static class AnimalHusbandryProductTests
    {
        internal static void RunAll()
        {
            AllValidRowsRemainSortedAndReadable();
            RowPoolReusesInstancesAcrossSelections();
            FailedAllocationPreservesExistingRows();
            CleanupContinuesAndRetriesOnlyFailures();
            ColorDraftDoesNotMutateAppliedConfig();
        }
        private static void AllValidRowsRemainSortedAndReadable()
        {
            var rows = new[]
            {
                new AnimalProgressRow("c", "桥动物的自定义隐藏产物", 0, 40),
                new AnimalProgressRow("b", "Beta", 60, 60),
                new AnimalProgressRow("a", "Alpha", 60, 60),
                new AnimalProgressRow("d", "High", 125, 100),
                new AnimalProgressRow("e", "Negative", -1, 40)
            };
            var result = AnimalStableRowPlan.CreateRows(rows, AnimalProgressRow.Compare);
            Assert(result.Select(x => x.Id).SequenceEqual(new[] { "d", "a", "b", "c", "e" }), "All five rows survive; actual ratio, then title controls ordering.");
            Assert(result[0].Fill == 1 && result[0].Text == "125/100" && result[4].Fill == 0 && result[4].Text == "-1/40", "Only the drawn bar is clamped; actual values remain visible.");
            Assert(result[3].Title == "桥动物的自定义隐藏产物", "Custom labels are not rewritten as IDs.");
            Assert(AnimalStableRowPlan.CreateRows(Array.Empty<AnimalProgressRow>(), AnimalProgressRow.Compare).Length == 0, "No products yields no invented row.");
            foreach (int threshold in new[] { 0, -1 })
            {
                bool rejected = false;
                try { _ = new AnimalProgressRow("x", "X", 1, threshold); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Assert(rejected, "Invalid threshold cannot become a plausible progress bar.");
            }
        }
        private static void RowPoolReusesInstancesAcrossSelections()
        {
            var pool = new AnimalOverlaySessionState<object>();
            int created = 0;
            Func<int, object> create = _ => { created++; return new object(); };
            pool.EnsureCount(4, create);
            object first = pool.Tracked[0];
            for (int i = 0; i < 100; i++) { pool.EnsureCount(2, create); pool.EnsureCount(4, create); }
            Assert(created == 4 && ReferenceEquals(first, pool.Tracked[0]), "Switching animals must reuse rows without allocation or replacement.");
            pool.EnsureCount(6, create);
            Assert(created == 6 && pool.Count == 6 && ReferenceEquals(first, pool.Tracked[0]), "Only additional visible capacity allocates rows.");
        }
        private static void FailedAllocationPreservesExistingRows()
        {
            var pool = new AnimalOverlaySessionState<object>();
            var first = new object(); pool.Track(first);
            bool failed = false;
            try { pool.EnsureCount(3, index => index == 2 ? throw new InvalidOperationException("create") : new object()); }
            catch (InvalidOperationException) { failed = true; }
            Assert(failed && pool.Count == 2 && ReferenceEquals(first, pool.Tracked[0]), "Successfully created rows retain their cleanup owner after later allocation failure.");
            Assert(pool.Clear(_ => { }) == 2 && pool.Count == 0, "Failure can release every owned prior row.");
        }
        private static void CleanupContinuesAndRetriesOnlyFailures()
        {
            var pool = new AnimalOverlaySessionState<string>();
            pool.Track("one"); pool.Track("two"); pool.Track("three");
            var attempts = new List<string>();
            bool failed = false;
            try { pool.Clear(item => { attempts.Add(item); if (item == "two") throw new InvalidOperationException("destroy"); }); }
            catch (AggregateException ex) { failed = ex.InnerExceptions.Count == 1; }
            Assert(failed && attempts.SequenceEqual(new[] { "three", "two", "one" }), "One failure cannot prevent other required destruction.");
            Assert(pool.Count == 1 && pool.Tracked[0] == "two", "Only failed disposal remains owned.");
            Assert(pool.Clear(item => attempts.Add(item)) == 1 && pool.Clear(_ => throw new Exception()) == 0, "Retry releases once; repeated close is harmless.");
        }
        private static void ColorDraftDoesNotMutateAppliedConfig()
        {
            var draft = new AnimalHusbandryConfig();
            var applied = draft.Copy();
            draft.ColorPreset = "Custom"; draft.ProgressColorHex = "#123abc"; draft.Normalize();
            Assert(applied.ProgressColorHex == "FF942E" && draft.ProgressColorHex == "123ABC", "Pending edits cannot mutate the applied copy.");
            draft.ProgressColorHex = "bad"; draft.Normalize();
            Assert(draft.ProgressColorHex == "FF942E", "Invalid custom colors use the declared default.");
            draft.ColorPreset = "Blue"; draft.Normalize();
            Assert(draft.ProgressColorHex == "65A6FF", "Named preset supplies its actual fill color.");
        }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
