# DTMAPI.Abstractions

Public contracts referenced by DTMAPI mod authors. Status/stability and
adoption disposition are separate per-surface axes, not assembly-wide labels.
Check the status in the public matrix and the disposition
(`Open`, `Frozen`, `Diagnostic`, `Internal`, or `Disabled`) on the
contract. New ordinary mods should use only contracts whose two axes permit
adoption in
[`docs/api/public-api-matrix.md`](../../docs/api/public-api-matrix.md).

Frozen/Obsolete declarations remain for existing binary consumers and are not
new-product APIs. Diagnostic contracts are support/QA surfaces, and Proposed or
blocked CustomEntity runtime verbs are not implementation promises. Public
contracts must avoid raw Doloc Town, Unity, Harmony, or BepInEx types.
