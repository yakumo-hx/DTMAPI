using System;
using System.Linq;
using DTMAPI.AudioContent;

namespace DTMAPI.Modules.Audio
{
    internal sealed partial class AudioEngine
    {
        internal Func<Action<SoundEndReceipt>, Action, MusicStart>? CaptureMusicEvent(string logical, out string owner)
        {
            var selected = registrations.Where(b => !b.Closed)
                .SelectMany((b, registration) => b.Input.Events.Select((e, i) => new { Batch = b, Definition = e, Index = i, Registration = registration }))
                .Where(e => e.Definition.NativeEvent.Length != 0 && string.Equals(e.Definition.NativeEvent, logical, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Batch.Order).ThenByDescending(e => e.Registration).ThenByDescending(e => e.Index).FirstOrDefault();
            owner = selected?.Batch.Owner ?? "";
            return selected == null ? null : CaptureMusic(selected.Batch, selected.Definition.Key);
        }
        private Func<Action<SoundEndReceipt>, Action, MusicStart> CaptureMusic(Batch batch, string eventKey)
        {
            // Bind once to this exact immutable registration, never resolve a
            // future registration with the same public key from a late job.
            return (ended, invalidate) =>
            {
                try
                {
                    if (batch.Closed || retiringWorld) return new MusicStart(null, true, "AudioEventWithdrawn");
                    batch.Guard();
                    if (batch.State == AudioPreparationState.Failed || batch.State == AudioPreparationState.CleanupPending)
                        return new MusicStart(null, true, batch.Diagnostic);
                    if (batch.State != AudioPreparationState.Ready) return new MusicStart(null, false, "Preparing:" + batch.State);
                    var evt = batch.Events[eventKey];
                    if (evt.Media.Streaming && !ReadyStreaming(batch, evt, out string detail))
                        return new MusicStart(null, evt.Media.StreamState == AudioPreparationState.Failed, detail);
                    return new MusicStart(StartVoice(batch, batch.Events[eventKey], 0, null, ended, invalidate), false, "Ready");
                }
                catch (Exception ex) { return new MusicStart(null, true, ex.GetBaseException().Message); }
            };
        }
    }
    internal readonly struct MusicStart
    {
        internal readonly OwnedSoundBank.Voice? Voice;
        internal readonly bool Failed;
        internal readonly string Detail;
        internal MusicStart(OwnedSoundBank.Voice? voice, bool failed, string detail) { Voice = voice; Failed = failed; Detail = detail; }
    }
}
