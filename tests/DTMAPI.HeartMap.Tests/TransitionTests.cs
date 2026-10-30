using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using DTMAPI.Internal.HeartMap;

internal static class TransitionTests
{
    private static int checks;
    internal static void Run()
    {
        string heart = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "inputs/heart.dtmap.json"));
        string second = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "inputs/asymmetric.dtmap.json"));
        var catalog = new MapCatalog();
        Check(catalog.TryAdd(heart, out var a, out var error), "heart accepted: " + error);
        Check(catalog.TryAdd(second, out var b, out error), "asymmetric accepted: " + error);
        Check(a!.SceneName != b!.SceneName && a.RoomId != b.RoomId && a.Room.Width != b.Room.Width, "independent identity and real dimensions");
        var old = new HeartLayout();
        for (int y = 0; y < HeartLayout.Height; y++) for (int x = 0; x < HeartLayout.Width; x++)
        {
            if (old.Solid[x, y] != a.Room.IsSolid(x, y) || old.Platform[x, y] != a.Room.Platform.Contains(y * HeartLayout.Width + x)) throw new Exception("Native consumer heart geometry drift");
        }
        Check(a.Spawn.WorldX == 36.75 && Math.Abs(a.Spawn.WorldY - 9.1) < .001 && a.Exit.ExitTargetKind == "home", "compiled point coordinates consumed");
        Check(!catalog.TryAdd("{", out _, out error) && error.Contains("field="), "bad input has located error");
        Check(catalog.Maps.Count() == 2 && ReferenceEquals(catalog.Maps.First(), a), "bad input preserves both definitions");
        Check(!catalog.TryAdd(heart, out _, out error) && error.Contains("id-conflict"), "duplicate rejected without replacement");
        var changed = JsonNode.Parse(heart)!;
        changed["projectId"] = "other-project"; changed["map"]!["revision"] = 99;
        var identity = new MapCatalog();
        Check(identity.TryAdd(changed.ToJsonString(), out var revised, out _), "revision accepted in independent catalog");
        Check(revised!.RoomId == a.RoomId, "stable native names across revision/project changes");
        changed["rooms"]![0]!["width"] = 49;
        Check(!catalog.TryAdd(changed.ToJsonString(), out _, out _), "invalid matrix/dimensions rejected before registration");
        // Exact rollback identities: failing one publication cannot remove an
        // existing map or a replacement installed by another owner.
        var rows = new Dictionary<string, object> { ["native"] = new object(), ["map-a"] = a };
        object candidate = new object(), replacement = new object();
        try
        {
            using var tx = new MapPublication();
            tx.Apply(() => rows.Add("candidate", candidate), () => { if (rows.TryGetValue("candidate", out var found) && ReferenceEquals(found, candidate)) rows.Remove("candidate"); });
            tx.Apply(() => throw new InvalidOperationException("publish failure"), () => { });
            tx.Commit();
        }
        catch (InvalidOperationException ex) { Check(ex.Message == "publish failure", "real failed publication path"); }
        Check(rows.Count == 2 && ReferenceEquals(rows["map-a"], a), "transaction rollback preserves previous map and native row");
        using (var tx = new MapPublication())
        {
            tx.Apply(() => rows.Add("candidate", candidate), () => { if (ReferenceEquals(rows["candidate"], candidate)) rows.Remove("candidate"); });
            rows["candidate"] = replacement;
        }
        Check(ReferenceEquals(rows["candidate"], replacement), "rollback does not remove a foreign replacement");

        foreach (bool exit in new[] { false, true })
        {
            var req = New(exit); var ticket = req.BeginNative(1);
            Check(ticket.TakeFade(7) && !ticket.TakeFade(7), "fade callback once");
            req.Published(); double deadline = req.Deadline;
            req.Published(); Check(req.Deadline == deadline, "duplicate publication does not extend timeout");
            Check(ticket.TakeLoad(7) && !ticket.TakeLoad(7), "native load callback once");
            Check(ticket.Complete(7) && !ticket.Complete(7), "completion callback once");
            req.Releasing(2); Check(!ticket.TakeLoad(7) && !ticket.Complete(7), "cleanup invalidates callbacks before release");
            var expected = exit ? MapResult.Exited : MapResult.Entered;
            Check(req.Finish(expected) && !req.Finish(MapResult.FaultedRecovery) && req.Result == expected && req.CompletionCount == 1, "exactly one immutable terminal result");
        }
        foreach (string failure in new[] { "publication-failed", "missing-load", "missing-complete", "cancelled" })
        {
            var req = New(); var oldTicket = req.BeginNative(1);
            if (failure == "missing-complete") { req.Published(); oldTicket.TakeLoad(7); }
            Check(!req.Expired(8.99) && req.Expired(9), "missing callback has finite deadline");
            var recovery = req.BeginRecovery(failure, 9)!;
            Check(!oldTicket.TakeFade(7) && !oldTicket.TakeLoad(7) && !oldTicket.Complete(7), "late attempt cannot act during recovery");
            Check(req.BeginRecovery("retry", 10) == null && req.Deadline == 17, "one bounded recovery attempt");
            Check(recovery.TakeFade(7) && recovery.TakeLoad(7) && recovery.Complete(7), "recovery owns distinct callbacks");
            req.Releasing(11); req.Finish(failure == "cancelled" ? MapResult.Cancelled : MapResult.FailedRecovered);
            Check(req.CompletionCount == 1 && !recovery.Complete(7), "recovered terminal cannot complete again");
        }
        var before = New(); Check(before.Expired(20), "prepare timeout bounded");
        before.Finish(MapResult.FailedBeforeTransition); Check(before.Ticket == null, "prepare failure never enters native owner");
        var broken = New(); broken.BeginNative(0); broken.BeginRecovery("lost world", 8);
        Check(broken.Expired(16), "lost world recovery deadline"); broken.Finish(MapResult.FaultedRecovery);
        Check(broken.BeginRecovery("infinite retry", 17) == null, "fault terminal cannot retry or create native work");
        var unload = New(); var unloadTicket = unload.BeginNative(0); unload.Releasing(1);
        Check(unload.Expired(9), "unload completion bounded"); unload.Finish(MapResult.FaultedUnload);
        Check(!unloadTicket.Complete(7) && unload.CompletionCount == 1, "failed unload cannot signal success later");
        foreach (MapPhase phase in new[] { MapPhase.Preparing, MapPhase.Publishing, MapPhase.Entering, MapPhase.Recovering, MapPhase.Releasing })
        {
            var req = New(); NativeTicket? ticket = null;
            if (phase != MapPhase.Preparing) ticket = req.BeginNative(1);
            if (phase == MapPhase.Entering) req.Published();
            if (phase == MapPhase.Recovering) ticket = req.BeginRecovery("test", 2);
            if (phase == MapPhase.Releasing) req.Releasing(3);
            req.Finish(MapResult.CancelledAtTitle);
            Check(!req.IsCurrent(8) && (ticket == null || (!ticket.TakeLoad(8) && !ticket.Complete(8))) && req.CompletionCount == 1, "title invalidates " + phase);
        }
        Console.WriteLine($"Map transitions/data/registration: {checks} focused checks PASS. Native recovery remains a separate game gate.");
    }
    private static MapRequest New(bool exit = false) => new MapRequest(1, 7, "map-b", exit, 0);
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
}
