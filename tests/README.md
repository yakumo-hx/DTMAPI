# Tests

| Executable project | Current coverage |
| --- | --- |
| DTMAPI.UnitTests | Core/runtime contracts, product logic, compatibility fixtures and source boundaries; focus currently selects execution within a broad compilation graph |
| DTMAPI.QaUnitTests | QA routing and synthetic observations |
| DTMAPI.AuthorSdk.Tests | SDK target, build, package, CLI and author-session behavior |
| DTMAPI.InstallDoctor.Tests | Installation inspection and diagnostics |
| DTMAPI.MultiPlatformInstaller.Tests | Installer target, path and transaction behavior |
| DTMAPI.AbiCompatibilityHarness | Retained consumer ABI execution |

IntegrationTests is a README placeholder. Independent net48 fixtures test assembly/host behavior; they do not prove Unity Mono or player behavior. Test graph splitting is recorded in its implementation Update when implemented.

Select checks using [product validation](../docs/workflows/product-change-validation.md) and [script commands](../tools/scripts/README.md#choose-validation). Game runs use the shared runtime lock and matching scenario. Shared temporary sessions use `Shared/DtmApiTestSession.cs`; ordinary tests need no extra manual cleanup cycle.

QA and negative Mod fixtures live under `mod-fixtures`; author examples and samples remain in the SDK tree.
