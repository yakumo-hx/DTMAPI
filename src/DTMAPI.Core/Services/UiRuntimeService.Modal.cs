using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    public sealed partial class UiRuntimeService
    {
        internal DtmModalOpenResult OpenOwnerBoundModal(string menuId, string ownerId, Action ensureRuntimeThread)
        {
            string menu = string.IsNullOrWhiteSpace(menuId) ? "DTMAPI.Custom" : menuId.Trim();
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            string owner = ownerId.Trim();
            if (IsOpen)
            {
                return IsOwnerBoundCustomMenuOpen(menu, owner)
                    ? new DtmModalOpenResult(DtmModalOpenStatus.AlreadyOpen, new ModalSession(this, menu, owner, OverlaySessionSequence, ensureRuntimeThread))
                    : new DtmModalOpenResult(DtmModalOpenStatus.Busy, null);
            }

            long sequence = ++OverlaySessionSequence;
            ActiveMenuId = menu;
            ActiveMenuOwnerId = owner;
            IsOpen = true;
            opened(menu);
            // EventManager isolates subscriber exceptions. A successful dispatch still
            // may have closed, replaced or deactivated this exact occupancy.
            return IsModalSessionOpen(menu, owner, sequence)
                ? new DtmModalOpenResult(DtmModalOpenStatus.Opened, new ModalSession(this, menu, owner, sequence, ensureRuntimeThread))
                : new DtmModalOpenResult(DtmModalOpenStatus.Interrupted, null);
        }

        private bool IsModalSessionOpen(string menu, string owner, long sequence) =>
            OverlaySessionSequence == sequence && IsOwnerBoundCustomMenuOpen(menu, owner);

        internal int CountOwnerResources(string ownerId) =>
            IsOpen && ActiveMenuOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        internal int RemoveOwner(string ownerId)
        {
            if (CountOwnerResources(ownerId) == 0)
                return 0;
            Close();
            return 1;
        }

        private sealed class ModalSession : IUiModalSession
        {
            private readonly UiRuntimeService ui;
            private readonly Action ensureRuntimeThread;

            internal ModalSession(UiRuntimeService ui, string menu, string owner, long sequence, Action ensureRuntimeThread)
            {
                this.ui = ui;
                MenuId = menu;
                OwnerId = owner;
                Sequence = sequence;
                this.ensureRuntimeThread = ensureRuntimeThread;
            }

            public string OwnerId { get; }
            public string MenuId { get; }
            public long Sequence { get; }
            public bool IsOpen
            {
                get
                {
                    ensureRuntimeThread();
                    return ui.IsModalSessionOpen(MenuId, OwnerId, Sequence);
                }
            }

            public bool Close()
            {
                if (!IsOpen)
                    return false;
                ui.Close();
                return true;
            }

            public void Dispose() => Close();
        }
    }
}
