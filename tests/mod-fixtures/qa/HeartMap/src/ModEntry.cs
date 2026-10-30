using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Internal.HeartMap
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private MapModule? module;
        public override void Entry(IDtmHelper helper)
        {
            ExperimentConfig config = helper.ReadConfig<ExperimentConfig>();
            if (!config.Enabled && !config.DiagnosticControl)
            {
                helper.Monitor.Log("HeartMap disabled: no map hooks, registration, scene, scans or frame subscriptions.");
                return;
            }
            if (config.DiagnosticControl) config.RegisterMap = false;
            module = new MapModule(helper, config);
            module.Start();
        }
        public void Dispose() { module?.Dispose(); module = null; }
    }

    public sealed class ExperimentConfig
    {
        public bool Enabled { get; set; }
        public bool RegisterMap { get; set; } = true;
        public string[] PrivateMaps { get; set; } = new[] { "heart", "asymmetric" };
        public string[] EditorFiles { get; set; } = Array.Empty<string>();
        public bool NoNativeSave { get; set; } = true;
        public bool DiagnosticPanel { get; set; }
        // Explicit common NoNativeSave/measurement harness for the native-world control.
        // Default false: disabling the module creates nothing.
        public bool DiagnosticControl { get; set; }
        public int MeasurementRepeats { get; set; } = 3;
        public string EvidenceDirectory { get; set; } = "";
        public string AcceptanceScenario { get; set; } = "";
        public int AcceptanceSaveSlot { get; set; } = 2;
        public bool AcceptanceLoadSave { get; set; } = true;
        public bool AcceptanceQuit { get; set; }
        public string AcceptanceExpectedFile { get; set; } = "";
        public string AcceptanceReplacementFile { get; set; } = "";
    }
}
