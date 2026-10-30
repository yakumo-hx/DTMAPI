# Experimental modal sessions (0.7.5 candidate)

Select the unpublished `0.7.5` API target and matching candidate Runtime. Existing published targets and the default target are unchanged. Obtain this optional capability from your own helper:

```csharp
IUiModalSessions modals = helper.UI as IUiModalSessions
    ?? throw new InvalidOperationException("This Mod requires modal sessions from Runtime 0.7.5.");
DtmModalOpenResult result = modals.OpenModal("Author.MyMod.Panel");
IUiModalSession? session = result.Session;
if (session == null)
    helper.Monitor.Log("Panel was not acquired: " + result.Status);
```

`Opened` acquires a new generation. `AlreadyOpen` returns a handle to the same owner's existing menu and generation; it is not a second lease or a reference count. `Busy` means another menu/owner holds the platform occupancy. `Interrupted` means synchronous `MenuOpened` callbacks closed, replaced or deactivated the new generation. The latter two results have no session. IDs are trimmed and matched without case; an empty menu ID retains the legacy `DTMAPI.Custom` default.

`OwnerId`, `MenuId` and `Sequence` identify the acquired generation. `IsOpen`, `Close()` and `Dispose()` run on the Runtime thread. `Close()` returns whether it released that exact generation; a callback may already have opened a successor by the time it returns. Stale or repeated release returns false/does nothing and cannot close that successor. Handles can be queried/disposed after owner deactivation; the stale helper cannot acquire another session. Owner deactivation releases any remaining occupancy.

The real Core event dispatcher isolates and diagnoses subscriber exceptions. They do not roll back an already opened or closed occupancy. Always use the returned result and the handle's current state, including after callbacks. A handle does not create or render a Canvas, observe arbitrary native menus, pause world time, or block native input by itself. Your product retains its focus, rendering, native input and bounded drain responsibilities. The existing DTMAPI input audience continues to use the same overlay sequence and owner.

Finish cleanup for the old presentation and its effects before releasing its handle: `MenuClosed` can synchronously reenter your product. Retain retryable native restoration responsibility if cleanup fails. Never reacquire a menu just to make a failed close look like it rolled back, and never keep only a menu string for delayed cleanup.

DebugConsole uses this API, and the Mod Manager exercises platform conflict handling. The modal-session and related UI/input APIs remain Experimental.
