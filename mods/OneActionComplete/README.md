# DTMAPI One Action Complete

Optional first-party Advanced CodeMod, version `1.1.0`, UniqueID `Yuuka.DTMAPI.OneActionComplete`. It uses the existing DTMAPI configuration menu and owns its native gameplay changes.

The settings page has six options, all off by default: trees, ore, garbage, weeds, fuel machines and feeders. Open it through DTMAPI's configuration menu. Save applies the draft; Cancel restores it. The retired master switch, F11 shortcut and status text are ignored, including in old config files.

Completion starts only after the current native action deals damage or consumes material and advances the selected machine. Each extra resource hit pays normal tool energy and uses native damage and drops. Wrong tools, insufficient tool level and rejected hits get no extra action. Insufficient energy stops at the paid damage. Filling stops at native capacity, missing/unsuitable material, no progress or 99 extra items. Fuel uses the native component's actual amount and IsFull, including its reserve; the clamped 100% display is not a capacity test. A native exception stops the loop; paid material is never blindly refunded after a possible mutation.

The SDK generates a NativeContract V2 package against the installed game. Owner `Yuuka.DTMAPI.OneActionComplete.Native` installs a Prefix and Postfix on each of `ToolCollider.HandleTools` and `AgentStateInteract.OnExit`; per-call state prevents one target's accepted hit from carrying into another. Owner deactivation detaches callbacks and removes its four patches. No product save data is written.

Requires DTMAPI and ModConfigMenu `0.7.0` or newer. `IActionCompletionApi` and the old V1 reference policy remain frozen compatibility artifacts; this product consumes neither. The compatibility guard recognizes both the old and current native owners. Build and package with `build/build-mod-project.ps1 -ProjectPath mods/OneActionComplete/Yuuka.DTMAPI.OneActionComplete.csproj -AssetRoot <artwork-directory>` from the public source root. Package metadata and the assembly version come from `info.json`. The existing Workshop item is `3742763540`; packaging and local acceptance do not publish it.
