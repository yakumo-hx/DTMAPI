using System;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    // Owns dialog callbacks; the existing UI service remains the sole modal occupancy authority.
    internal sealed class ConfigMenuDialogService : IConfigMenuDialogHost
    {
        private readonly UiRuntimeService ui;
        private readonly Action ensureThread;
        private readonly Action<string, Exception> reportError;
        internal Func<Session, bool>? Show { get; set; }
        internal Action<Session>? Hide { get; set; }
        internal Session? Current { get; private set; }

        internal ConfigMenuDialogService(UiRuntimeService ui, Action ensureThread, Action<string, Exception> reportError)
        { this.ui = ui; this.ensureThread = ensureThread; this.reportError = reportError; }

        public bool TryShow(string ownerId, DtmConfigDialog dialog, Action<string> onSelected, out IConfigMenuDialogSession? session)
        {
            ensureThread();
            if (dialog == null) throw new ArgumentNullException(nameof(dialog));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));
            session = null;
            Validate();
            if (Current != null || Show == null) return false;
            bool child = ui.IsOpen && ui.ActiveMenuOwnerId.Length == 0 && ui.ActiveMenuId == "DTMAPI." + ui.CurrentPage;
            if (ui.IsOpen && !child) return false;
            var candidate = new Session(this, ownerId, dialog, onSelected, child ? ui.OverlaySessionSequence : (long?)null);
            Current = candidate;
            try
            {
                if (!child)
                {
                    var opened = ui.OpenOwnerBoundModal("DTMAPI.ConfigDialog", ownerId, ensureThread);
                    candidate.Occupancy = opened.Session;
                    if (opened.Session == null || !ReferenceEquals(Current, candidate)) { candidate.Close(); return false; }
                }
                candidate.Opening = false;
                if (!candidate.IsOpen || !Show(candidate) || !candidate.IsOpen) { candidate.Close(); return false; }
                session = candidate;
                return true;
            }
            catch (Exception ex) { candidate.Close(); reportError(ownerId, ex); return false; }
        }

        internal void Validate()
        {
            ensureThread();
            if (Current != null && !Current.Opening && !Current.IsOpen) Current.Close();
        }
        internal void Clear() { ensureThread(); Current?.Close(); }
        internal int CountOwnerResources(string ownerId) => Current?.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
        public int RemoveOwner(string ownerId)
        {
            ensureThread();
            if (CountOwnerResources(ownerId) == 0) return 0;
            Current!.Close(); return 1;
        }

        internal sealed class Session : IConfigMenuDialogSession
        {
            private readonly ConfigMenuDialogService service;
            private readonly long? parentSequence;
            private Action<string>? callback;
            internal IUiModalSession? Occupancy;
            internal bool Opening = true;
            internal string OwnerId { get; }
            internal DtmConfigDialog Dialog { get; }
            internal bool IsChild => parentSequence.HasValue;
            internal Session(ConfigMenuDialogService service, string ownerId, DtmConfigDialog dialog, Action<string> callback, long? parentSequence)
            { this.service = service; OwnerId = ownerId; Dialog = dialog; this.callback = callback; this.parentSequence = parentSequence; }
            public bool IsOpen
            {
                get
                {
                    service.ensureThread();
                    return ReferenceEquals(service.Current, this) && (parentSequence.HasValue
                        ? service.ui.IsOpen && service.ui.OverlaySessionSequence == parentSequence.Value
                        : Occupancy?.IsOpen == true);
                }
            }
            internal void Select(string id)
            {
                if (!IsOpen || !Dialog.Buttons.Any(b => b.Id == id && b.Enabled)) return;
                Action<string>? selected = callback;
                Close();
                try { selected?.Invoke(id); }
                catch (Exception ex) { service.reportError(OwnerId, ex); }
            }
            public bool Close()
            {
                service.ensureThread();
                callback = null;
                IUiModalSession? occupancy = Occupancy;
                Occupancy = null;
                if (!ReferenceEquals(service.Current, this)) { occupancy?.Close(); return false; }
                service.Current = null;
                try { service.Hide?.Invoke(this); }
                finally { occupancy?.Close(); }
                return true;
            }
            public void Dispose() => Close();
        }
    }
}
