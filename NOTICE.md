# DTMAPI Notice

DTMAPI is a clean-room rebuild of a Doloc Town modding API. The source in this repository is intended to be distributed under the MIT License in `LICENSE`.

## Third-Party And Reference Material

- BepInEx is used as the bootstrap/runtime injection layer. BepInEx is not vendored into this source tree.
- Harmony and other runtime patching dependencies are resolved through project/package references where applicable.
- Doloc Town official Workshop documentation and reverse-engineering notes under `references/` are reference material only.
- Official Doloc Town binaries, `Assembly-CSharp.dll`, reverse `input/`, full decompiled game source, private debug evidence, third-party mod binaries, and local-only mod source copies are not part of the public source distribution.
- Stardew Valley SMAPI material is architecture/reference material only and must not be copied into DTMAPI implementation.

## Public Package Boundary

Public source packages should include DTMAPI-authored source, tests, scripts, public docs, selected reference documentation, `LICENSE`, and this notice. They should exclude build outputs, NuGet/package artifacts, official game binaries, private local paths, and local-only research folders.
