# Explicit author sessions

The current source implements descriptor/envelope schema 2 and session protocol 1.0.
This is an internal implementation contract; release availability is owned by the
repository Product Catalog and its release records. This SDK is the unpublished 0.7.0 candidate; published Runtime remains 0.6.1.
The compiler target, SDK version, installed Runtime version and session protocol are separate values.

## Prepare and hello

`session prepare --game-root <path>` writes one startup descriptor and a protected
client credential. It does not start the game or assume a Host version. Both files
bind a canonical game root, GUID-N session ID, derived pipe name, random token and
ten-minute UTC lifetime. Windows uses a protected current-user ACL; Unix uses 0600.

Schema 2 adds `protocolMajor`, `minimumMinor`, `maximumMinor`, `apiTarget`,
`minimumRuntimeVersion`, `requiredCapabilities` and `optionalCapabilities`.
The current writer requests protocol 1.0, the existing frozen compiler target,
the session contract's minimum Runtime, required `get-source-snapshot/1` and
optional `reload-content/1`. There is no offline `hostVersion` or legacy
`runtimeVersion` field. The descriptor is consumed atomically once at startup;
an absent descriptor creates no listener, watcher or polling loop.

The Host validates credentials, paths and expiry, then selects the highest minor
in the intersection of both protocol ranges. Major mismatch, an empty minor
intersection, an unmet minimum Runtime or an unknown required capability rejects
the descriptor. Unknown optional capabilities are reported without enabling them.
The first wire request must be an authenticated `hello` carrying the descriptor's
same offer. Its response contains the selected major/minor, actual `hostVersion`,
`apiTarget`, `acceptedCapabilities` and `unsupportedOptionalCapabilities`.

Each CLI invocation repeats hello with a fresh request ID and the original offer.
The Host returns the same selection; changing the offer cannot renegotiate an
existing session. After the first successful hello, the SDK atomically replaces
the protected client credential with a copy containing its `negotiated` result.
The startup descriptor is never recreated. Later CLI invocations and responses
must match this result, including actual Host identity and capability lists.

## Business messages and lifecycle

Candidate source additionally supports optional `execute-command/1`, requested only
by `session prepare --commands true`. `session command --command-line <text>` uses
an existing session and selected owner. The schema-2 request includes bounded
`commandLine`; the Host reuses the current pending-request/dedup lifecycle while the
single Runtime scheduler executes the synchronous handler. Completion carries the
same request ID and owner. Deadline or session close cancels work that has not begun;
running work completes without a rollback claim. This is independent of public API
target availability; see [candidate service semantics](PLATFORM-SERVICES.md).

Schema 2 requests carry the offer fields plus `gameRoot`, `session`, `token`,
`requestId` and `operation`. Business requests additionally bind the selected
`hostVersion`/`protocolMinor`, `uniqueId`, `selectedRoot` and `expectedTreeSha256`.
Responses bind schema, protocol, Host, root, session, request, operation and owner.
Only accepted capabilities authorize their corresponding operations. Hello is
handled by the transport; business operations remain in the bounded Runtime-thread
queue. Duplicate request IDs are rejected, including hello retries with an old ID.

`snapshot` reads the currently selected source. `reload` checks selected root,
current tree, active identity and package metadata; it only refreshes supported
non-code content. DLL changes, unknown formats or changed manifest/package identity
require restart. The package's compilation target is checked against the existing
SDK package contract, independently of the Host release.

Unknown optional JSON fields are ignored; duplicate critical fields are rejected.
Descriptor/request limits remain 32 KiB, responses 64 KiB, capabilities 32 per list,
and the replay window 4,096 request IDs. Existing pending/concurrent limits and
Runtime deadlines remain. Closure revokes acceptance, clears queued work, replay
state and Host credentials, and closes listeners. Token values never enter reports.

## Compatibility and diagnostics

Schema 1 accepts only the known legacy `runtimeVersion` value `0.5.5`. It has no
hello. Its request and response `runtime` fields keep that exact per-session wire
value even on a newer Host. Actual Runtime version appears separately as the
`hostVersion` status value. The old response envelope has no new top-level fields,
so the existing SDK's strict response reader remains compatible.

New SDK commands never silently write or downgrade to schema 1. An existing legacy
credential can be cleared or replaced after expiry; a new command asks the author
to clear/prepare before using schema 2. This does not migrate the project's compiler target.

| Status | Evidence and action |
| --- | --- |
| `host-unavailable` | No listener is available, or connection failed. Start the game and check logs/versions. On Windows, a timed-out connect distinguishes a missing named pipe using the local pipe API. |
| `handshake-timeout` | Connection/hello did not finish before the deadline. No Host version or upgrade requirement is inferred. |
| `upgrade-required` | An identity-checked hello response explicitly reports protocol/capability/minimum-version incompatibility. |
| `pipe-response-identity-mismatch` / `pipe-response-negotiation-mismatch` | Response identity or an established selection changed; no business request is accepted as successful. |
| `restart-required` | The authenticated business result requires a game restart; it is not a successful hot reload. |

An old Host that consumes/rejects schema 2 without creating a listener is
indistinguishable from a Host that has not started using absence alone. Both
remain unavailable/timeout diagnoses, never an inferred upgrade requirement.

Wire shapes are defined in [descriptor schema](schemas/author-session-descriptor.schema.json)
and [JSONL schema](schemas/author-session-wire.schema.json).
Focused validation uses `DTMAPI_AUTHOR_SDK_TEST_FOCUS=platform-session-handshake`
in `DTMAPI.AuthorSdk.Tests` and `DTMAPI_UNIT_TEST_FOCUS=platform-session-core`
in `DTMAPI.UnitTests`. These functions also run in their default suites.
`DTMAPI_AUTHOR_LEGACY_EXE` optionally supplies a previously built SDK executable
for an additional real old-client/Host/old-response-validator test; its absence is
printed explicitly. Fixtures and credentials stay inside `DtmApiTestSession`.
Unity Mono author-flow acceptance remains a separate platform integration gate.
