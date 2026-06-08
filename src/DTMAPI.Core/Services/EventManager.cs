using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Services
{
    internal sealed class EventManager
    {
        private const int HighFrequencyFailureThreshold = 3;
        private readonly DiagnosticsService diagnostics;

        private readonly EventSlot<GameLaunchedEventArgs> gameLaunched;
        private readonly EventSlot<UpdateTickedEventArgs> updateTicked;
        private readonly EventSlot<OneSecondUpdateTickedEventArgs> oneSecondUpdateTicked;
        private readonly EventSlot<ReturnedToTitleEventArgs> returnedToTitle;
        private readonly EventSlot<ButtonPressedEventArgs> buttonPressed;
        private readonly EventSlot<ButtonReleasedEventArgs> buttonReleased;
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
            gameLaunched = new EventSlot<GameLaunchedEventArgs>(diagnostics, "GameLoop.GameLaunched");
            updateTicked = new EventSlot<UpdateTickedEventArgs>(diagnostics, "GameLoop.UpdateTicked", HighFrequencyFailureThreshold);
            oneSecondUpdateTicked = new EventSlot<OneSecondUpdateTickedEventArgs>(diagnostics, "GameLoop.OneSecondUpdateTicked", HighFrequencyFailureThreshold);
            returnedToTitle = new EventSlot<ReturnedToTitleEventArgs>(diagnostics, "GameLoop.ReturnedToTitle");
            buttonPressed = new EventSlot<ButtonPressedEventArgs>(diagnostics, "Input.ButtonPressed");
            buttonReleased = new EventSlot<ButtonReleasedEventArgs>(diagnostics, "Input.ButtonReleased");
            saveLoaded = new EventSlot<SaveLoadedEventArgs>(diagnostics, "Save.SaveLoaded");
            saveSaving = new EventSlot<SaveSavingEventArgs>(diagnostics, "Save.SaveSaving");
            saveSaved = new EventSlot<SaveSavedEventArgs>(diagnostics, "Save.SaveSaved");
            menuOpened = new EventSlot<MenuOpenedEventArgs>(diagnostics, "UI.MenuOpened");
            menuClosed = new EventSlot<MenuClosedEventArgs>(diagnostics, "UI.MenuClosed");
            workshopModListChanged = new EventSlot<WorkshopModListChangedEventArgs>(diagnostics, "Workshop.ModListChanged");
            logExported = new EventSlot<LogExportedEventArgs>(diagnostics, "Diagnostics.LogExported");
            hookStatusChanged = new EventSlot<HookStatusChangedEventArgs>(diagnostics, "Diagnostics.HookStatusChanged");
        }

        public IEventsHelper CreateProxy(string owner) => new EventsProxy(this, owner);

        public void DispatchGameLaunched() => gameLaunched.Dispatch(this, new GameLaunchedEventArgs());
        public void DispatchUpdateTicked(ulong tick) => updateTicked.Dispatch(this, new UpdateTickedEventArgs(tick));
        public void DispatchOneSecondUpdateTicked(uint second) => oneSecondUpdateTicked.Dispatch(this, new OneSecondUpdateTickedEventArgs(second));
        public void DispatchReturnedToTitle() => returnedToTitle.Dispatch(this, new ReturnedToTitleEventArgs());
        public void DispatchButtonPressed(string button) => buttonPressed.Dispatch(this, new ButtonPressedEventArgs(button));
        public void DispatchButtonReleased(string button) => buttonReleased.Dispatch(this, new ButtonReleasedEventArgs(button));
        public void DispatchSaveLoaded(int? saveSlot, bool isNewGame) => saveLoaded.Dispatch(this, new SaveLoadedEventArgs(saveSlot, isNewGame));
        public void DispatchSaveSaving(int? saveSlot) => saveSaving.Dispatch(this, new SaveSavingEventArgs(saveSlot));
        public void DispatchSaveSaved(int? saveSlot) => saveSaved.Dispatch(this, new SaveSavedEventArgs(saveSlot));
        public void DispatchMenuOpened(string menuId) => menuOpened.Dispatch(this, new MenuOpenedEventArgs(menuId));
        public void DispatchMenuClosed(string menuId) => menuClosed.Dispatch(this, new MenuClosedEventArgs(menuId));
        public void DispatchWorkshopModListChanged(int count) => workshopModListChanged.Dispatch(this, new WorkshopModListChangedEventArgs(count));
        public void DispatchLogExported(string path) => logExported.Dispatch(this, new LogExportedEventArgs(path));
        public void DispatchHookStatusChanged(string hookId, string status) => hookStatusChanged.Dispatch(this, new HookStatusChangedEventArgs(hookId, status));

        private sealed class EventSlot<TArgs> where TArgs : EventArgs
        {
            private readonly DiagnosticsService diagnostics;
            private readonly string eventName;
            private readonly int failureThreshold;
            private readonly List<OwnedHandler<TArgs>> handlers = new List<OwnedHandler<TArgs>>();

            public EventSlot(DiagnosticsService diagnostics, string eventName, int failureThreshold = 0)
            {
                this.diagnostics = diagnostics;
                this.eventName = eventName;
                this.failureThreshold = failureThreshold;
            }

            public void Add(string owner, EventHandler<TArgs>? handler)
            {
                if (handler != null)
                    handlers.Add(new OwnedHandler<TArgs>(owner, handler));
            }

            public void Remove(string owner, EventHandler<TArgs>? handler)
            {
                if (handler == null)
                    return;
                handlers.RemoveAll(entry => entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) && entry.Handler == handler);
            }

            public void Dispatch(object sender, TArgs args)
            {
                foreach (OwnedHandler<TArgs> entry in handlers.ToArray())
                {
                    if (entry.Disabled)
                        continue;

                    try
                    {
                        entry.Handler(sender, args);
                        entry.ConsecutiveFailures = 0;
                    }
                    catch (Exception ex)
                    {
                        entry.ConsecutiveFailures++;
                        diagnostics.RecordError(entry.Owner, "Unhandled exception in " + eventName + " event handler.", ex.ToString());
                        if (failureThreshold > 0 && entry.ConsecutiveFailures >= failureThreshold)
                        {
                            entry.Disabled = true;
                            diagnostics.RecordError(
                                entry.Owner,
                                eventName + " event handler disabled after " + entry.ConsecutiveFailures + " consecutive failures.",
                                "DTMAPI disabled this high-frequency handler to protect the runtime update loop.");
                        }
                    }
                }
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
            public int ConsecutiveFailures { get; set; }
            public bool Disabled { get; set; }
        }

        private sealed class EventsProxy : IEventsHelper
        {
            public EventsProxy(EventManager events, string owner)
            {
                GameLoop = new GameLoopProxy(events, owner);
                Input = new InputProxy(events, owner);
                Save = new SaveProxy(events, owner);
                UI = new UiProxy(events, owner);
                Workshop = new WorkshopProxy(events, owner);
                Diagnostics = new DiagnosticsProxy(events, owner);
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
            public GameLoopProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<GameLaunchedEventArgs>? GameLaunched { add => events.gameLaunched.Add(owner, value); remove => events.gameLaunched.Remove(owner, value); }
            public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add => events.updateTicked.Add(owner, value); remove => events.updateTicked.Remove(owner, value); }
            public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add => events.oneSecondUpdateTicked.Add(owner, value); remove => events.oneSecondUpdateTicked.Remove(owner, value); }
            public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle { add => events.returnedToTitle.Add(owner, value); remove => events.returnedToTitle.Remove(owner, value); }
        }

        private sealed class InputProxy : IInputEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            public InputProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<ButtonPressedEventArgs>? ButtonPressed { add => events.buttonPressed.Add(owner, value); remove => events.buttonPressed.Remove(owner, value); }
            public event EventHandler<ButtonReleasedEventArgs>? ButtonReleased { add => events.buttonReleased.Add(owner, value); remove => events.buttonReleased.Remove(owner, value); }
        }

        private sealed class SaveProxy : ISaveEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            public SaveProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<SaveLoadedEventArgs>? SaveLoaded { add => events.saveLoaded.Add(owner, value); remove => events.saveLoaded.Remove(owner, value); }
            public event EventHandler<SaveSavingEventArgs>? SaveSaving { add => events.saveSaving.Add(owner, value); remove => events.saveSaving.Remove(owner, value); }
            public event EventHandler<SaveSavedEventArgs>? SaveSaved { add => events.saveSaved.Add(owner, value); remove => events.saveSaved.Remove(owner, value); }
        }

        private sealed class UiProxy : IUiEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            public UiProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<MenuOpenedEventArgs>? MenuOpened { add => events.menuOpened.Add(owner, value); remove => events.menuOpened.Remove(owner, value); }
            public event EventHandler<MenuClosedEventArgs>? MenuClosed { add => events.menuClosed.Add(owner, value); remove => events.menuClosed.Remove(owner, value); }
        }

        private sealed class WorkshopProxy : IWorkshopEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            public WorkshopProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<WorkshopModListChangedEventArgs>? ModListChanged { add => events.workshopModListChanged.Add(owner, value); remove => events.workshopModListChanged.Remove(owner, value); }
        }

        private sealed class DiagnosticsProxy : IDiagnosticsEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            public DiagnosticsProxy(EventManager events, string owner) { this.events = events; this.owner = owner; }
            public event EventHandler<LogExportedEventArgs>? LogExported { add => events.logExported.Add(owner, value); remove => events.logExported.Remove(owner, value); }
            public event EventHandler<HookStatusChangedEventArgs>? HookStatusChanged { add => events.hookStatusChanged.Add(owner, value); remove => events.hookStatusChanged.Remove(owner, value); }
        }
    }
}
