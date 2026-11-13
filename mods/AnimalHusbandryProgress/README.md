# AnimalHusbandryProgress

Managed Advanced CodeMod for the animal bell viewer, requiring Runtime 0.7.0.

Shows every valid output from the merged native husbandry table, including custom animal outputs. Rows sort by progress, then name; the view scrolls after three rows and reuses its existing objects when selecting another animal. Names follow the current game language. Unknown item names fall back to their IDs; invalid thresholds are omitted.

The only settings are the color preset and custom color. Older enable/cache/logging settings are ignored. It reads animal progress without changing animal state, AI, host membership or save data.

The product owns its four native patches, selected-animal binding and scroll view. Public APIs provide item names, configuration, reflection and owner resource cleanup. Closing the panel removes only its own objects; failed owner cleanup remains registered for retry.
