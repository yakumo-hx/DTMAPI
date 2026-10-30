# DTMAPI API status for authors

This guide describes the 0.7.5 source baseline. A source checkout or successful build does not establish that this version has been published. Reference the API DLLs you use and declare the minimum Runtime your Mod requires. SDK target catalogs do not control compilation or loading.

## Status vocabulary

| Status | Meaning for a Mod author |
| --- | --- |
| Stable | Supported public contract. Ordinary Mods may depend on it. |
| StableCandidate | Implemented, but not yet a full stability commitment. Check the selected target. |
| Experimental | Version-sensitive API. Handle unavailable services and failed operations. |
| Diagnostic | Developer or troubleshooting surface; not a gameplay stability guarantee. |
| Frozen / Deprecated | Retained legacy surface. Do not build new gameplay integrations on it. |
| Proposed / Retired / Disabled | Not a supported implementation to depend on. |

## Supported authoring paths

Use ordinary CodeMods for DTMAPI APIs and product-specific native integration through [native references](NATIVE-REFERENCES.md). All CodeMods use the same runtime loading path. Content Packs use explicit optional [content hosts](CONTENT-HOSTS.md). Availability of a DTO or interface does not promise that its host is installed or that a native operation succeeds.

| Surface | Current status and boundary |
| --- | --- |
| `DtmMod`, manifest/dependency identity, `IDtmHelper`, minimum Runtime checks | Stable |
| `IMonitor.Log`, `LogOnce`, `LogException`; basic configuration read/write/path | Stable |
| Entry type selection, config migrations, Mod registry, translations | StableCandidate |
| Game launch, save loading/saving/saved, title return, update events | GameLaunched is StableCandidate; other lifecycle events are Experimental. Only a successful native save commits gameplay state. |
| Optional runtime context, scheduling, owned resources, commands | Experimental; obtain optional capabilities and release owned resources. |
| Owner files, global data, versioned settings, input diagnostics | Experimental; global data is not a substitute for save-bound gameplay persistence. |
| Keybinds, input scopes, modal sessions | Experimental; respect input ownership and release registrations. |
| Config-menu registration and query | StableCandidate; optional reset/default-keybind capability is Experimental. |
| Status pages and other UI helpers | Experimental |
| Diagnostics snapshots, evidence and log export | Diagnostic; query/report availability does not establish gameplay success. |
| Workshop discovery and content queries | Experimental; official enablement and supported source locations apply. |
| `IItemDisplayNameApi`, `IMailDeliveryApi`, crop harvesting | Experimental; check operation results and host availability. |
| Monster, audio and other optional content modules | Experimental; use the host-specific contracts and minimum versions. |
| Historical product gameplay APIs (action speed/completion, fishing, tooltip/viewer, equipment, saves, chest locator, planting gun, camera) | Frozen / Deprecated; current independent Mods own their gameplay implementations. |
| Historical custom-entity creation, lamp and vehicle surfaces | Frozen, disabled or retired according to the selected target; a retained signature is not proof of a working native implementation. |

## Compatibility

Compatibility maintenance and acceptance for API targets before 0.6.0 have ended. Retained old types or assembly identity `0.5.3.0` do not extend that support promise. Current Mod versions and current save formats are evaluated independently of those old target numbers.

Use [migration notes](MIGRATION.md), [platform services](PLATFORM-SERVICES.md), [modal sessions](UI-MODAL-SESSIONS.md) and [audio content](AUDIO-CONTENT.md) for concrete author workflows. Internal research and release records are not SDK build inputs.
