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
            StableRowsAreSortedOnceAndCappedAtThree();
            OverlayWritesOnlyOneNextFrameGuard();
            OverlayCleanupIsReverseOrderAndExactlyOnce();
        }

        private static void StableRowsAreSortedOnceAndCappedAtThree()
        {
            TestRow[] result = AnimalStableRowPlan.CreateTopRows(
                new[]
                {
                    new TestRow("zeta", 0.4),
                    new TestRow("beta", 0.9),
                    new TestRow("alpha", 0.9),
                    new TestRow("gamma", 0.7)
                },
                (left, right) =>
                {
                    int progress = right.Progress.CompareTo(left.Progress);
                    return progress != 0
                        ? progress
                        : StringComparer.OrdinalIgnoreCase.Compare(left.Title, right.Title);
                },
                maximumRows: 3);

            Assert(result.Select(row => row.Title).SequenceEqual(new[] { "alpha", "beta", "gamma" }, StringComparer.Ordinal),
                "Animal stable rows should sort once by descending progress and stable title, then retain exactly three.");
        }

        private static void OverlayWritesOnlyOneNextFrameGuard()
        {
            var session = new AnimalOverlaySessionState<TestClone>();
            session.Track(new TestClone("one"));
            session.Track(new TestClone("two"));
            session.ArmNextFrameGuard();

            int writes = 1;
            if (session.TryConsumeNextFrameGuard())
                writes++;
            for (int i = 0; i < 100; i++)
                if (session.TryConsumeNextFrameGuard())
                    writes++;

            Assert(writes == 2, "Animal overlay should perform one initial write and at most one next-frame guard write.");
            Assert(!session.NextFrameGuardPending, "Animal next-frame guard should be consumed after one update.");

            session.ArmNextFrameGuard();
            session.Clear(_ => { });
            Assert(!session.TryConsumeNextFrameGuard(), "Animal data/selection invalidation should cancel a pending guard.");
        }

        private static void OverlayCleanupIsReverseOrderAndExactlyOnce()
        {
            var session = new AnimalOverlaySessionState<TestClone>();
            session.Track(new TestClone("one"));
            session.Track(new TestClone("two"));
            session.Track(new TestClone("three"));
            var destroyed = new List<string>();

            int firstCount = session.Clear(clone => destroyed.Add(clone.Id));
            int secondCount = session.Clear(clone => destroyed.Add(clone.Id));

            Assert(firstCount == 3 && secondCount == 0, "Animal overlay cleanup should drain tracked clones exactly once.");
            Assert(destroyed.SequenceEqual(new[] { "three", "two", "one" }, StringComparer.Ordinal),
                "Animal overlay cleanup should destroy tracked clones in reverse construction order.");
            Assert(session.Count == 0, "Animal overlay cleanup should leave zero tracked clones.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TestRow
        {
            internal TestRow(string title, double progress)
            {
                Title = title;
                Progress = progress;
            }

            internal string Title { get; }
            internal double Progress { get; }
        }

        private sealed class TestClone
        {
            internal TestClone(string id) => Id = id;
            internal string Id { get; }
        }
    }
}
