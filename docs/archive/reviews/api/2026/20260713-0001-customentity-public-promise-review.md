# 20260713-0001 CustomEntity Public Promise Review

Status: recorded / E1 selected / implementation open
Date: 2026-07-13
Scope: current C# CustomEntity public surface, implemented registry behavior, protected JSON animal route, compatibility window, and future native-owner boundaries
Related decision docket: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0002-second-round-closure-third-decision-docket.md`

## Source Request

After closing the second decision round, the user asked to continue the major-update boundary discussion. The prior docket deferred whether DTMAPI should formalize JSON creation, keep blocked C# placeholders indefinitely, or commit to runtime creation for animals, monsters, attacks/projectiles, and drones.

This is a source/API review. It does not change public signatures or status, delete a provider, alter the protected JSON+PNG+WAV animal route, implement native entities, or launch the game.

## Current Public Surface

`src/DTMAPI.Abstractions/CustomEntities.cs` currently contains 964 lines and 97 public types, including nine public interfaces. Four family APIs mix two very different promises in the same interface:

- definition registration, unregistration, lookup, snapshots, status, and lifecycle listeners;
- native runtime verbs such as animal/monster `RequestSpawn`, attack `SpawnProjectile` / `ExecuteAttack`, and drone `RequestSummon` / `Equip` / `SetMode`.

The public API matrix describes the registry portion as `StableCandidate` and runtime creation as `Experimental / blocked`. That member-level distinction cannot be expressed by the interface identity seen by a binary consumer, IDE, or reflection scan. Obtaining `ICustomAnimalApi`, for example, exposes both the registry and the unimplemented spawn promise.

## Implemented Behavior

`CustomEntityRegistryService` implements all four APIs, but current production behavior is a Core registry rather than a native entity runtime:

- validation checks basic owner/id/null/duplicate conditions;
- definitions are stored in dictionaries, returned through lookup/snapshot routes, and removed by owner cleanup;
- native request verbs always return `runtime-creation-blocked` / `RuntimeCreationBlocked`;
- no production path creates active instance entries for animals, monsters, attacks, or drones;
- many public provider/tick/save/migration/feed/combat/lifecycle concepts have no native producer or production call site;
- current smoke verifies registration and the expected blocked results, not native creation.

Even the registry should not be promoted from its present evidence. It does not yet validate the family-specific policies implied by the DTOs, prove owner-qualified definition identity beyond the shallow id shape, or provide immutable definition snapshots. A large speculative DTO surface therefore cannot become stable merely because dictionary registration works.

## Protected JSON Animal Route Is Separate

The stable manual behavior baseline for custom husbandry animals does not use these C# definitions or runtime verbs.

Its working path is:

```text
official animal/item/store/produce JSON and animal bag
  -> native creation/save/economy owners

Content/DTMAPI/custom-animals.json + PNG
  -> internal animation/template/AI safety bridge

Content/DTMAPI/audio-replacements.json + WAV
  -> reviewed internal short-SFX animal voice bridge
```

`CustomAnimalSpeciesDefinition` is not the serialization contract for the working content route. The protected route does not establish monsters, attacks/projectiles, drones, arbitrary C# spawning, or the unimplemented behavior-provider lifecycle.

Do not force JSON content definitions into the old C# registry merely to claim one unified architecture. That would bind a working official-content path to speculative DTOs.

## Known Consumer Boundary

Repository projects contain no ordinary product adopter of the four C# CustomEntity APIs. Read-only scans of the current workspace and locally present official/Workshop CodeMod assemblies found the interface names only in DTMAPI assemblies.

This means no known adopter was found. It does not prove every historical, private, or external binary is absent. The 0.5.5 old-DLL compatibility gate therefore still applies.

## Options

### E1 - formal JSON animal route; retire the C# umbrella after compatibility

- Formally support custom husbandry animals through the existing official-JSON + DTMAPI PNG/WAV content route.
- Freeze the current generic C# CustomEntity umbrella and add no new adopter/template/capability.
- In 0.5.5 preserve binary types/provider registration and blocked behavior, but mark the surface or at least the runtime verbs Deprecated/Frozen with owner-scoped warnings.
- After a published warning-bearing cycle and renewed consumer scan, remove or replace the umbrella only at an explicit breaking API boundary.
- If C# query demand later exists, design narrow read-only DTOs over verified loaded content instead of reusing speculative runtime DTOs.

This is the recommended lightweight/public baseline.

### E2 - permanent blocked placeholders

Keep all 97 public types and four mixed interfaces indefinitely so the platform may implement them later.

This avoids a future removal but converts unimplemented save/AI/combat/projectile/drone promises into permanent documentation, testing, ABI, and architecture obligations. It is suitable only as the temporary 0.5.5 compatibility state, not the recommended endpoint.

### E3 - commit to generic runtime creation

Commit now to implementing all four families behind the present umbrella.

This is not recommended. Animal, monster, attack/projectile, and drone responsibilities have different native owners, save policies, AI, collision, damage, equipment, and lifecycle gates. Future demand should create four independent native-owner projects. Selecting E1 now does not prevent one of those projects later; it prevents today's speculative common DTOs from pre-authorizing all four.

## Compatibility Requirement Regardless Of Decision

DTMAPI 0.5.5 cannot physically delete these shipped public types or stop registering their providers. The current 0.5.5 gate requires an approved zero-deletion public diff and representative old DLL loading.

The earliest safe E1 sequence is:

1. freeze and document the umbrella in 0.5.5;
2. keep all members/provider ids and existing `runtime-creation-blocked` behavior;
3. emit bounded owner-scoped deprecation warnings and publish migration guidance;
4. complete at least one actual warning-bearing release cycle;
5. rescan known Workshop/OfficialLocal/external consumers;
6. remove only in an explicit breaking API epoch, with a compatibility facade when real consumers require one.

For animals the migration target is the content route. Monster/Attack/Drone currently have no replacement; migration text must say that plainly rather than pretending feature parity exists.

## Work-Order Consequence

```text
Batch 0  freeze identity/API baseline and record known consumers
Batch 2  include warnings and zero-deletion in the 0.5.5 binary matrix
Batch 3A make Author SDK own animal JSON schema/templates/validation/B1 reload
Batch 3B move CustomEntity smoke into optional QA
Batch 4  remove CustomAnimals polling independently through B0 lifecycle loading
Batch 6  evaluate physical C# retirement after the warning window
Future   start Animal / Monster / Attack-Projectile / Drone separately on demand
```

## Decision Boundary

The user selected **E1** on 2026-07-13, with **E2 only as the 0.5.5 transition state** and **E3 split into four possible future projects rather than one present commitment**. Implementation remains open and cannot delete the public surface during 0.5.5.
