using System;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private bool platformNewGamePrefix, platformNewGamePostfix, platformNewGameFinalizer, platformLoadFinalizer;
        private bool platformSceneStart, platformDungeonStart, platformQuitRoom, platformRoomEnterPrefix, platformRoomEnterPostfix;
        private object? platformCompletedRoom;
        private PropertyInfo? platformDataProperty, platformNormalProperty, platformArchiveProperty, platformAgentProperty, platformRoomProperty;

        private bool PlatformContextHooksReady => platformNewGamePrefix && platformNewGamePostfix && platformNewGameFinalizer &&
            platformLoadFinalizer && platformSceneStart && platformDungeonStart && platformQuitRoom && platformRoomEnterPrefix &&
            platformRoomEnterPostfix && loadRequestedPatched && loadReturnedPatched && nativeGameFramePatched &&
            IsSaveLoadedHookReady && returnHomeRequestedPatched && returnHomePatched;

        private void InstallPlatformContextHooks(HarmonyReflectionPatcher patcher)
        {
            MethodInfo Callback(string name) => typeof(DolocTownHookCallbacks).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
            var newGame = HarmonyTargetSignature.Exact("DolocAPI", "System.Void", "System.Int32");
            var loadGame = HarmonyTargetSignature.Exact("DolocAPI", "System.Boolean", "System.Int32");
            if (!platformNewGamePrefix) platformNewGamePrefix = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "NewGame", Callback(nameof(DolocTownHookCallbacks.PlatformNewGamePrefix)), newGame);
            if (!platformNewGamePostfix) platformNewGamePostfix = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "NewGame", Callback(nameof(DolocTownHookCallbacks.PlatformNewGamePostfix)), newGame);
            if (!platformNewGameFinalizer) platformNewGameFinalizer = patcher.TryPatchFinalizer("DolocAPI, Assembly-CSharp", "NewGame", Callback(nameof(DolocTownHookCallbacks.PlatformLoadFinalizer)), newGame);
            if (!platformLoadFinalizer) platformLoadFinalizer = patcher.TryPatchFinalizer("DolocAPI, Assembly-CSharp", "LoadGame", Callback(nameof(DolocTownHookCallbacks.PlatformLoadFinalizer)), loadGame);
            if (!platformSceneStart) platformSceneStart = patcher.TryPatchPrefix("DolocTown.GameStateSceneTransition, Assembly-CSharp", "Start", Callback(nameof(DolocTownHookCallbacks.PlatformWorldTransitionPrefix)),
                HarmonyTargetSignature.Exact("DolocTown.GameStateSceneTransition", "System.Void", "DolocTown.Room", "UnityEngine.Vector2", "System.Action", "System.Boolean"));
            if (!platformDungeonStart) platformDungeonStart = patcher.TryPatchPrefix("DolocTown.DungeonTransitionState, Assembly-CSharp", "Start", Callback(nameof(DolocTownHookCallbacks.PlatformWorldTransitionPrefix)),
                HarmonyTargetSignature.Exact("DolocTown.DungeonTransitionState", "System.Void", "DolocTown.DungeonRoom", "System.Action"));
            if (!platformQuitRoom) platformQuitRoom = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "QuitCurrentRoom", Callback(nameof(DolocTownHookCallbacks.PlatformWorldTransitionPrefix)),
                HarmonyTargetSignature.Exact("DolocAPI", "System.Void", "DolocTown.Room", "System.Boolean"));
            var enterRoom = HarmonyTargetSignature.Exact("DolocTown.Room", "System.Void");
            if (!platformRoomEnterPrefix) platformRoomEnterPrefix = patcher.TryPatchPrefix("DolocTown.Room, Assembly-CSharp", "OnEnterRoom", Callback(nameof(DolocTownHookCallbacks.PlatformWorldTransitionPrefix)), enterRoom);
            if (!platformRoomEnterPostfix) platformRoomEnterPostfix = patcher.TryPatchPostfix("DolocTown.Room, Assembly-CSharp", "OnEnterRoom", Callback(nameof(DolocTownHookCallbacks.PlatformRoomEnteredPostfix)), enterRoom);
            runtime.SetHookStatus("Platform.Context", PlatformContextHooksReady ? "experimental" : "pending", "GameBridge native lifecycle boundaries",
                PlatformContextHooksReady ? "Candidate boundaries installed; ready requires fresh successful load/new-game, room entry and a normal native frame. PN-009/020 validation required." : "Incomplete native boundaries; world scope remains unavailable.");
        }

        internal void ResetPlatformWorldObservation() { platformCompletedRoom = null; }
        internal void ObservePlatformWorldTransition()
        {
            ResetPlatformWorldObservation();
            runtime.NotifyPlatformWorldTransition();
        }
        internal void ObservePlatformRoomEntered(object room) { platformCompletedRoom = room; }
        internal void ObservePlatformNativeFrame()
        {
            if (!PlatformContextHooksReady || platformCompletedRoom == null || runtime.PlatformContextSnapshot.Phase != DtmRuntimePhase.LoadingWorld) return;
            Type? api = Type.GetType("DolocAPI, Assembly-CSharp", false);
            if (api == null) return;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            platformDataProperty ??= api.GetProperty("IsDataLoaded", flags);
            platformNormalProperty ??= api.GetProperty("IsNormalState", flags);
            platformArchiveProperty ??= api.GetProperty("archiveHandle", flags);
            platformAgentProperty ??= api.GetProperty("agent", flags);
            platformRoomProperty ??= api.GetProperty("CurrentRoom", flags);
            if (!(platformDataProperty?.GetValue(null, null) is bool data) || !data ||
                !(platformNormalProperty?.GetValue(null, null) is bool normal) || !normal ||
                platformArchiveProperty?.GetValue(null, null) == null || platformAgentProperty?.GetValue(null, null) == null ||
                !ReferenceEquals(platformCompletedRoom, platformRoomProperty?.GetValue(null, null))) return;
            runtime.NotifyPlatformWorldReady();
            if (runtime.PlatformContextSnapshot.IsWorldReady) platformCompletedRoom = null;
        }
    }
}
