# DTMAPI Alternate Flying Motor

Experimental official-local DTMAPI package for the motor vehicle API.

- Registers `dtmapi.second_motor` through `IMotorVehicleApi`.
- Adds `dtmapi_second_motor_key` / `飞行摩托钥匙（异色）` through official `Content/**/item_tbitem.json`.
- Registers a DTMAPI config/status page so the title settings UI can show bridge, vehicle, and key-delivery state.
- On save load, first verifies that `Local.DTMAPI_SecondMotor` is enabled through the official local-mod path and that the key item is indexed from that source. It then checks backpack count and unclaimed item-mail attachments, and uses `IMailDeliveryApi -> DolocAPI.SendItemAsEmail` to deliver one key when needed.
- Mail delivery is source-aware and attachment-preflighted; disabled or missing SecondMotor content must skip delivery rather than send an empty mail.
- The key is still an official `ItemFunctionMotorKey`; DTMAPI only intercepts that specific item id before native `ItemMotorKey.OnUse`.
- Installer intentionally does not copy the official example `sprite_vehicle_motor*` replacement assets, because those global asset keys also change the original Doloc Town motor.
- GameBridge applies an instance-scoped tint to the cloned motor so the original motor keeps its native appearance until a scoped sprite adapter is implemented.
- Runtime riding remains experimental because it temporarily routes `AgentControllerState.motorController` to a DTMAPI-managed clone and restores the original controller on dismount. Failed summons, title returns, save loads, and save switches run DTMAPI-owned clone cleanup so residue from this API does not cross save boundaries.
- GameBridge captures `DolocAPI.EnterRoom` targets while riding the second motor and synchronizes the clone/body/AgentPosition proxy through map transitions without moving the original motor archive state.
- `GAME-SMOKE/20260604-111533` verifies enabled-source registration, key/mail duplicate handling, dual visibility, instance appearance isolation, riding, edge transition to `city_郊区-上游丘陵1`, original-motor restore, and clean exit. `GAME-SMOKE/20260604-111901` verifies disabled-source behavior with `Skip=2 Registration=0 Mail=0`.
