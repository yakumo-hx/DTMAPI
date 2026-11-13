using System;
using System.Diagnostics;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HookInstallScheduler
    {
        private readonly Func<double> elapsedSeconds;
        private double? retryAt;
        private bool pending;
        private bool closed;

        internal HookInstallScheduler(Func<double>? elapsedSeconds = null)
        {
            this.elapsedSeconds = elapsedSeconds ?? (() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        }

        internal bool RetryActive => retryAt.HasValue;

        internal void StartRetry()
        {
            if (!closed && !retryAt.HasValue)
                retryAt = elapsedSeconds() + 2;
        }

        internal void StopRetry() => retryAt = null;

        internal void Close()
        {
            closed = true;
            retryAt = null;
            pending = false;
        }

        internal void PollRetry()
        {
            if (!retryAt.HasValue)
                return;
            double now = elapsedSeconds();
            if (now < retryAt.Value)
                return;
            retryAt = now + 2;
            Request();
        }

        internal void Request()
        {
            if (!closed)
                pending = true;
        }

        internal bool TryConsumePending()
        {
            bool requested = pending;
            pending = false;
            return requested;
        }
    }
}
