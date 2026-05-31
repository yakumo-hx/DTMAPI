# AnimalHusbandryProgressMod DTMAPI Migration

This migration understands the old mod as: when the animal bell detail viewer renders, append one special-produce progress bar below the normal official progress bars.

Current 0.1.10 boundary:

- DTMAPI-native `DtmMod` entry.
- Config menu for display toggle, label, cache interval, and low-frequency logs.
- Registers display policy with `IAnimalViewerApi`.
- The GameBridge animal viewer hook and final in-game tooltip/progress-bar path are still pending verification.
