# 20260708-0002 ResourceLifecycleLedger Cross-Mod GC Review

## Scope

User asked to migrate the AutoFishing `ResourceLifecycleLedger` accumulation suspicion to other mods/features and check whether the same issue exists elsewhere. The user also requested an independent sub-agent review without giving it the player observation that long runs may become worse after enabling AutoFishing.

This is source/document review only. No game launch, no runtime lock, and no source implementation change.

## Short Answer

The suspicion does migrate, but it splits into two different shapes:

1. AutoFishing has the clearest record-growth shape: repeated gameplay loop -> unique native handle id -> `SaveLifetime` + `BorrowedNative` ledger record -> released record remains in the current open save generation -> later ledger updates copy/sort a growing record set.
2. AudioReplacement and CustomAnimals have the clearest per-frame diagnostic-churn shape: stable or bounded records, but every-frame skipped refresh still enters the ledger, builds a full snapshot, formats summary, and updates feature status.

So the player observation is plausible but not proven. AutoFishing has a structural difference that could make long AFK fishing worse than ordinary long-running mods: it can add new released `SaveLifetime/BorrowedNative` records per fish/native handle, while most other ledger users tend to converge on content/asset definitions. But other mods are not clean: AudioReplacement and CustomAnimals can create persistent snapshot/status pressure even without record growth.

## Independent Blind Review

I spawned an explorer sub-agent with only this task: inspect `ResourceLifecycleLedger` / `ObserveResourceLifecycle` / `ReleaseResourceLifecycle` usage across DTMAPI, compare AutoFishing to other features, and report long-run GC risks. I did not tell it that a player observed AutoFishing as a possible differentiator.

The blind review independently reached the same split:

- AutoFishing is "record growth + release log/snapshot pressure" because it maps a repeated gameplay loop into unique `SaveLifetime + BorrowedNative` native-handle records.
- AudioReplacement and CustomAnimals are "stable record set + high-frequency refresh snapshot/status pressure" because every-frame skipped refresh still enters `ObserveResourceRefresh`.
- Other feature mods may have their own lifecycle risks, but they do not directly call `ResourceLifecycleLedger`.

The blind review also added one useful caution: `TitleIdleResourceGrowth=Passed` proves the configured resource-growth diagnostic did not fire, but does not prove skipped refresh had zero allocation cost.

## Ledger Mechanics

`DtmApiRuntime.ObserveResourceLifecycle`, `ReleaseResourceLifecycle`, `ObserveResourceRefresh`, and `ObserveResourceCleanup` all call into `ResourceLifecycleLedgerService` and then `PublishResourceLifecycleLedgerUpdate`.

Every update builds a full snapshot. `BuildSnapshotNoLock()` copies diagnostics, copies the timeline, orders all records by kind/id into an array, and orders refresh counts into a dictionary.

Every publication formats the snapshot summary and replaces feature statuses. Release, cleanup, generation, and non-skipped refresh operations also log. Skipped refresh avoids the log branch, but still builds the snapshot and status payload.

Save-lifetime pruning only removes records from older save generations. Released records in the currently open save generation remain present.

## Call-Point Map

| Area | Direct ledger use | Frequency shape | Record growth shape |
| --- | --- | --- | --- |
| Runtime core | phase, generation, content signature, cleanup, snapshot summaries | lifecycle/save-load/report | bounded by lifecycle events |
| AutoFishing / FishingAutomation | owner policy/state, minigame handle, Ready charge state, animator snapshot, hook physics snapshot | active fishing loop, per fish/native phase | high risk: unique native handle ids in current open save |
| AudioReplacement | definitions, WAV request/clip/player, animal sound context, refresh/cleanup | every frame update plus audio/content events | mostly bounded records; high skipped-refresh churn |
| CustomAnimals | definitions, animator/AI registration, bundle/controller refs, PNG sprite, sprite override contexts, sleep follow-up contexts, refresh/cleanup | every frame update plus render/animation/content events | mostly bounded by content/sprite names; possible high observe churn |
| Other feature mods | no direct calls found in source search | not this ledger path | inspect separately for non-ledger GC risks |

## AutoFishing Difference

AutoFishing's resource ids for native handles are generated from native object type plus `RuntimeHelpers.GetHashCode`. That means a new minigame handle, Ready charge state, animator, or hook rigidbody can become a distinct ledger record.

The service does clear gameplay transients on expected boundaries:

- `FishingGameScrollBar.StopGame` removes minigame state and releases the minigame handle.
- Leaving Ready releases Ready charge resources.
- Pull/base exits restore animator/hook physics snapshots.
- Disable/save/title reset clears transient dictionaries.

But release does not delete the current-generation ledger record. In one long open save, the ledger can hold many released AutoFishing native-handle records. Each later observe/release/refresh pays a larger snapshot/sort/summary cost.

This is different from most other content-driven records, which tend to reuse stable ids such as replacement id, species id, sprite name, bundle path, or controller key.

