# Native slot 0 repair adapter

This maintainer-only component is called by `../invoke-player-save-repair.ps1`. It is not a general save editor and exposes one repair, `ruinedcity-mail-v1`. Its contract is owned by [player support](../../../docs/workflows/player-support.md) and implementation [20260908-0003](../../../docs/updates/2026/20260908-0003-player-support-and-save-repair.md).

## Format and provenance

`ArchiveFormat.cs` independently implements a strict UTF-8 / JSON span reader, the standard AES-CBC / PKCS7 envelope, and one insertion. It does not invoke game assemblies or deserialize `$type` into executable types. JSON is read as data; duplicate decoded property names (including case aliases), malformed tokens and excessive depth are rejected. Ambiguous dictionary keys also stop this bounded mode rather than guessing native interpretation. Game-loaded dependencies and a new SDK are unnecessary; Windows PowerShell 5.1 can compile it with `Add-Type`.

The reviewed local reference is build `24966367_public_958EAF`. `GameManager.prefab` declares the encrypted envelope, filename and format material. The production adapter pins that resource's SHA-256. `LocalSave`, `AesEncryptor`, `BaseArchiveData`, `DateInfo`, `Email`, `EmailAttachMission`, `EmailManager`, `MissionManager`, `MissionChainManager` and `DialogueManager` establish serialization field facts only. No decompiled implementation or asset text was copied. The two prior repairs establish save versions `1.00.02` and `1.00.06`; other versions stop for a focused compatibility assessment.

The local ignored profile has these fields:

| Field | Meaning |
| --- | --- |
| `FormatVersion` / `FormatId` | `1` / `doloc-aes-cbc-utf8-v1` |
| `ReferenceBuild` / `ResourceSha256` | Reviewed build and exact resource fingerprint |
| `ArchiveFileName` / `Prefix` | `doloc-save-{0}.data` / `DOLOC-TOWN:` |
| `SupportedSaveVersions` | Exactly the two reviewed versions |
| `SyntheticFixture` | `false` for the reviewed resource; tests use `true` and require a `FIXTURE-` case |
| `KeyBase64` / `IVBase64` | Base64 of the resource fields' UTF-8 bytes; local ignored material only |

The reusable local profile is referenced in the Update's engineering evidence. Do not put it in a recovery ZIP or copy it into a tracked file. A missing or changed resource/profile blocks this format adapter; it does not trigger whole-game unpacking for every case. Review a new matching baseline before extending the allowlist. Profile SHA, resource SHA, format/build/save version and case/source/slot identities are copied to non-secret receipts; format material is never copied to them.

## Deliberate limits

Inputs are a native-named `.data` or a support ZIP with one unambiguous `/SAVE/doloc-save-0.data`. Files and their ancestor directories may not traverse reparse points. Archive bytes are limited to 32 MiB; support packages to 1 GiB and 20,000 entries; JSON to 128 levels and one million nodes. ZIP data is streamed into bounded memory and never extracted by path.

Only one unread native `ruinedcity_continue` mail is inserted at the start of `farmData.emailManager.emails`, carrying `ruinedcity_main` with `autoAccept=true`, `isAccept=false` and the existing date span. Completed `wetland_main@1` is a mission ID checked in `finishMissions` alongside `_0`–`_6`; `finishDecorators` contains separate decorator IDs. `chainInfos` remains a definition projection. The adapter does not edit version, native index, task state, dialogue state, clock or world data.

Verify recomputes the allowed insertion from the unchanged source, decrypts the final `.data` and final candidate ZIP independently, and requires byte equality after removing precisely that insertion. Updating a checksum cannot make another plaintext change acceptable. The existing slot 0 builder then packages the verified encrypted bytes. Runtime readability and first-read native task acceptance remain separate game checks.

## Offline tests

Run `tools/scripts/test-player-save-repair.ps1` with Windows PowerShell 5.1. Its tiny synthetic JSON has no playable world and uses a synthetic key; never use it as a game fixture. Test outputs are retained under `temp/player-save-repair-test-*` and identify themselves as synthetic. No live SAVE, game installation, Steam state or player source is read by these tests.
