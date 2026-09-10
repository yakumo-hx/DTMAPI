# Platform services for internal API 0.6.2

SDK 0.6.2 includes API 0.6.2 as its default internal candidate, combining the previously validated M2 and reflection slices.
These services remain Experimental; SDK and Runtime 0.6.2 have not been published.
Explicit API 0.5.5 projects retain their original payload and cannot call these services.
Use the normal SDK build and its matching payload; never reference installed Runtime
DLLs to make an author project compile. The target catalog owns availability.

## Service acquisition and lifetime

During `Entry`, use `helper.GetRequiredService<T>("0.6.2", "0.6.2")`, or
`helper.GetOptionalService<T>()` when the service is optional. Service acquisition requires
the Runtime thread and an active owner. The exact contract Type determines lookup;
there is no name-based lookup or Mod API registry fallback.

`IDtmRuntimeContext.Snapshot` is immutable and may be read from a background thread;
it contains no native object. `IsMainThread` describes the calling thread. Snapshot
publication precedes synchronous `Subscribe` notifications. A failed load attempt
still consumes a save epoch, and a room transition invalidates the world epoch
before publishing the next ready world. `SaveLoaded` remains the old load callback;
it does not imply `WorldReady`. Neither epoch is a persistent save ID.

## Scheduling and owned resources

`IDtmScheduler.Post`, `NextTick` and `Delay` accept synchronous Actions from any
thread. Choose `CurrentWorld` for native world work, `CurrentSaveSession` for work
bound to one load attempt, and `ModOwner` for owner-wide work. Enqueue captures the
current epoch. Unavailable scopes reject immediately; a scope change cancels work
that has not started. `NextTick` starts after the current Core tick; work submitted
inside a callback cannot run in that same drain. Delay and queue timeout measure
monotonic elapsed time. Timeout limits waiting to start, not callback duration.

Each handle has one terminal result. Cancellation cannot interrupt a running Action.
Callbacks share the Runtime frame budget, so keep them short. Completion Tasks run
continuations asynchronously and do not marshal back to Unity's thread. Finish
background work first, then Post the result. Do not use `async void`: SDK203 catches
directly recognizable async lambdas/method groups passed to scheduler, context,
command and language callbacks. It does not trace delegates stored in variables.

`IDtmOwnedResources.Register` registers an `IDisposable` on the Runtime thread and
returns a token. `Unregister` detaches without disposing; token `Dispose` requests
cleanup. Background disposal waits for the Runtime drain. Scope invalidation and
owner closure dispose in reverse registration order, isolate exceptions and retain
failed resources for retry. Make disposal idempotent. A closed owner/token never
becomes active when a Mod with the same ID is loaded again.

## Commands

`IDtmCommands.Register("status", handler, scope, description, parameters, alias)`
owns the canonical `UniqueID/status` name. Names and aliases compare without case;
alias conflicts fail without overwriting the first registration. `GetHelp` is a
read-only snapshot. `Execute` uses the same scheduler and scope checks as other
work. Handlers receive a request ID, owner ID, read-only arguments and `WriteLine`.
Output is bounded to 32 lines/4096 characters. The parser supports quoted arguments,
backslash escapes and empty quoted arguments; input is not a shell or script.

The existing title Status page shows a command field when commands are registered.
Use `help` for names; visible output is short and full output goes to the log.
For an authenticated author session, prepare with `--commands true`, then use
`session command <UniqueID> <selectedRoot> --game-root <path> --command-line "UniqueID/status"`.
This optionally negotiates `execute-command/1`. Existing selected-source, tree,
owner and request-ID checks remain required. A command cannot target another owner
through that session. Queued timeout/session close cancels unstarted work; already
running work is not rolled back.

## Versioned configuration

`IVersionedConfigHelper` is Runtime-thread and owner bound. `Read<T>(schema, validate)`
returns `Missing` for a new configuration; explicitly write defaults with
`Write(defaults, schema, validate)`. Register each `n → n+1` step with
`RegisterMigration<T>(n, value => migratedValue)`. Versions start at 1. The same
serializable model type is used across the chain; preserve fields needed for migration.

Migrations work on a candidate, which is validated before one atomic commit.
Validation returns null/empty for success or an error string. Validation receives
an isolated value and must be a pure check; mutations are not committed. A successful
read at the already committed version does not repeat migration. `Corrupt`,
`UnsupportedSchema`, `MigrationRequired`, `MigrationFailed`, `ValidationFailed`,
`AccessDenied` and `IoError` preserve existing bytes and are distinct from `Missing`.
Do not treat them as permission to write defaults. No automatic repair overwrites
bad or future data. Owner closure releases migration callbacks.

Versioned config lives in the Runtime's configuration area, separately from legacy
`ReadConfig` files. Adopting it does not silently import or rewrite the legacy file.
An author can explicitly read a legacy model, validate it and write the new config
only when the versioned result is `Missing`. Old migration Actions retain their old
per-read behavior. Configuration is independent of gameplay save/rollback semantics.

## Input and translations

Keep using the existing owner `Input` registrations, scopes and suppression API.
`IDtmInputDiagnostics.Snapshot` describes the most recently frozen input audience.
`GetRegistrations` returns only this owner's immutable binding descriptions;
`FindConflicts` returns potentially overlapping bindings with owner/name/scope.
Conflict information does not choose a winner. Shared modifiers alone are not a
conflict; generic Control/Shift/Alt include physical left/right variants. Scope,
platform/owner UI focus, suppression and physical-neutral rearming still govern
delivery. Controller coverage is limited to backend paths actually supported and
tested; these DTOs do not add a device driver.

`IDtmTranslations.Get` uses this package's catalogs and the existing locale →
English → explicit fallback/key behavior. Parameters use `{name}`; `{{` and `}}`
emit literal braces. Replacement is a single pass; missing parameters remain visible
and are listed in `MissingParameters`. Names are case-sensitive. Subscribe with
`SubscribeLanguageChanged` and refresh visible author UI in that synchronous
Runtime-thread callback. There is no initial notification. Regional language changes
are distinct even when English fallback is shared. Dispose the subscription or close
the owner to detach it. Reads also require the Runtime thread, because locale selection
may sample native language state.
