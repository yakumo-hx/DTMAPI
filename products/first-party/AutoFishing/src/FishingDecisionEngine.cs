using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingProductAction
    {
        None,
        Cast,
        PrepareNativeBite,
        ReelVisibleMiniGame,
        ReelSkipMiniGame
    }

    internal readonly struct FishingProductDecision
    {
        internal FishingProductDecision(FishingProductAction action, string reason)
        {
            Action = action;
            Reason = reason ?? string.Empty;
        }

        internal FishingProductAction Action { get; }
        internal string Reason { get; }
    }

    internal sealed class FishingDecisionEngine
    {
        internal FishingProductDecision Decide(
            FishingPrimitiveSnapshot snapshot,
            bool instantBite,
            bool skipMiniGame,
            DateTimeOffset nowUtc,
            DateTimeOffset nextCastAtUtc)
        {
            if ((snapshot.Phase == FishingPrimitivePhase.Idle || snapshot.Phase == FishingPrimitivePhase.PullExited || snapshot.Phase == FishingPrimitivePhase.Interrupted) &&
                nowUtc >= nextCastAtUtc)
                return new FishingProductDecision(FishingProductAction.Cast, "product:auto-cast");
            if (snapshot.Phase == FishingPrimitivePhase.WaitPlayable && instantBite)
                return new FishingProductDecision(FishingProductAction.PrepareNativeBite, "product:instant-native-bite");
            if (snapshot.Phase == FishingPrimitivePhase.BiteReady)
                return new FishingProductDecision(skipMiniGame ? FishingProductAction.ReelSkipMiniGame : FishingProductAction.ReelVisibleMiniGame, skipMiniGame ? "product:skip-minigame" : "product:native-visible-minigame");
            return new FishingProductDecision(FishingProductAction.None, "product:wait-native-transition");
        }

        internal FishingSyntheticInputAction DecideMiniGameInput(FishingMiniGameFrame frame)
        {
            if (frame.CurrentTime < frame.NoteStart || frame.CurrentTime > frame.NoteEnd)
                return FishingSyntheticInputAction.Release;
            if (frame.NoteKind == FishingMiniGameNoteKind.Stable)
                return FishingSyntheticInputAction.Hold;
            if (frame.NoteKind == FishingMiniGameNoteKind.Bonus && !frame.BonusAlreadyTapped)
                return FishingSyntheticInputAction.TapBonus;
            return FishingSyntheticInputAction.Release;
        }
    }
}
