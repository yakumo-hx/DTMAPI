# DTMAPI Fish Roe Info Display

This is the separately admitted fourth first-party Advanced CodeMod. It is optional and is not part of the five-assembly DTMAPI Runtime.

When enabled in the official Mod list, this product appends the current-language name of the fish that will hatch from the roe: `original title (fish name)`. Ordinary items, descriptions and details remain unchanged. Unknown fish or failed name lookups retain the original title; repeated queries do not append twice.

There are no settings or configuration page. Old configuration files are ignored and left intact. Disable the product in the official Mod list to restore native titles.

The product owns one `Item.get_title` Harmony Postfix and exact-owner cleanup. It reads the roe's native `fishName`, which is passed to the native fry by `ToFry`, and uses the existing Experimental `IItemDisplayNameApi` for names. The shared adapter owns language and environment cache invalidation. The product has no data table, cache, save subscription or persistent gameplay state, and does not consume frozen `IItemTooltipApi`.

Candidate version: `1.1.0`, requiring DTMAPI `0.7.0`; identity: `Yuuka.DTMAPI.FishBreedingAssistant`; existing Workshop item: `3742763706`. NativeContractVersion 2 uses SDK-generated local native references and package binding. This candidate was accepted with this worktree's Runtime language-cache fix; the published Runtime does not automatically include that change. Package preparation and validation do not imply a Workshop upload.
