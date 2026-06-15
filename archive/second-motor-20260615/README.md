# Second Motor Archive - 2026-06-15

Status: archived / not active.

This archive preserves the DTMAPI-authored `SecondMotorMod` source snapshot after the experimental custom motor route was merged for history but retired as an active local project.

## Why Archived

User manual QA on 2026-06-15 reported the rebuilt second motor as effectively unusable:

- abnormal light texture behavior;
- abnormal farm-room textures;
- severe cross-map texture pollution.

The retained native-owner research still says Doloc Town's native motor path is singleton-oriented. The current DTMAPI `IMotorVehicleApi` code may remain in `DTMAPI.GameBridge.DolocTown` as Experimental research, but this sample mod is no longer built, installed, published, or used as smoke acceptance.

## Contents

- `testmods/SecondMotorMod/`: archived DTMAPI-authored mod source and official-local content metadata.

This archive intentionally does not include official extracted Doloc Town textures, sprites, animation clips, animator controllers, prefabs, asset bundles, or raw Unity asset dumps.

## Local Package Archive

The local installed packages were moved outside the repo to:

`E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS`

Moved folders:

- `DTMAPI_SecondMotor`
- `DLK_SecondMotor`

## Restore Warning

Do not restore this as an active package without a new native-owner/API rebuild and fresh manual QA. Historical smoke evidence is retained only as research history, not as current completion proof.
