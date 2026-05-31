# Test Mods

Test mods are small DTMAPI mods used to prove runtime behavior inside Doloc Town.

- `HelloDtmMod`: minimal load and logging proof.
- `BrokenManifestMod`: manifest and dependency error handling.
- `ConfigMenuExample`: config menu option coverage.
- `ActionSpeedMod`: first DTMAPI-native migration slice for ActionSpeed config/input/update/restore behavior.
- `OneActionCompleteMod`: DTMAPI-native config/input/policy migration for one-action resource and machine completion.
- `AutoFishingMod`: DTMAPI-native config/input/state-policy migration for auto fishing.
- `FishBreedingAssistantMod`: DTMAPI-native fish roe tooltip lookup/provider migration.
- `AnimalHusbandryProgressMod`: DTMAPI-native animal viewer progress policy migration.
- `HookProbeMod`: logs hook evidence for GameLaunched, SaveLoaded, UpdateTicked, UI, input, and exit smoke tests.

HookProbe should use the local game's third save by default when a save is needed.
