using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void PlatformContextInvalidatesBeforeCallbacksAndRejectsFailedReadiness()
        {
            var fixture = new PlatformFixture();
            var runtime = fixture.Runtime;
            var context = fixture.Service<IDtmRuntimeContext>("A");
            var scheduler = fixture.Service<IDtmScheduler>("A");
            runtime.ReturnToTitle(true);
            Assert(scheduler.Post(() => throw new Exception(), DtmWorkScope.CurrentWorld).Status == DtmWorkStatus.Rejected, "Title cannot capture a world scope.");
            long failed = runtime.BeginSaveAttempt();
            runtime.ObserveAfterLoad();
            runtime.CompleteSaveAttempt(false);
            runtime.ObserveWorldReady();
            Assert(!context.Snapshot.IsWorldReady && !context.Snapshot.SaveSessionEpoch.HasValue, "Native false must invalidate even after a partial after-load observation.");
            long next = runtime.BeginSaveAttempt();
            Assert(next > failed, "Failure consumes a save generation.");
            runtime.ObserveWorldReady();
            Assert(!context.Snapshot.IsWorldReady, "Native presence before success/AfterLoad cannot make context ready.");
            runtime.ObserveAfterLoad(); runtime.CompleteSaveAttempt(true); runtime.ObserveWorldReady();
            var first = context.Snapshot;
            Assert(first.IsWorldReady && first.SaveSessionEpoch == next, "Fresh success plus native ready establishes the current world.");
            var world = scheduler.Delay(() => throw new Exception("old world executed"), TimeSpan.FromHours(1), DtmWorkScope.CurrentWorld);
            var save = scheduler.Delay(() => { }, TimeSpan.FromHours(1), DtmWorkScope.CurrentSaveSession);
            int notices = 0;
            using var subscription = context.Subscribe(snapshot =>
            {
                Assert(ReferenceEquals(snapshot, context.Snapshot), "Publish must precede the owner notification.");
                Assert(world.Status == DtmWorkStatus.Cancelled, "Old world work must be cancelled before notifications.");
                notices++;
            });
            runtime.BeginWorldTransition();
            Assert(context.Snapshot.Phase == DtmRuntimePhase.LoadingWorld && context.Snapshot.SaveSessionEpoch == next && save.Status == DtmWorkStatus.Queued, "Scene transition invalidates only world scope.");
            runtime.ObserveWorldReady();
            Assert(context.Snapshot.WorldEpoch > first.WorldEpoch && first.IsWorldReady, "New world epoch must increase without mutating old snapshots.");
            runtime.ReturnToTitle(false);
            Assert(save.Status == DtmWorkStatus.Cancelled && context.Snapshot.Phase == DtmRuntimePhase.ReturningToTitle && notices == 3, "Title request cancels save work before native cleanup.");
            var background = Task.Run(() => (context.Snapshot, context.IsMainThread)).GetAwaiter().GetResult();
            Assert(!background.IsMainThread && ReferenceEquals(background.Snapshot, context.Snapshot), "Background snapshot reads must avoid native/thread-dependent data.");
            AssertThrows(() => Task.Run(() => runtime.BeginSaveAttempt()).GetAwaiter().GetResult(), "Background lifecycle mutation must reject.");
        }

        private static void PlatformSchedulerHonorsCutoffTickFairnessCapacityAndBudget()
        {
            var idle = new PlatformFixture();
            idle.Service<IDtmScheduler>("idle-owner");
            for (ulong tick = 1; tick <= 1024; tick++) { idle.Runtime.BeginTick(tick); idle.Runtime.Drain(); }
            long beforeIdle = GC.GetAllocatedBytesForCurrentThread();
            for (ulong tick = 1025; tick <= 11024; tick++) { idle.Runtime.BeginTick(tick); idle.Runtime.Drain(); }
            Assert(GC.GetAllocatedBytesForCurrentThread() == beforeIdle,
                "An activated but idle scheduler owner must not allocate across 10k frames.");
            var f = new PlatformFixture();
            var a = f.Service<IDtmScheduler>("A");
            var b = f.Service<IDtmScheduler>("B");
            var order = new List<string>();
            f.Runtime.BeginTick(1);
            a.NextTick(() => order.Add("next"));
            a.Post(() => { order.Add("post"); a.Post(() => order.Add("nested")); f.Runtime.Drain(); });
            f.Runtime.Drain(); f.Runtime.Drain();
            Assert(order.SequenceEqual(new[] { "post" }), "NextTick and callback submissions cannot run in the same drain, including reentrant/duplicate drains.");
            f.Runtime.BeginTick(2); f.Runtime.Drain();
            Assert(order.SequenceEqual(new[] { "post", "next", "nested" }), "Next tick must release eligible work in same-owner FIFO order.");
            f.Runtime.OwnerCapacity = 3; f.Runtime.GlobalCapacity = 4; f.Runtime.FrameStartLimit = 2;
            var delayed = a.Delay(() => order.Add("delayed"), TimeSpan.FromMilliseconds(100));
            a.Post(() => order.Add("a1")); a.Post(() => order.Add("a2"));
            Assert(a.Post(() => { }).Completion.Result.ErrorCode == "queue-full", "Delayed items count toward owner capacity.");
            b.Post(() => order.Add("b1"));
            Assert(b.Post(() => { }).Completion.Result.ErrorCode == "queue-full", "Global capacity cannot silently overwrite work.");
            f.Runtime.BeginTick(3); f.Runtime.Drain();
            Assert(order.Skip(3).SequenceEqual(new[] { "b1", "a1" }), "Persistent round-robin fairness must admit B despite A flood and A's future delay.");
            f.Runtime.BeginTick(4); f.Runtime.Drain();
            Assert(order.Last() == "a2" && delayed.Status == DtmWorkStatus.Queued, "Future delay must not block ready same-owner work.");
            var expired = b.Delay(() => throw new Exception(), TimeSpan.FromDays(1), queueTimeout: TimeSpan.FromMilliseconds(10));
            f.Now = 100; f.Runtime.BeginTick(5); f.Runtime.Drain();
            Assert(delayed.Status == DtmWorkStatus.Succeeded && expired.Status == DtmWorkStatus.Expired, "Monotonic deadline and delay are independent.");
            a.Post(() => { order.Add("slow"); f.Now += 3; }); a.Post(() => order.Add("later"));
            f.Runtime.BeginTick(6); f.Runtime.Drain();
            Assert(order.Last() == "slow" && f.Runtime.PendingWorkCount == 1, "Elapsed budget prevents another start, never interrupts running work.");
            f.Runtime.BeginTick(7); f.Runtime.Drain();
            Assert(order.Last() == "later" && f.Runtime.PendingWorkCount == 0, "Budget-deferred work remains queued.");
        }

        private static void PlatformSchedulerCancelsRacesAndCompletesAsynchronously()
        {
            var f = new PlatformFixture();
            var scheduler = f.Service<IDtmScheduler>("A");
            using var cancelled = new CancellationTokenSource();
            var pending = scheduler.Delay(() => throw new Exception(), TimeSpan.FromDays(1), cancellationToken: cancelled.Token);
            Task.Run(() => cancelled.Cancel()).GetAwaiter().GetResult();
            Assert(pending.Status == DtmWorkStatus.Cancelled && !pending.TryCancel() && f.Runtime.PendingWorkCount == 0, "Background cancellation must have one terminal result and remove delayed work.");
            using var began = new ManualResetEventSlim();
            using var raced = new ManualResetEventSlim();
            bool couldCancel = true;
            IDtmScheduledWork? running = null;
            running = scheduler.Post(() => { began.Set(); Assert(raced.Wait(2000), "Cancellation worker did not respond."); });
            var competitor = Task.Run(() => { Assert(began.Wait(2000), "Work did not begin."); couldCancel = running.TryCancel(); raced.Set(); });
            int completionThread = 0;
            var continuation = running.Completion.ContinueWith(_ => completionThread = Thread.CurrentThread.ManagedThreadId, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            f.Runtime.BeginTick(1); f.Runtime.Drain(); competitor.GetAwaiter().GetResult(); continuation.GetAwaiter().GetResult();
            Assert(!couldCancel && running.Status == DtmWorkStatus.Succeeded && completionThread != f.MainThread, "Running work cannot be cancelled and completion continuations cannot inline on its Runtime callback.");
            var failed = scheduler.Post(() => throw new InvalidOperationException(new string('x', 2000)));
            int good = 0;
            var success = f.Service<IDtmScheduler>("B").Post(() => good++);
            f.Runtime.BeginTick(2); f.Runtime.Drain();
            Assert(failed.Status == DtmWorkStatus.Failed && failed.Completion.Result.Message.Length <= 512 && success.Status == DtmWorkStatus.Succeeded && good == 1 && f.Errors == 1, "A failed owner callback produces bounded diagnostics and cannot block another owner.");
            for (int index = 0; index < 30; index++)
            {
                using var token = new CancellationTokenSource();
                var item = scheduler.Post(() => { }, cancellationToken: token.Token);
                var race = Task.Run(() => token.Cancel());
                f.Runtime.BeginTick((ulong)index + 3); f.Runtime.Drain(); race.GetAwaiter().GetResult();
                Assert(item.Completion.IsCompletedSuccessfully && (item.Status == DtmWorkStatus.Cancelled || item.Status == DtmWorkStatus.Succeeded), "Cancel/start race must finish exactly once without deadlock.");
            }
        }

        private static void PlatformResourcesCloseInReverseOrderAndRetryFailures()
        {
            var f = new PlatformFixture();
            var resources = f.Service<IDtmOwnedResources>("A");
            var scheduler = f.Service<IDtmScheduler>("A");
            var context = f.Service<IDtmRuntimeContext>("A");
            var order = new List<int>();
            int attempts = 0;
            resources.Register(new PlatformDispose(() => order.Add(1)));
            var failed = resources.Register(new PlatformDispose(() => { order.Add(2); if (++attempts == 1) throw new Exception("once"); }));
            var detached = resources.Register(new PlatformDispose(() => throw new Exception("unregistered must not dispose")));
            Assert(detached.Unregister(), "Unregister releases ownership without disposal.");
            var queued = resources.Register(new PlatformDispose(() => { Assert(Thread.CurrentThread.ManagedThreadId == f.MainThread, "Disposal must run on Runtime thread."); order.Add(3); }));
            Task.Run(() => queued.Dispose()).GetAwaiter().GetResult();
            Assert(queued.Status == DtmResourceStatus.DisposeQueued && order.Count == 0, "Background Dispose queues without invoking author cleanup.");
            f.Runtime.BeginTick(1); f.Runtime.Drain();
            Assert(order.SequenceEqual(new[] { 3 }), "Drain processes requested disposal.");
            var closure = f.Runtime.RemoveOwner("a", ModOwnerCleanupReason.EntryFailed);
            Assert(order.SequenceEqual(new[] { 3, 2, 1 }) && closure.RemainingResources == 1 && closure.FailureCount == 1, "Owner closes resources in reverse order and isolates disposal failure.");
            AssertThrows(() => context.Snapshot.ToString(), "Held context cannot revive a closed owner.");
            Assert(scheduler.Post(() => { }).Status == DtmWorkStatus.Rejected, "Held scheduler rejects after close.");
            AssertThrows(() => resources.Register(new PlatformDispose(() => { })), "Closed resources cannot accept registration.");
            var retry = f.Runtime.RemoveOwner("A", ModOwnerCleanupReason.EntryFailed);
            Assert(retry.RemainingResources == 0 && attempts == 2 && failed.Status == DtmResourceStatus.Disposed, "Only failed resources retry; successful releases never repeat.");
            var newOwner = f.Service<IDtmScheduler>("A");
            Assert(!ReferenceEquals(newOwner, scheduler) && scheduler.Post(() => { }).Status == DtmWorkStatus.Rejected, "A new activation cannot reactivate an old facade.");
        }

        private static void PlatformCommandsParseDispatchAndIsolateOwners()
        {
            var f = new PlatformFixture();
            var a = f.Service<IDtmCommands>("Owner.A");
            var b = f.Service<IDtmCommands>("Owner.B");
            IReadOnlyList<string>? arguments = null;
            int calls = 0;
            using var registration = a.Register("echo", context =>
            {
                Assert(Thread.CurrentThread.ManagedThreadId == f.MainThread, "Commands must run on Runtime thread.");
                arguments = context.Arguments; calls++;
                for (int i = 0; i < 32; i++) Assert(context.WriteLine("ok"), "Bounded output should accept its first 32 lines.");
                Assert(!context.WriteLine("overflow"), "Output must reject excess lines.");
            }, alias: "say", description: "Echo arguments");
            AssertThrows(() => b.Register("other", _ => { }, alias: "SAY"), "Alias conflict must reject whole registration.");
            Assert(!b.GetHelp().Any(info => info.Name == "Owner.B/other"), "Rejected alias cannot leak its canonical registration.");
            var execution = Task.Run(() => a.Execute("say \"two words\" '' escaped\\ space"));
            // Task.Run unwraps the command Task; wait for submission, never for its completion before drain.
            Assert(SpinWait.SpinUntil(() => f.Runtime.PendingWorkCount == 1, 2000), "Background command did not enqueue.");
            Assert(calls == 0, "Background Execute must not invoke inline.");
            f.Runtime.BeginTick(1); f.Runtime.Drain();
            var result = execution.GetAwaiter().GetResult();
            Assert(result.Work.Status == DtmWorkStatus.Succeeded && result.OwnerId == "Owner.A" && result.Output.Count == 32 && arguments!.SequenceEqual(new[] { "two words", "", "escaped space" }), "Command parser/output/owner result must reflect the real dispatch.");
            Assert(a.Execute("missing").Result.Work.ErrorCode == "unknown-command" && a.Execute("say 'unterminated").Result.Work.ErrorCode == "invalid-command", "Malformed/unknown commands must return actionable results.");
            Assert(a.Execute("help").Result.Output.Any(line => line.Contains("Owner.A/echo")), "Help comes from actual registrations.");
            using var worldCommand = a.Register("world", _ => throw new Exception(), DtmWorkScope.CurrentWorld);
            Assert(a.Execute("Owner.A/world").Result.Work.ErrorCode == "scope-unavailable", "World command is rejected before queueing on title.");
            f.Ready();
            var old = a.Execute("Owner.A/world");
            f.Runtime.BeginWorldTransition();
            Assert(old.GetAwaiter().GetResult().Work.Status == DtmWorkStatus.Cancelled, "Queued world command cannot execute after its captured epoch ends.");
            using var boom = b.Register("boom", _ => throw new Exception("B failure"));
            var bad = b.Execute("Owner.B/boom"); var good = a.Execute("Owner.A/echo");
            f.Runtime.BeginTick(2); f.Runtime.Drain();
            Assert(bad.GetAwaiter().GetResult().Work.Status == DtmWorkStatus.Failed && good.GetAwaiter().GetResult().Work.Status == DtmWorkStatus.Succeeded, "Command exception must isolate owners.");
            f.Runtime.RemoveOwner("Owner.A", ModOwnerCleanupReason.RuntimeShutdown);
            Assert(!b.GetHelp().Any(info => info.OwnerId == "Owner.A") && a.Execute("Owner.B/boom").Result.Work.ErrorCode == "owner-closed", "Owner close clears commands and rejects held caller facade.");
        }

        private sealed class PlatformFixture
        {
            internal readonly int MainThread = Thread.CurrentThread.ManagedThreadId;
            internal double Now;
            internal int Errors;
            internal readonly PlatformRuntimeServices Runtime;
            internal PlatformFixture() { Runtime = new PlatformRuntimeServices(() => Thread.CurrentThread.ManagedThreadId == MainThread, (_, _, _) => Errors++, () => Now); }
            internal T Service<T>(string owner) where T : class => (T)Runtime.Resolve(owner, typeof(T))!;
            internal void Ready() { Runtime.BeginSaveAttempt(); Runtime.ObserveAfterLoad(); Runtime.CompleteSaveAttempt(true); Runtime.ObserveWorldReady(); }
        }
        private sealed class PlatformDispose : IDisposable
        {
            private readonly Action action;
            internal PlatformDispose(Action action) { this.action = action; }
            public void Dispose() => action();
        }
    }
}
