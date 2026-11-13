using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Services
{
    internal sealed class EventManager
    {
        private readonly DiagnosticsService diagnostics;

        private readonly EventSlot<GameLaunchedEventArgs> gameLaunched;
        private readonly EventSlot<UpdateTickedEventArgs> updateTicked;
        private readonly EventSlot<OneSecondUpdateTickedEventArgs> oneSecondUpdateTicked;
        private readonly EventSlot<ReturnedToTitleEventArgs> returnedToTitle;
        private readonly EventSlot<ButtonPressedEventArgs> buttonPressed;
        private readonly EventSlot<ButtonReleasedEventArgs> buttonReleased;
        private readonly EventSlot<KeybindPressedEventArgs> keybindPressed;
        private readonly EventSlot<KeybindReleasedEventArgs> keybindReleased;
        private readonly EventSlot<SaveLoadedEventArgs> saveLoaded;
        private readonly EventSlot<SaveSavingEventArgs> saveSaving;
        private readonly EventSlot<SaveSavedEventArgs> saveSaved;
        private readonly EventSlot<MenuOpenedEventArgs> menuOpened;
        private readonly EventSlot<MenuClosedEventArgs> menuClosed;
        private readonly EventSlot<WorkshopModListChangedEventArgs> workshopModListChanged;
        private readonly EventSlot<LogExportedEventArgs> logExported;
        private readonly EventSlot<HookStatusChangedEventArgs> hookStatusChanged;

        public EventManager(DiagnosticsService diagnostics)
        {
            this.diagnostics = diagnostics;
            gameLaunched = CreateSlot<GameLaunchedEventArgs>("GameLoop.GameLaunched");
            updateTicked = CreateSlot<UpdateTickedEventArgs>("GameLoop.UpdateTicked");
            oneSecondUpdateTicked = CreateSlot<OneSecondUpdateTickedEventArgs>("GameLoop.OneSecondUpdateTicked");
            returnedToTitle = CreateSlot<ReturnedToTitleEventArgs>("GameLoop.ReturnedToTitle");
            buttonPressed = CreateSlot<ButtonPressedEventArgs>("Input.ButtonPressed");
            buttonReleased = CreateSlot<ButtonReleasedEventArgs>("Input.ButtonReleased");
            keybindPressed = CreateSlot<KeybindPressedEventArgs>("Input.KeybindPressed");
            keybindReleased = CreateSlot<KeybindReleasedEventArgs>("Input.KeybindReleased");
            saveLoaded = CreateSlot<SaveLoadedEventArgs>("Save.SaveLoaded");
            saveSaving = CreateSlot<SaveSavingEventArgs>("Save.SaveSaving");
            saveSaved = CreateSlot<SaveSavedEventArgs>("Save.SaveSaved");
            menuOpened = CreateSlot<MenuOpenedEventArgs>("UI.MenuOpened");
            menuClosed = CreateSlot<MenuClosedEventArgs>("UI.MenuClosed");
            workshopModListChanged = CreateSlot<WorkshopModListChangedEventArgs>("Workshop.ModListChanged");
            logExported = CreateSlot<LogExportedEventArgs>("Diagnostics.LogExported");
            hookStatusChanged = CreateSlot<HookStatusChangedEventArgs>("Diagnostics.HookStatusChanged");
        }

        public IEventsHelper CreateProxy(string owner) => new EventsProxy(this, owner, () => { });
        public IEventsHelper CreateOwnerBoundProxy(string owner, Action ensureOwnerActive) => new EventsProxy(this, owner, ensureOwnerActive);

        private EventSlot<TArgs> CreateSlot<TArgs>(string eventName) where TArgs : EventArgs =>
            new EventSlot<TArgs>(diagnostics, eventName);

        public int RemoveOwner(string owner)
        {
            int removed = 0;
            removed += gameLaunched.RemoveOwner(owner);
            removed += updateTicked.RemoveOwner(owner);
            removed += oneSecondUpdateTicked.RemoveOwner(owner);
            removed += returnedToTitle.RemoveOwner(owner);
            removed += buttonPressed.RemoveOwner(owner);
            removed += buttonReleased.RemoveOwner(owner);
            removed += keybindPressed.RemoveOwner(owner);
            removed += keybindReleased.RemoveOwner(owner);
            removed += saveLoaded.RemoveOwner(owner);
            removed += saveSaving.RemoveOwner(owner);
            removed += saveSaved.RemoveOwner(owner);
            removed += menuOpened.RemoveOwner(owner);
            removed += menuClosed.RemoveOwner(owner);
            removed += workshopModListChanged.RemoveOwner(owner);
            removed += logExported.RemoveOwner(owner);
            removed += hookStatusChanged.RemoveOwner(owner);
            return removed;
        }

        public int CountOwnerResources(string owner) =>
            gameLaunched.CountOwnerResources(owner) +
            updateTicked.CountOwnerResources(owner) +
            oneSecondUpdateTicked.CountOwnerResources(owner) +
            returnedToTitle.CountOwnerResources(owner) +
            buttonPressed.CountOwnerResources(owner) +
            buttonReleased.CountOwnerResources(owner) +
            keybindPressed.CountOwnerResources(owner) +
            keybindReleased.CountOwnerResources(owner) +
            saveLoaded.CountOwnerResources(owner) +
            saveSaving.CountOwnerResources(owner) +
            saveSaved.CountOwnerResources(owner) +
            menuOpened.CountOwnerResources(owner) +
            menuClosed.CountOwnerResources(owner) +
            workshopModListChanged.CountOwnerResources(owner) +
            logExported.CountOwnerResources(owner) +
            hookStatusChanged.CountOwnerResources(owner);

        public void DispatchGameLaunched()
        {
            if (gameLaunched.HasHandlers)
                gameLaunched.Dispatch(this, new GameLaunchedEventArgs());
        }

        internal void DispatchGameLaunched(string ownerId) =>
            gameLaunched.DispatchOwner(this, new GameLaunchedEventArgs(), ownerId);

        public void DispatchUpdateTicked(ulong tick)
        {
            if (updateTicked.HasHandlers)
                updateTicked.Dispatch(this, new UpdateTickedEventArgs(tick));
        }

        public void DispatchOneSecondUpdateTicked(uint second)
        {
            if (oneSecondUpdateTicked.HasHandlers)
                oneSecondUpdateTicked.Dispatch(this, new OneSecondUpdateTickedEventArgs(second));
        }

        public void DispatchReturnedToTitle()
        {
            if (returnedToTitle.HasHandlers)
                returnedToTitle.Dispatch(this, new ReturnedToTitleEventArgs());
        }

        public void DispatchButtonPressed(string button)
        {
            if (buttonPressed.HasHandlers)
                buttonPressed.Dispatch(this, new ButtonPressedEventArgs(button));
        }

        internal IReadOnlyList<string> DispatchButtonPressedWithRecipients(string button) =>
            buttonPressed.HasHandlers
                ? buttonPressed.DispatchWithRecipients(this, new ButtonPressedEventArgs(button))
                : Array.Empty<string>();

        internal int DispatchButtonPressedToOwner(string ownerId, string button) =>
            buttonPressed.HasOwner(ownerId)
                ? buttonPressed.DispatchOwner(this, new ButtonPressedEventArgs(button), ownerId)
                : 0;

        internal IReadOnlyList<string> DispatchButtonPressedToOwnerWithRecipients(string ownerId, string button) =>
            buttonPressed.HasOwner(ownerId)
                ? buttonPressed.DispatchWithRecipients(this, new ButtonPressedEventArgs(button), ownerId)
                : Array.Empty<string>();

        public void DispatchButtonReleased(string button)
        {
            if (buttonReleased.HasHandlers)
                buttonReleased.Dispatch(this, new ButtonReleasedEventArgs(button));
        }

        internal void DispatchButtonReleasedToOwners(IReadOnlyList<string> ownerIds, string button)
        {
            if (buttonReleased.HasHandlers && ownerIds.Count > 0)
                buttonReleased.DispatchToOwners(this, new ButtonReleasedEventArgs(button), ownerIds);
        }

        public void DispatchKeybindPressed(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (keybindPressed.HasHandlers)
                keybindPressed.Dispatch(this, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton));
        }

        internal IReadOnlyList<string> DispatchKeybindPressedWithRecipients(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton) =>
            keybindPressed.HasHandlers
                ? keybindPressed.DispatchWithRecipients(this, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton))
                : Array.Empty<string>();

        internal IReadOnlyList<string> DispatchKeybindPressedToOwnerWithRecipients(string targetOwnerId, string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton) =>
            keybindPressed.HasOwner(targetOwnerId)
                ? keybindPressed.DispatchWithRecipients(this, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton), targetOwnerId)
                : Array.Empty<string>();

        public void DispatchKeybindReleased(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (keybindReleased.HasHandlers)
                keybindReleased.Dispatch(this, new KeybindReleasedEventArgs(ownerId, keybindId, keybinds, triggerButton));
        }

        internal void DispatchKeybindReleasedToOwners(IReadOnlyList<string> ownerIds, string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (keybindReleased.HasHandlers && ownerIds.Count > 0)
                keybindReleased.DispatchToOwners(this, new KeybindReleasedEventArgs(ownerId, keybindId, keybinds, triggerButton), ownerIds);
        }

        public void DispatchSaveLoaded(int? saveSlot, bool isNewGame)
        {
            if (saveLoaded.HasHandlers)
                saveLoaded.Dispatch(this, new SaveLoadedEventArgs(saveSlot, isNewGame));
        }

        public void DispatchSaveSaving(int? saveSlot) =>
            _ = TryDispatchSaveSaving(saveSlot);

        public bool TryDispatchSaveSaving(int? saveSlot) =>
            !saveSaving.HasHandlers || saveSaving.Dispatch(this, new SaveSavingEventArgs(saveSlot));

        public void DispatchSaveSaved(int? saveSlot)
        {
            if (saveSaved.HasHandlers)
                saveSaved.Dispatch(this, new SaveSavedEventArgs(saveSlot));
        }

        public void DispatchMenuOpened(string menuId)
        {
            if (menuOpened.HasHandlers)
                menuOpened.Dispatch(this, new MenuOpenedEventArgs(menuId));
        }

        public void DispatchMenuClosed(string menuId)
        {
            if (menuClosed.HasHandlers)
                menuClosed.Dispatch(this, new MenuClosedEventArgs(menuId));
        }

        public void DispatchWorkshopModListChanged(int count)
        {
            if (workshopModListChanged.HasHandlers)
                workshopModListChanged.Dispatch(this, new WorkshopModListChangedEventArgs(count));
        }

        public void DispatchLogExported(string path)
        {
            if (logExported.HasHandlers)
                logExported.Dispatch(this, new LogExportedEventArgs(path));
        }

        public void DispatchHookStatusChanged(string hookId, string status)
        {
            if (hookStatusChanged.HasHandlers)
                hookStatusChanged.Dispatch(this, new HookStatusChangedEventArgs(hookId, status));
        }

        private sealed class EventSlot<TArgs> where TArgs : EventArgs
        {
            private readonly DiagnosticsService diagnostics;
            private readonly string eventName;
            private readonly List<OwnedHandler<TArgs>> handlers = new List<OwnedHandler<TArgs>>();

            public EventSlot(DiagnosticsService diagnostics, string eventName)
            {
                this.diagnostics = diagnostics;
                this.eventName = eventName;
            }

            public bool HasHandlers => handlers.Count > 0;

            public bool HasOwner(string owner)
            {
                foreach (OwnedHandler<TArgs> entry in handlers)
                    if (entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            public void Add(string owner, EventHandler<TArgs>? handler, Action ensureOwnerActive)
            {
                if (handler == null)
                    return;
                ensureOwnerActive();
                handlers.Add(new OwnedHandler<TArgs>(owner, handler));
            }

            public void Remove(string owner, EventHandler<TArgs>? handler)
            {
                if (handler == null)
                    return;
                for (int index = handlers.Count - 1; index >= 0; index--)
                {
                    OwnedHandler<TArgs> entry = handlers[index];
                    if (!entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) || entry.Handler != handler)
                        continue;
                    handlers.RemoveAt(index);
                    break;
                }
            }

            public int RemoveOwner(string owner) =>
                handlers.RemoveAll(entry => entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase));

            public int CountOwnerResources(string owner) =>
                handlers.Count(entry => entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase));

            public bool Dispatch(object sender, TArgs args)
            {
                DispatchCore(sender, args, handlers.ToArray(), null, null, out bool succeeded);
                return succeeded;
            }

            public int DispatchOwner(object sender, TArgs args, string ownerId) =>
                DispatchCore(sender, args, handlers.ToArray(), ownerId, null, out _);

            public string[] DispatchWithRecipients(object sender, TArgs args, string? ownerId = null)
            {
                var delivered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                DispatchCore(sender, args, handlers.ToArray(), ownerId, delivered, out _);
                return delivered.ToArray();
            }

            public void DispatchToOwners(object sender, TArgs args, IReadOnlyList<string> ownerIds)
            {
                // One release uses one subscription copy for all owners recorded by InputService.
                OwnedHandler<TArgs>[] membership = handlers.ToArray();
                string[] recipients = ownerIds.Where(owner => !string.IsNullOrWhiteSpace(owner))
                    .Select(owner => owner.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                foreach (string owner in recipients)
                    DispatchCore(sender, args, membership, owner, null, out _);
            }

            private int DispatchCore(object sender, TArgs args, OwnedHandler<TArgs>[] membership,
                string? ownerId, ISet<string>? delivered, out bool succeeded)
            {
                int dispatched = 0;
                succeeded = true;
                foreach (OwnedHandler<TArgs> entry in membership)
                {
                    if (ownerId != null && !entry.Owner.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    dispatched++;
                    delivered?.Add(entry.Owner);
                    try
                    {
                        entry.Handler(sender, args);
                    }
                    catch (Exception ex)
                    {
                        succeeded = false;
                        diagnostics.RecordError(entry.Owner, "Unhandled exception in " + eventName + " event handler.", ex.ToString());
                    }
                }
                return dispatched;
            }
        }

        private sealed class OwnedHandler<TArgs> where TArgs : EventArgs
        {
            public OwnedHandler(string owner, EventHandler<TArgs> handler)
            {
                Owner = owner;
                Handler = handler;
            }

            public string Owner { get; }
            public EventHandler<TArgs> Handler { get; }
        }

        private sealed class EventsProxy : IEventsHelper
        {
            public EventsProxy(EventManager events, string owner, Action ensureOwnerActive)
            {
                GameLoop = new GameLoopProxy(events, owner, ensureOwnerActive);
                Input = new InputProxy(events, owner, ensureOwnerActive);
                Save = new SaveProxy(events, owner, ensureOwnerActive);
                UI = new UiProxy(events, owner, ensureOwnerActive);
                Workshop = new WorkshopProxy(events, owner, ensureOwnerActive);
                Diagnostics = new DiagnosticsProxy(events, owner, ensureOwnerActive);
            }

            public IGameLoopEvents GameLoop { get; }
            public IInputEvents Input { get; }
            public ISaveEvents Save { get; }
            public IUiEvents UI { get; }
            public IWorkshopEvents Workshop { get; }
            public IDiagnosticsEvents Diagnostics { get; }
        }

        private sealed class GameLoopProxy : IGameLoopEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public GameLoopProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<GameLaunchedEventArgs>? GameLaunched { add => events.gameLaunched.Add(owner, value, ensure); remove => events.gameLaunched.Remove(owner, value); }
            public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add => events.updateTicked.Add(owner, value, ensure); remove => events.updateTicked.Remove(owner, value); }
            public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add => events.oneSecondUpdateTicked.Add(owner, value, ensure); remove => events.oneSecondUpdateTicked.Remove(owner, value); }
            public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle { add => events.returnedToTitle.Add(owner, value, ensure); remove => events.returnedToTitle.Remove(owner, value); }
        }

        private sealed class InputProxy : IInputEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public InputProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<ButtonPressedEventArgs>? ButtonPressed { add => events.buttonPressed.Add(owner, value, ensure); remove => events.buttonPressed.Remove(owner, value); }
            public event EventHandler<ButtonReleasedEventArgs>? ButtonReleased { add => events.buttonReleased.Add(owner, value, ensure); remove => events.buttonReleased.Remove(owner, value); }
            public event EventHandler<KeybindPressedEventArgs>? KeybindPressed { add => events.keybindPressed.Add(owner, value, ensure); remove => events.keybindPressed.Remove(owner, value); }
            public event EventHandler<KeybindReleasedEventArgs>? KeybindReleased { add => events.keybindReleased.Add(owner, value, ensure); remove => events.keybindReleased.Remove(owner, value); }
        }

        private sealed class SaveProxy : ISaveEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public SaveProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<SaveLoadedEventArgs>? SaveLoaded { add => events.saveLoaded.Add(owner, value, ensure); remove => events.saveLoaded.Remove(owner, value); }
            public event EventHandler<SaveSavingEventArgs>? SaveSaving { add => events.saveSaving.Add(owner, value, ensure); remove => events.saveSaving.Remove(owner, value); }
            public event EventHandler<SaveSavedEventArgs>? SaveSaved { add => events.saveSaved.Add(owner, value, ensure); remove => events.saveSaved.Remove(owner, value); }
        }

        private sealed class UiProxy : IUiEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public UiProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<MenuOpenedEventArgs>? MenuOpened { add => events.menuOpened.Add(owner, value, ensure); remove => events.menuOpened.Remove(owner, value); }
            public event EventHandler<MenuClosedEventArgs>? MenuClosed { add => events.menuClosed.Add(owner, value, ensure); remove => events.menuClosed.Remove(owner, value); }
        }

        private sealed class WorkshopProxy : IWorkshopEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public WorkshopProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<WorkshopModListChangedEventArgs>? ModListChanged { add => events.workshopModListChanged.Add(owner, value, ensure); remove => events.workshopModListChanged.Remove(owner, value); }
        }

        private sealed class DiagnosticsProxy : IDiagnosticsEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public DiagnosticsProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<LogExportedEventArgs>? LogExported { add => events.logExported.Add(owner, value, ensure); remove => events.logExported.Remove(owner, value); }
            public event EventHandler<HookStatusChangedEventArgs>? HookStatusChanged { add => events.hookStatusChanged.Add(owner, value, ensure); remove => events.hookStatusChanged.Remove(owner, value); }
        }
    }
}
