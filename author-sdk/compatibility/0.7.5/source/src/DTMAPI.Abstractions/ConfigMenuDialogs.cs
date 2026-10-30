using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Abstractions
{
    /// <summary>Optional capability on IDtmConfigMenuApi. Declare immediately after adding a value option at startup.</summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public interface IDtmConfigMenuRestartApi
    {
        /// <summary>Captures this option's startup value. Successful saves join the platform's combined close-menu reminder.</summary>
        void RequireRestartForLastOption(IManifest mod);
    }

    /// <summary>Optional capability on IDtmConfigMenuApi. Calls are runtime-thread-only and owner-bound.</summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public interface IDtmConfigMenuDialogsApi
    {
        /// <summary>Returns false when another dialog/menu owns the surface or no native menu can host it.</summary>
        bool TryShowDialog(IManifest mod, DtmConfigDialog dialog, Action<string> onSelected, out IConfigMenuDialogSession? session);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public sealed class DtmConfigDialog
    {
        public DtmConfigDialog(string title, string message, IReadOnlyList<DtmConfigDialogButton> buttons, string cancelButtonId, string? defaultButtonId = null, DtmConfigDialogSize? size = null)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            if (buttons == null || buttons.Count == 0 || buttons.Any(b => b == null))
                throw new ArgumentException("A dialog requires buttons.", nameof(buttons));
            var copy = buttons.ToArray();
            if (copy.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("Dialog button IDs must be unique.", nameof(buttons));
            CancelButtonId = cancelButtonId;
            DefaultButtonId = defaultButtonId ?? cancelButtonId;
            if (!copy.Any(b => b.Id == CancelButtonId && b.Enabled) || !copy.Any(b => b.Id == DefaultButtonId && b.Enabled))
                throw new ArgumentException("Cancel and default buttons must name enabled buttons.");
            Buttons = Array.AsReadOnly(copy);
            Size = size ?? new DtmConfigDialogSize(820, 570);
        }
        public string Title { get; }
        public string Message { get; }
        public IReadOnlyList<DtmConfigDialogButton> Buttons { get; }
        public string CancelButtonId { get; }
        public string DefaultButtonId { get; }
        public DtmConfigDialogSize Size { get; }
    }

    /// <summary>Logical UI units before viewport scaling. Oversized dialogs fit the current screen.</summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public sealed class DtmConfigDialogSize
    {
        public DtmConfigDialogSize(int width, int height)
        {
            if (width < 320) throw new ArgumentOutOfRangeException(nameof(width), "Minimum dialog width is 320.");
            if (height < 200) throw new ArgumentOutOfRangeException(nameof(height), "Minimum dialog height is 200.");
            Width = width; Height = height;
        }
        public int Width { get; }
        public int Height { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public sealed class DtmConfigDialogButton
    {
        public DtmConfigDialogButton(string id, string label, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Button ID is required.", nameof(id));
            Id = id; Label = label ?? string.Empty; Enabled = enabled;
        }
        public string Id { get; }
        public string Label { get; }
        public bool Enabled { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public interface IConfigMenuDialogSession : IDisposable
    {
        bool IsOpen { get; }
        /// <summary>Closes only this dialog, without invoking a selection callback. Safe after owner cleanup.</summary>
        bool Close();
    }

    internal interface IConfigMenuDialogHost
    {
        bool TryShow(string ownerId, DtmConfigDialog dialog, Action<string> onSelected, out IConfigMenuDialogSession? session);
        int RemoveOwner(string ownerId);
    }
}
