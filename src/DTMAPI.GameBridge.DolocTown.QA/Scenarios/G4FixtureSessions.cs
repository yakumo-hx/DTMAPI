using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal delegate bool TryReadHatchAdultState(out bool adult);

    internal sealed class G4FixtureStepResult
    {
        private G4FixtureStepResult(bool completed, bool succeeded, string details)
        {
            Completed = completed;
            Succeeded = succeeded;
            Details = details ?? string.Empty;
        }

        internal bool Completed { get; }
        internal bool Succeeded { get; }
        internal string Details { get; }

        internal static G4FixtureStepResult Pending(string details) => new G4FixtureStepResult(false, false, details);
        internal static G4FixtureStepResult Verified(string details) => new G4FixtureStepResult(true, true, details);
        internal static G4FixtureStepResult Failed(string details) => new G4FixtureStepResult(true, false, details);
    }

    internal sealed class AgentPositionFixtureReceipt
    {
        internal AgentPositionFixtureReceipt(bool captured, double x, double y, double z)
        {
            Captured = captured;
            X = x;
            Y = y;
            Z = z;
        }

        internal bool Captured { get; }
        internal double X { get; }
        internal double Y { get; }
        internal double Z { get; }
    }

    internal sealed class CameraPositionFixtureReceipt
    {
        internal CameraPositionFixtureReceipt(bool captured, double x, double y, double z)
        {
            Captured = captured;
            X = x;
            Y = y;
            Z = z;
        }

        internal bool Captured { get; }
        internal double X { get; }
        internal double Y { get; }
        internal double Z { get; }
    }

    internal sealed class HatchVoiceFixtureSession : IDisposable
    {
        private readonly TryReadHatchAdultState tryReadAdult;
        private readonly Action<bool> writeAdult;
        private readonly Action playHatch;
        private readonly Action? playVanilla;
        private bool originalAdult;
        private bool armed;
        private bool terminal;
        private string closeSummary = "not-closed";

        internal HatchVoiceFixtureSession(TryReadHatchAdultState tryReadAdult, Action<bool> writeAdult, Action playHatch, Action? playVanilla)
        {
            this.tryReadAdult = tryReadAdult ?? throw new ArgumentNullException(nameof(tryReadAdult));
            this.writeAdult = writeAdult ?? throw new ArgumentNullException(nameof(writeAdult));
            this.playHatch = playHatch ?? throw new ArgumentNullException(nameof(playHatch));
            this.playVanilla = playVanilla;
        }

        internal string Run()
        {
            if (armed)
                throw new InvalidOperationException("Hatch voice fixture session may run only once.");
            if (!tryReadAdult(out originalAdult))
                throw new InvalidOperationException("Hatch voice fixture could not read the original adult state; no mutation was attempted.");
            armed = true;
            try
            {
                WriteAndVerify(false, "child");
                playHatch();
                WriteAndVerify(true, "adult");
                playHatch();
                playVanilla?.Invoke();
                return "child=called; adult=called; vanilla=" + (playVanilla == null ? "not-applicable" : "called");
            }
            finally
            {
                Close();
            }
        }

        internal string Close()
        {
            if (terminal)
                return closeSummary;
            if (!armed)
            {
                terminal = true;
                closeSummary = "armed=false; restored=true";
                return closeSummary;
            }

            // Repeat the idempotent write on every non-terminal Close attempt. A
            // native async setter may acknowledge a write before readback settles.
            writeAdult(originalAdult);
            bool readSucceeded = tryReadAdult(out bool currentAdult);
            bool restored = readSucceeded && currentAdult == originalAdult;
            closeSummary = "armed=" + armed + "; restored=" + restored;
            if (!restored)
                throw new InvalidOperationException("Hatch voice fixture close did not restore the original adult state with explicit readback.");
            terminal = true;
            return closeSummary;
        }

        private void WriteAndVerify(bool expectedAdult, string phase)
        {
            writeAdult(expectedAdult);
            if (!tryReadAdult(out bool actualAdult) || actualAdult != expectedAdult)
                throw new InvalidOperationException("Hatch voice fixture " + phase + " write was not confirmed by explicit adult-state readback.");
        }

        public void Dispose() => Close();
    }
}
