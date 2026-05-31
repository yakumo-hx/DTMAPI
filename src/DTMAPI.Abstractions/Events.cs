using System;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IEventsHelper
    {
        IGameLoopEvents GameLoop { get; }
        IInputEvents Input { get; }
        ISaveEvents Save { get; }
        IUiEvents UI { get; }
        IWorkshopEvents Workshop { get; }
        IDiagnosticsEvents Diagnostics { get; }
    }

    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IGameLoopEvents
    {
        event EventHandler<GameLaunchedEventArgs>? GameLaunched;
        event EventHandler<UpdateTickedEventArgs>? UpdateTicked;
        event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked;
        event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IInputEvents
    {
        event EventHandler<ButtonPressedEventArgs>? ButtonPressed;
        event EventHandler<ButtonReleasedEventArgs>? ButtonReleased;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface ISaveEvents
    {
        event EventHandler<SaveLoadedEventArgs>? SaveLoaded;
        event EventHandler<SaveSavingEventArgs>? SaveSaving;
        event EventHandler<SaveSavedEventArgs>? SaveSaved;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IUiEvents
    {
        event EventHandler<MenuOpenedEventArgs>? MenuOpened;
        event EventHandler<MenuClosedEventArgs>? MenuClosed;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IWorkshopEvents
    {
        event EventHandler<WorkshopModListChangedEventArgs>? ModListChanged;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IDiagnosticsEvents
    {
        event EventHandler<LogExportedEventArgs>? LogExported;
        event EventHandler<HookStatusChangedEventArgs>? HookStatusChanged;
    }

    public sealed class GameLaunchedEventArgs : EventArgs { }
    public sealed class ReturnedToTitleEventArgs : EventArgs { }
    public sealed class UpdateTickedEventArgs : EventArgs
    {
        public UpdateTickedEventArgs(ulong tick) => Tick = tick;
        public ulong Tick { get; }
    }

    public sealed class OneSecondUpdateTickedEventArgs : EventArgs
    {
        public OneSecondUpdateTickedEventArgs(uint second) => Second = second;
        public uint Second { get; }
    }

    public sealed class ButtonPressedEventArgs : EventArgs
    {
        public ButtonPressedEventArgs(string button) => Button = button;
        public string Button { get; }
    }

    public sealed class ButtonReleasedEventArgs : EventArgs
    {
        public ButtonReleasedEventArgs(string button) => Button = button;
        public string Button { get; }
    }

    public sealed class SaveLoadedEventArgs : EventArgs
    {
        public SaveLoadedEventArgs(int? saveSlot, bool isNewGame)
        {
            SaveSlot = saveSlot;
            IsNewGame = isNewGame;
        }

        public int? SaveSlot { get; }
        public bool IsNewGame { get; }
    }

    public sealed class SaveSavingEventArgs : EventArgs
    {
        public SaveSavingEventArgs(int? saveSlot) => SaveSlot = saveSlot;
        public int? SaveSlot { get; }
    }

    public sealed class SaveSavedEventArgs : EventArgs
    {
        public SaveSavedEventArgs(int? saveSlot) => SaveSlot = saveSlot;
        public int? SaveSlot { get; }
    }

    public sealed class MenuOpenedEventArgs : EventArgs
    {
        public MenuOpenedEventArgs(string menuId) => MenuId = menuId;
        public string MenuId { get; }
    }

    public sealed class MenuClosedEventArgs : EventArgs
    {
        public MenuClosedEventArgs(string menuId) => MenuId = menuId;
        public string MenuId { get; }
    }

    public sealed class WorkshopModListChangedEventArgs : EventArgs
    {
        public WorkshopModListChangedEventArgs(int modCount) => ModCount = modCount;
        public int ModCount { get; }
    }

    public sealed class LogExportedEventArgs : EventArgs
    {
        public LogExportedEventArgs(string reportPath) => ReportPath = reportPath;
        public string ReportPath { get; }
    }

    public sealed class HookStatusChangedEventArgs : EventArgs
    {
        public HookStatusChangedEventArgs(string hookId, string status)
        {
            HookId = hookId;
            Status = status;
        }

        public string HookId { get; }
        public string Status { get; }
    }
}
