using System;
namespace DTMAPI.AudioContent
{
    internal sealed class AudioSnapshot
    {
        public AudioPreparationState State { get; set; }
        public string Diagnostic { get; set; } = "";
        public int MediaCount { get; set; }
        public int BankCount { get; set; }
        public int VoiceCount { get; set; }
        public long ManagedMediaBytes { get; set; }
        public long LoadedBankBytes { get; set; }
        public long TemporaryNativeBytes { get; set; }
        public int RegistrationCount { get; set; }
        public int CleanupPendingCount { get; set; }
        public long ReservedBytes { get; set; }
        public long CacheBytes { get; set; }
        public long OwnerCacheBytes { get; set; }
        public int FilePinCount { get; set; }
        public int PreparationWorkerCount { get; set; }
        public string CacheDirectory { get; set; } = "";
    }
    internal sealed class AudioLease : IDisposable
    {
        private Action? release;
        public AudioLease(Action release) { this.release = release ?? throw new ArgumentNullException(nameof(release)); }
        public void Dispose() { release?.Invoke(); release = null; }
    }
}
