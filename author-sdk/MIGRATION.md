# Migrating frozen API consumers

The current SDK/API/Runtime candidate is 0.7.0, still unpublished and pending final acceptance; published Runtime remains 0.6.1. Existing 0.5.5 payloads and old DLLs remain compatibility inputs; do not relabel their manifests or replace their hashes to make them look newly built.

The bundled [API status](API-STATUS.md#07-candidate-migration-and-removal-preparation) projects the authoritative family status, alternatives, earliest breaking series and actual notice/removal dates. No new removal date has been announced by this preparation. The matrix records the historical CSV physical-removal exception separately; this candidate does not restore that API or perform another ABI deletion. A player product provides its own behavior; it is not a drop-in programmable replacement for a frozen API.

1. Keep the original package, DLL, manifest, SDK/target and their hashes before rebuilding. Run the read-only Player Doctor against the exact package or installation. A missing reference in this bounded scan cannot establish that no outside author uses an API.
2. Read both stability and disposition. A successful `GetApi` on a Disabled/Frozen contract does not promise native behavior. In particular, Lamp returns a disabled shell, native custom-entity creation remains blocked, and some retained interfaces have no provider.
3. Choose the documented player product, a separately verified Advanced implementation, or an explicit unavailable branch. Do not migrate CameraZoom to CameraView: both are frozen. Public helper/context/owner-data services are not substitutes for missing gameplay capabilities.
4. Preserve the author's UniqueID and existing data when updating a package. Removing a provider query does not authorize deleting configuration, global data, native saves, or their readers. Keep the original package for a cold rollback.

## A runnable unavailable migration

Use the public CLI to create an ordinary project, then replace its generated Entry body with the example below and run `pack`. The example intentionally has no replacement lighting behavior and never queries the Lamp API. This is the supported result when the selected family has no replacement public capability.

```text
dtmapi-author new codemod LampMigration --id Linden.OldLampConsumer --name "Lamp migration" --author Linden --api-target 0.6.4
dtmapi-author pack LampMigration --json
```

```csharp
using DTMAPI.Abstractions;
namespace Linden.OldLampConsumer;
public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.Monitor.Log("Lamp control is unavailable; this version performs no lighting changes.");
    }
}
```

For an existing project, retain its ID and deliberately increase its manifest Version instead of overwriting the original source/package. `install-local` requires the package report's exact ID, version and SHA-256. Exit the game before updating code; compare the next process's loaded source/version. Use `withdraw UniqueID --game-root PATH` to withdraw the owned package, then install the retained original with its original hash for rollback. Package withdrawal and owner-data deletion are separate operations.

Runtime's new candidate warning names the consumer and obsolete requested contract once per owner activation. Repeated lookups do not produce repeated reminders; a diagnostic sink failure does not change the API result. Doctor's metadata warning is useful even when a Mod never reaches Entry. Neither warning means that every old-target Mod is incompatible.

The repository's PN-033 evidence retains a 0.5.5 Lamp consumer and a same-ID 0.6.4 unavailable migration built by these public paths. Their package compile results, exact binaries and subsequent Mono results are separate evidence; a successful build alone is not runtime acceptance.
