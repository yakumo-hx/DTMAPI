# Test Mods

Test mods are small DTMAPI mods used to prove runtime behavior inside Doloc Town.

- `HelloDtmMod`: minimal load and logging proof.
- `BrokenManifestMod`: manifest and dependency error handling.
- `ConfigMenuExample`: config menu option coverage.
- `ActionSpeedMod`: first DTMAPI-native migration slice for ActionSpeed config/input/update/restore behavior.
- `OneActionCompleteMod`: DTMAPI-native config/input/policy migration for one-action resource and machine completion.
- `AutoFishingMod`: DTMAPI-native config/input/state-policy migration for auto fishing.
- `FishBreedingAssistantMod`: DTMAPI-native fish roe tooltip lookup/provider migration. The public source tree includes a placeholder generated lookup file without private fish data.
- `AnimalHusbandryProgressMod`: DTMAPI-native animal viewer progress policy migration.
- `DebugConsoleMod`: official-local Y-key debug console mod backed by experimental in-save debug APIs.
- `SecondMotorMod`: experimental official-local second flying motor example.
- `OilMod`: official-local content mod for crude oil item metadata and coal mining drops.
- `MineMod`: official-local content mod for the experimental Mine machine and Machine API behavior.
- `MoreEquipmentSlotsMod`: experimental extra equipment slots mod backed by DTMAPI-managed storage and native save transactions.
- `MoreSavesMod`: experimental official-save-screen slot expansion mod backed by `ISaveSlotsApi`.
- `ZoomMod`: official-local large-view camera zoom mod backed by lease-based `ICameraViewApi`.
- `ChestLocatorEnhancerMod`: official-local shared chest lookup enhancement backed by `IChestLocatorEnhancerApi`.
- `StrongPlantingGunMod`: official-local farming-gun upgrade backed by `IStrongPlantingGunApi`.
- `AutoHarvestMod`: default-off crop harvesting sample backed by `ICropHarvestingApi`; it uses DTMAPI only and does not reference raw Doloc Town assemblies.
- `HookProbeMod`: logs hook evidence for GameLaunched, SaveLoaded, UpdateTicked, UI, input, and exit smoke tests.

HookProbe should use the local game's third save by default when a save is needed.
