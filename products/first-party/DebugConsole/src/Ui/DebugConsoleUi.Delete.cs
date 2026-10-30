using System;
using System.Collections.Generic;
using System.Globalization;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleUi
    {
        private object? deleteConfirmationRoot;
        private SelectedStackDeleteToken? deleteConfirmation;
        private long deleteConfirmationGeneration;
        private readonly List<object> deleteConfirmationBinders = new List<object>();

        private void PrepareSelectedStackDelete()
        {
            if (!IsOpen || ownerManifest == null || advancedApi == null) return;
            ResetDeleteConfirmation();
            long generation = deleteConfirmationGeneration;
            SelectedStackDeletePreparation preparation = advancedApi.PrepareSelectedStackDelete(ownerManifest, generation);
            if (preparation.Token == null)
            { SetStatusMessage(T("debug.delete." + preparation.Reason, "Select a backpack quick slot and try again."), false); return; }
            SelectedStackDeleteToken token = deleteConfirmation = preparation.Token;
            try
            {
                deleteConfirmationRoot = CreateUiObject("DTMAPI.DebugConsole.DeleteConfirmation", root!);
                object shade = AddComponent(deleteConfirmationRoot, imageType!);
                SetProperty(shade, "color", Color(0, 0, 0, 0.75f));
                SetRect(deleteConfirmationRoot, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(0, 0));
                object panel = CreateUiObject("DTMAPI.DebugConsole.DeleteConfirmation.Panel", deleteConfirmationRoot);
                object background = AddComponent(panel, imageType!);
                SetProperty(background, "color", Color(0.08f, 0.09f, 0.10f, 1));
                SetRect(panel, Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(540, 270));
                string message = string.Format(CultureInfo.CurrentCulture,
                    T("debug.delete.confirmText", "Delete {0} × {1} (the entire selected stack)?"), token.DisplayName, token.Quantity);
                if (token.HasContents) message += "\n" + T("debug.delete.includesContents", "Includes all items inside the container.");
                message += "\n" + T("debug.delete.savePolicy", "This change follows the normal game save; no automatic save.");
                object label = AddText(panel, "DTMAPI.DebugConsole.DeleteConfirmation.Message", message, 20,
                    Color(1, 0.94f, 0.88f, 1), TextAnchorMiddleLeft, 24, -20, 492, 164);
                SetProperty(label, "supportRichText", false);
                List<object>? previous = activeBinderSink;
                activeBinderSink = deleteConfirmationBinders;
                try
                {
                    CreateButton(panel, "DTMAPI.DebugConsole.DeleteConfirmation.Cancel", T("debug.delete.cancel", "Cancel"),
                        () => CancelSelectedStackDelete(token, generation), Color(0.20f, 0.25f, 0.28f, 1), Color(1, 1, 1, 1), 24, -200, 226, 48);
                    CreateButton(panel, "DTMAPI.DebugConsole.DeleteConfirmation.Confirm", T("debug.delete.confirm", "Delete entire stack"),
                        () => ConfirmSelectedStackDelete(token, generation), Color(0.54f, 0.17f, 0.13f, 1), Color(1, 1, 1, 1), 290, -200, 226, 48);
                }
                finally { activeBinderSink = previous; }
            }
            catch { ResetDeleteConfirmation(); throw; }
        }
        private bool OwnDeleteConfirmation(SelectedStackDeleteToken token, long generation) => IsOpen &&
            ReferenceEquals(deleteConfirmation, token) && deleteConfirmationGeneration == generation;
        private void CancelSelectedStackDelete(SelectedStackDeleteToken token, long generation)
        {
            if (!OwnDeleteConfirmation(token, generation)) return;
            ResetDeleteConfirmation();
            SetStatusMessage(T("debug.delete.cancelled", "Deletion cancelled."), true);
        }
        private void ConfirmSelectedStackDelete(SelectedStackDeleteToken token, long generation)
        {
            if (!OwnDeleteConfirmation(token, generation) || ownerManifest == null || advancedApi == null) return;
            // Detach only this UI before native notification can open a successor confirmation.
            deleteConfirmation = null;
            DestroyDeleteConfirmationView();
            deleteConfirmationGeneration++;
            SelectedStackDeleteResult result = advancedApi.ConfirmSelectedStackDelete(ownerManifest, token, generation);
            if (deleteConfirmationGeneration != generation + 1 || deleteConfirmation != null || !IsOpen) return;
            SetStatusMessage(T("debug.delete." + result.Outcome, "Deletion could not be confirmed. Check the original slot."), result.Outcome == "removed");
        }
        private void CheckDeleteConfirmation()
        {
            if (deleteConfirmation == null) return;
            if (!IsOpen || ownerManifest == null || advancedApi == null ||
                !advancedApi.IsSelectedStackDeleteCurrent(ownerManifest, deleteConfirmation, deleteConfirmationGeneration))
            {
                ResetDeleteConfirmation();
                if (IsOpen) SetStatusMessage(T("debug.delete.invalidated", "Target changed; start a new confirmation."), false);
            }
        }
        private void ResetDeleteConfirmation()
        {
            SelectedStackDeleteToken? token = deleteConfirmation;
            deleteConfirmation = null;
            deleteConfirmationGeneration++;
            if (token != null) advancedApi?.CancelSelectedStackDelete(token);
            DestroyDeleteConfirmationView();
        }
        private void DestroyDeleteConfirmationView()
        {
            ReleaseOwnedBinders(deleteConfirmationBinders);
            object? previous = deleteConfirmationRoot;
            deleteConfirmationRoot = null;
            if (previous != null && !IsDestroyed(previous)) Destroy(previous);
        }
    }
}
