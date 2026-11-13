using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;

namespace DTMAPI.UnitTests
{
    // Test transport for the product's internal UI/native interfaces. No retired API host.
    internal sealed class DebugConsoleTestRuntime : IDebugConsoleRuntime,
        IDebugConsoleNativeRuntime, IDebugConsoleInputRuntime, IDebugConsoleModalRuntime
    {
        private readonly DtmApiRuntime runtime;
        internal DebugConsoleTestRuntime(DtmApiRuntime runtime) => this.runtime = runtime;
        public IMonitor RuntimeMonitor => runtime.RuntimeMonitor;
        public IMonitor Monitor => runtime.RuntimeMonitor;
        public string NativeOwnerLabel => "ProductNativeTest";
        public IDebugConsoleInputRuntime Input => this;
        public IDebugConsoleModalRuntime UI => this;
        public ITranslationHelper Translation { get; } = new TestTranslation();
        public string ProductVersion => "1.1.10";
        public string DtmApiPath => runtime.Paths.DtmApiPath;
        public bool ModalOpen { set => DebugConsoleInputGate.ModalOpen = value; }
        public bool NativeInputDrainActive { set => DebugConsoleInputGate.NativeInputDrainActive = value; }
        public DtmModalOpenResult OpenModal(string menuId, string ownerId) =>
            runtime.UI.OpenOwnerBoundModal(menuId, ownerId, () => runtime.EnsureModHelperRuntimeThread(ownerId));
        public void Suppress(string button) => runtime.Input.Suppress(button);
        public void SetHookStatus(string id, string status, string source, string details) =>
            runtime.SetHookStatus(id, status, source, details);
        public void RecordError(string owner, string message, string details) =>
            runtime.Diagnostics.RecordError(owner, message, details);
        public IReadOnlyDictionary<string, InventoryDebugSourceGroup> GetContentSources(string table) => new Dictionary<string, InventoryDebugSourceGroup>();
        public IReadOnlyList<AnimalContentSource> GetAnimalContentSources() => Array.Empty<AnimalContentSource>();
        public void SetMovementMultiplier(object? player, double multiplier) => DebugConsoleMovementHooks.SetMultiplier(player, multiplier);
        public void Status(string id, string status, string source, string details) => SetHookStatus(id, status, source, details);
        public void Error(string operation, Exception error) => RecordError("DTMAPI.DebugConsoleMod", operation, error.ToString());
        private sealed class TestTranslation : ITranslationHelper
        {
            public string Language => CultureInfo.CurrentUICulture.Name;
            public string Get(string key, string fallback = "") => fallback;
        }
    }
}
