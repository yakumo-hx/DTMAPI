using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.DebugConsole;
using DolocTown.GameData;

namespace DTMAPI.DebugConsoleHarmonyOwnerFixture
{
    internal static partial class Program
    {

        private static void SessionModesRespectNativeOwners()
        {
            CleanupAll();
            var installer = new DebugConsoleHookInstaller();
            installer.InstallAtomically();
            var actions = new DebugConsoleNativeActions(new ModeRuntime());
            var owner = new ManifestModel { Name = "Mode fixture", UniqueID = "DTMAPI.Test.Modes", Author = "DTMAPI", Version = "1.0.0" };
            DolocAPI.archiveHandle = new ArchiveDataHandle();
            DolocAPI.timeScaleManager = new DolocTown.TimeScaleManager();
            DolocAPI.agent = new DolocTown.BodyController();
            actions.BeginSaveSession();
            DebugConsoleModeHooks.Actions = actions;
            DebugConsoleModeHooks.EndSessionBeforeLoad = () =>
            {
                try { actions.EndSaveSession("load entry"); return true; }
                catch { return false; }
            };
            try
            {
                var data = DolocAPI.archiveHandle.farmData.agentData;
                Assert(actions.SetGodMode(owner, true).Success && data.health == 100 && data.energy == 200 && data.spirit == 300,
                    "God enable must immediately fill all three current maxima.");
                Assert(ArchiveOperationGlobal._CostHealth(DolocAPI.archiveHandle, 500) == 100, "Health cost escaped god protection.");
                Assert(ArchiveOperationGlobal._CostEnergy(DolocAPI.archiveHandle, 500), "God energy use must succeed.");
                ArchiveOperationGlobal._CostEnergyNoProtect(DolocAPI.archiveHandle, 500);
                data.Tick(); data.AddSpirit(-1000);
                Assert(data.health == 100 && data.energy == 200 && data.spirit == 300, "Native consumption escaped one of the three locks.");
                data.MaxHealth = 130; data.MaxEnergy = 230; data.MaxSpirit = 330;
                actions.Update();
                Assert(data.health == 130 && data.energy == 230 && data.spirit == 330, "God must follow current maxima.");
                Assert(actions.SetGodMode(owner, false).Success && data.health == 130 && data.energy == 230 && data.spirit == 330,
                    "God disable restored an obsolete snapshot.");
                ArchiveOperationGlobal._CostHealth(DolocAPI.archiveHandle, 5);
                ArchiveOperationGlobal._CostEnergy(DolocAPI.archiveHandle, 6); data.Tick();
                Assert(data.health == 125 && data.energy == 224 && data.spirit == 329, "Native consumption did not resume after god disable.");
                Console.WriteLine("PASS current-max fill, health/energy/spirit native hooks, unlock without rollback");

                var manager = DolocAPI.timeScaleManager;
                foreach (int value in new[] { 1, 4, 7, 20 })
                    Assert(actions.SetTimeScale(owner, value).Success && manager.Value == value, "Representative time button value failed: " + value);
                Assert(!actions.SetTimeScale(owner, 0).Success && !actions.SetTimeScale(owner, 21).Success && !actions.SetTimeScale(owner, 1.5).Success,
                    "Out-of-range/fractional time was admitted.");
                int writes = manager.Writes;
                actions.Update(); actions.Update();
                Assert(manager.Writes == writes, "Update forced time every frame.");
                DolocAPI.gameLoop.IsPaused = true;
                Assert(!actions.SetTimeScale(owner, 4).Success && manager.Writes == writes, "Time button overwrote native pause.");
                DolocAPI.gameLoop.IsPaused = false;
                manager.SetTimeScale(0.5f, false);
                actions.EndSaveSession("native cutscene control");
                Assert(manager.Value == 0.5f, "Session cleanup overwrote a later native time write.");
                actions.BeginSaveSession();
                Assert(actions.SetTimeScale(owner, 4).Success, "Could not apply four.");
                manager.SetTimeScale(4, false);
                actions.EndSaveSession("same-value native control");
                Assert(manager.Value == 4, "A same-value native write did not take ownership.");
                actions.BeginSaveSession();
                Assert(actions.SetTimeScale(owner, 4).Success, "Could not apply four after native control.");
                actions.EndSaveSession("ordinary session end");
                Assert(manager.Value == 1, "Owned time did not return to normal at session end.");
                Console.WriteLine("PASS bounded time values, no per-frame writes, pause and later native ownership");

                actions.BeginSaveSession();
                manager.FailReadAfterWrite = true;
                Assert(!actions.SetTimeScale(owner, 4).Success && manager.Value == 4, "Post-write fault was not retained.");
                Assert(actions.SetTimeScale(owner, 1).Success && manager.Value == 1, "Explicit one-times retry lost restoration responsibility.");
                Assert(actions.SetTimeScale(owner, 4).Success && actions.SetGodMode(owner, true).Success && actions.SetSpeedMultiplier(owner, 2).Success,
                    "Could not arrange the load-failure boundary.");
                var archive = DolocAPI.archiveHandle;
                int loads = DolocAPI.Loads;
                manager.FailWrite = true;
                Assert(!DolocAPI.LoadGame(9) && DolocAPI.Loads == loads && ReferenceEquals(archive, DolocAPI.archiveHandle),
                    "Native load began despite unresolved old-session restoration.");
                Assert(!actions.GodModeEnabled && !actions.IsCurrentSaveSession && DolocAPI.agent.MoveSpeed == 10,
                    "Retained restoration debt left mode effects active.");
                Assert(DolocAPI.LoadGame(9) && manager.Value == 1 && !ReferenceEquals(archive, DolocAPI.archiveHandle),
                    "Same-slot load retry did not release time before replacing the archive.");
                actions.BeginSaveSession();
                Assert(!actions.GodModeEnabled && DolocAPI.agent.MoveSpeed == 10, "New session inherited old modes.");
                Console.WriteLine("PASS post-write retry and real LoadGame same-slot entry rejects until cleanup succeeds");

                Assert(actions.SetTimeScale(owner, 4).Success && actions.SetGodMode(owner, true).Success, "Replacement setup failed.");
                manager.FailWrite = true;
                AssertThrows<InvalidOperationException>(() => actions.EndSaveSession("fault before replacement"), "Expected retained cleanup failure.");
                var replacement = new DolocTown.TimeScaleManager { Value = 4 };
                DolocAPI.timeScaleManager = replacement;
                DolocAPI.archiveHandle = new ArchiveDataHandle();
                var replacementData = DolocAPI.archiveHandle.farmData.agentData;
                actions.Update(); actions.EndSaveSession("discard retired identity");
                Assert(replacement.Value == 4 && replacement.Writes == 0 && replacementData.health == 10,
                    "Old responsibility mutated a replacement holder or data with equal multiplier.");
                actions.BeginSaveSession();
                Assert(actions.SetTimeScale(owner, 7).Success, "Could not apply new session time.");
                DolocAPI.NewGame(8);
                Assert(replacement.Value == 1 && !actions.GodModeEnabled, "NewGame did not release session modes.");
                Console.WriteLine("PASS restore failure plus holder/data replacement cannot mutate new objects; NewGame ends modes");
            }
            finally
            {
                DolocAPI.gameLoop.IsPaused = false;
                actions.EndSaveSession("fixture cleanup");
                installer.Unpatch();
            }
            Assert(CountOwner(ProductOwner) == 0, "Mode fixture left product hooks.");
        }

        private sealed class ModeRuntime : IDebugConsoleNativeRuntime
        {
            public IMonitor Monitor => NullMonitor.Instance;
            public string NativeOwnerLabel => "ProductNative";
            public IReadOnlyList<IContentItemInfo> GetIndexedItems() => Array.Empty<IContentItemInfo>();
            public IContentItemInfo? GetIndexedItem(string id) => null;
            public IReadOnlyList<AnimalContentSource> GetAnimalContentSources() => Array.Empty<AnimalContentSource>();
            public void SetMovementMultiplier(object? player, double multiplier) => DebugConsoleMovementHooks.SetMultiplier(player, multiplier);
            public void Status(string id, string status, string source, string details) { }
            public void Error(string operation, Exception error) { Console.WriteLine("Expected fault: " + operation); }
        }
    }
}