## AudioReplacement Difference

AudioReplacement has two relevant risk shapes:

- `AnimalSoundContext` is save-lifetime and can be observed during animal voice activity. Its record id is species/stage/event, so it is more likely to converge than AutoFishing native handle ids.
- `RefreshContentPackDefinitions("update", force:false)` runs from an every-frame feature. When the loaded content signature is unchanged, it still calls `ObserveResourceRefresh` with `skipped-throttle` or `skipped-unchanged`.

That skipped refresh does not log, but still builds a full ledger snapshot and updates feature status. This is not record growth; it is steady per-frame diagnostic allocation.

## CustomAnimals Difference

CustomAnimals also has two risk shapes:

- Content definitions, registrations, bundle/controller refs, template controllers, and PNG sprites are title/content-lifetime records that generally converge on content assets.
- `RefreshDefinitions("Update", force:false)` runs from an every-frame feature and still calls `ObserveResourceRefresh` on unchanged signatures.

Additional caution: PNG sprite bridge observations may be higher frequency depending on native sprite loading/cache behavior. The record count may converge by sprite name, but observe/status/snapshot churn can still be high if the native path calls it often.

## Why Other Mods May Not Show The Same Symptom

Most testmods do not directly touch `ResourceLifecycleLedger`. They exercise GameBridge services:

- `AutoFishingMod` drives FishingAutomation.
- `ManboCardboardAudioMod` drives AudioReplacement.
- Custom animal content drives CustomAnimals.

Other mods such as ActionSpeed, AnimalViewer, Camera/Zoom, SaveSlots, EquipmentSlots, and CropHarvesting can still have normal lifecycle or hot-path GC risks, but this specific ledger accumulation path is not their direct mechanism unless they indirectly route through one of the ledger-backed services.

This explains why "other mods long-run do not necessarily GC, but AutoFishing does" is plausible as a player observation: AutoFishing has both active gameplay hot paths and unique current-save ledger record growth. But the observation remains unproven without a controlled matrix, because AudioReplacement/CustomAnimals also produce ledger pressure and the already-solved title-idle route had AutoFishing disabled in important reproductions.

## Findings

1. `ResourceLifecycleLedger` is being used as both a lifecycle ledger and a runtime diagnostic stream. The latter is risky because each entry publishes a full snapshot.
2. AutoFishing is the strongest candidate for current-save record growth because native handle ids are unique per gameplay instance.
3. AudioReplacement and CustomAnimals are the strongest candidates for every-frame skipped-refresh churn because both features call refresh from `Update`.
4. Existing `ResourceLifecycleLedger=Passed` / `TitleIdleResourceGrowth=Passed` evidence does not eliminate allocation pressure. It mainly proves no configured growth warning/error fired and no old-generation SaveLifetime record leak was observed.
5. The player observation should be treated as a useful hypothesis, not a conclusion. Blind review converged on the same structural AutoFishing difference without that observation, which strengthens the hypothesis.

## Non-Semantic Hardening Direction

1. Add a no-publish/no-snapshot fast path for skipped refresh when the area/result/count has not changed since the previous publication.
2. Split high-churn resource events from the full lifecycle ledger: aggregate counters by kind/owner/status instead of adding one full record per transient native handle.
3. For AutoFishing, sample or aggregate borrowed-native handle records per fish batch or per resource kind, and preserve only first/recent examples.
4. Avoid logging every release-like operation for known high-churn transient resources.
5. Add counters that separate `recordCount`, `releasedCurrentSaveRecords`, `skippedRefreshCalls`, `snapshotBuilds`, and per-area ledger publishes.
6. Keep gameplay semantics untouched: do not destroy borrowed native objects, do not make `EnvironmentReset` destructive, and do not remove content/feature behavior to reduce diagnostics.

## Validation Matrix

No runtime validation was performed in this review. A future validation should compare:

- AutoFishing off baseline, with ResourceLifecycle enabled.
- AutoFishing default long AFK loop.
- AutoFishing default with ResourceLifecycle disabled or high-churn sampling enabled.
- AudioReplacement/Manbo long idle and active animal-sound route.
- CustomAnimals long idle, animal render route, and PNG sprite route.

Required counters:

- Total ledger records.
- Current-save released records.
- Records by kind/owner/ownership/lifetime.
- `ObserveResourceRefresh` calls by area/result.
- Snapshot build count and max records per snapshot.
- Resource lifecycle log count.
- AutoFishing fish/minigame/Ready/Pull counts.

## Conclusion

AutoFishing remains the sharpest ResourceLifecycle-ledger suspect for long AFK fishing because it combines active gameplay frequency with unique current-save borrowed-native records. AudioReplacement and CustomAnimals also have ledger pressure, but their dominant shape is repeated full-snapshot publication over mostly stable record sets. The correct next step is not to attribute the crash directly to AutoFishing, but to instrument or harden the ledger so these two pressure shapes can be separated.

