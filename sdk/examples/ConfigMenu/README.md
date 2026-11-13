# ConfigMenuExample

Sample mod for every supported config menu option and save/reset behavior.

The 0.7.5 example also casts the existing `IDtmConfigMenuApi` facade to the optional Experimental `IDtmConfigMenuDialogsApi`. `TryShowDialog` accepts a `DtmConfigDialog` and a selection callback; it returns false if the surface is busy or no native menu can host it. Its out session is non-null only on success. No native/Unity object is passed by the author.

Buttons have stable IDs, labels and an enabled flag. Cancel/default IDs must identify enabled buttons; Cancel invokes the designated choice, while `Close()`/`Dispose()` only dismiss the exact session and never invokes a business action. Callbacks run once on the Runtime thread. Owner withdrawal, save/title transitions and invalidated parent menus close the dialog and release callbacks. The underlying configuration page remains open after dismissing its child dialog. Keep the returned session if the Mod's own UI can disappear before its owner unloads, and dispose it at that boundary.

Use a single acknowledgement button for notices, or multiple buttons for choices. Pass `size: new DtmConfigDialogSize(480, 300)` to set logical width/height; omission uses 820×570. The minimum is 320×200; larger windows fit the viewport. Long content and button lists paginate within that space. A busy result is not queued: retain an explicit retry/reopen action where needed.

For restart requirements, cast the same facade to Experimental `IDtmConfigMenuRestartApi`. Call `RequireRestartForLastOption(manifest)` immediately after adding the relevant value option at startup, while its getter still returns the value used by this process. The example declares its Mode option. This captures the startup baseline once; it does not change the setting's apply logic. No custom save-time popup is needed: successful saves update the platform's pending list, and closing the menu displays one reminder grouped by Mod. Cancelled drafts and failed saves are excluded. Saving the startup value removes the entry; acknowledgement suppresses unchanged reminders, and owner removal clears the owner's state. The core switches follow this same contract.
