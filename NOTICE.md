# DTMAPI Notice

DTMAPI is a clean-room rebuild of a Doloc Town modding API. The source in this repository is intended to be distributed under the MIT License in `LICENSE`.

## Third-Party And Reference Material

- The exact upstream BepInEx 5.4.23.5 Windows x64 ZIP is retained at `build/bootstrap/BepInEx_win_x64_5.4.23.5.zip` for installer and synthetic Harmony tests. Its SHA-256 is `82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4`, matching the upstream release asset.
- The ZIP contains BepInEx, BepInEx.Harmony (including the `0Harmony20` compatibility assembly and HarmonyXInterop), HarmonyX 2.9.0, MonoMod 22.1.29.1, Mono.Cecil 0.10.4, and Unity Doorstop 4.5.0. Their licenses and every archive member's hash are recorded in [the fixture manifest](licenses/bepinex-fixture.json). DTMAPI's MIT license does not replace these licenses.
- Unity Doorstop 4.5.0 is licensed under LGPL v2.1. Its [license](licenses/UnityDoorstop.LICENSE.txt) and [complete corresponding source archive](licenses/UnityDoorstop-v4.5.0-source.tar.gz), including the upstream build scripts, accompany the binary fixture. The source archive is checked against all 55 blobs in the upstream Git tree; it has no submodules. It is supplied unmodified.
- Other Harmony and runtime patching dependencies are resolved through project/package references where applicable.
- Official Doloc Town binaries, `Assembly-CSharp.dll`, reverse `input/`, full decompiled game source, private debug evidence, third-party mod binaries, and local-only mod source copies are not part of the public source distribution.
- Stardew Valley SMAPI material is architecture/reference material only and must not be copied into DTMAPI implementation.

## Public Package Boundary

Public source includes DTMAPI-authored source, tests, scripts, public documentation, `LICENSE`, this notice, and the explicitly inventoried third-party fixture and license/source files above. Official documentation snapshots, game binaries, private evidence, local research, build outputs, unlicensed media, and package caches are excluded.
