# DTMAPI Alternate Flying Motor

Experimental official-local DTMAPI package for the custom motor vehicle API.

- Registers `dtmapi.second_motor` through `IMotorVehicleApi.RegisterCustomMotor`.
- Adds `dtmapi_second_motor_key` / `飞行摩托钥匙（异色）` through official `Content/**/item_tbitem.json`.
- Adds the key to the official phone booth store through `mod_tbmodstoreextension.json`; this package no longer mails the key.
- Multiple copies of `dtmapi_second_motor_key` route to the same DTMAPI-managed motor instance.
- Registers a DTMAPI config/status page so the title settings UI can show bridge, vehicle, key, and scoped appearance state.
- The key is still an official `ItemFunctionMotorKey`; DTMAPI intercepts that specific item id before native `ItemMotorKey.OnUse`, so the custom key can work even when the native motor is locked.
- The custom motor keeps native flying-motor movement and collision behavior for this first rebuild. Future definitions should split movement, collider, attack, and harvest behavior behind explicit GameBridge-owned policies.
- Installer/runtime package code must not install the official example `sprite_vehicle_motor*` assets as global content replacements. When the local official vehicle example is available, the installer copies the texture into DTMAPI-named private assets under `Content/DTMAPI/assets/second-motor/` and GameBridge applies it only to the DTMAPI clone. If that scoped sprite path cannot be proven at runtime, GameBridge falls back to the instance-scoped tint and records the failure in hook status.
- Runtime riding remains experimental because it temporarily routes `AgentControllerState.motorController` to a DTMAPI-managed clone and restores the original controller on dismount. Failed summons, title returns, save loads, and save switches run DTMAPI-owned clone cleanup so residue from this API does not cross save boundaries.
- The new acceptance path uses save slot 8 for an already-unlocked native-motor comparison fixture and save slot 9 for a locked-native-motor independence fixture.
