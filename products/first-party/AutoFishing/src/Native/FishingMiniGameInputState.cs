using System;
using System.Collections.Generic;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingMiniGameFrameReadStatus
    {
        Ready,
        NotReady,
        Faulted
    }

    internal delegate FishingMiniGameFrameReadStatus FishingMiniGameFrameReader(
        object gameHandle,
        long sequence,
        out FishingMiniGameFrame frame,
        out string failureReason);

    internal sealed class FishingMiniGameInputTransaction
    {
        private readonly HashSet<int> tappedBonusNoteIndexes = new HashSet<int>();
        private object? currentGameHandle;

        internal int TrackedBonusNoteCount => tappedBonusNoteIndexes.Count;

        internal bool TryExecute(
            object gameHandle,
            long sequence,
            FishingMiniGameFrameReader frameReader,
            Func<FishingMiniGameFrame, FishingSyntheticInputAction> provider,
            Action publishSuccess,
            Action<string, string, Exception?> publishFault,
            out FishingSyntheticInputAction action,
            out FishingMiniGameFrame frame)
        {
            if (frameReader == null)
                throw new ArgumentNullException(nameof(frameReader));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (publishSuccess == null)
                throw new ArgumentNullException(nameof(publishSuccess));
            if (publishFault == null)
                throw new ArgumentNullException(nameof(publishFault));

            action = FishingSyntheticInputAction.Release;
            frame = default;
            string stage = "frame";
            try
            {
                FishingMiniGameFrameReadStatus frameStatus = frameReader(gameHandle, sequence, out frame, out string failureReason);
                if (frameStatus == FishingMiniGameFrameReadStatus.NotReady)
                    return false;
                if (frameStatus == FishingMiniGameFrameReadStatus.Faulted)
                {
                    publishFault(stage, failureReason ?? string.Empty, null);
                    return false;
                }

                stage = "bonus-lookup";
                BindGame(gameHandle);
                if (frame.NoteIndex >= 0 && tappedBonusNoteIndexes.Contains(frame.NoteIndex))
                    frame = new FishingMiniGameFrame(frame.Sequence, frame.CurrentTime, frame.NoteKind, frame.NoteIndex, frame.NoteStart, frame.NoteEnd, bonusAlreadyTapped: true);

                stage = "provider";
                action = provider(frame);
                if (action == FishingSyntheticInputAction.TapBonus && frame.NoteIndex >= 0)
                {
                    stage = "bonus-record";
                    tappedBonusNoteIndexes.Add(frame.NoteIndex);
                }

                stage = "publish";
                publishSuccess();
                return true;
            }
            catch (Exception ex)
            {
                publishFault(stage, string.Empty, ex);
                return false;
            }
        }

        internal void Clear()
        {
            currentGameHandle = null;
            tappedBonusNoteIndexes.Clear();
        }

        private void BindGame(object gameHandle)
        {
            if (gameHandle == null)
                throw new ArgumentNullException(nameof(gameHandle));
            if (ReferenceEquals(currentGameHandle, gameHandle))
                return;
            currentGameHandle = gameHandle;
            tappedBonusNoteIndexes.Clear();
        }
    }
}
