# Public fixture licenses

`bepinex-fixture.json` records the exact upstream BepInEx 5.4.23.5 Windows x64 archive, all 22 members and their six license owners. The BepInEx release asset digest agrees with the retained ZIP. Versions come from the release tag's project files and BepInEx.Harmony submodule, including the `HarmonyX2Interop` project that produces `0Harmony20.dll`.

Upstream inputs:

- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5), MIT.
- [BepInEx.Harmony at d4cdcb4](https://github.com/BepInEx/BepInEx.Harmony/tree/d4cdcb4cdeac14a0b77012165f5f5a9f5032a9fa), MIT.
- [HarmonyX 2.9.0](https://github.com/BepInEx/HarmonyX/tree/v2.9.0), MIT.
- [MonoMod 22.1.29.1](https://github.com/MonoMod/MonoMod/tree/v22.01.29.01), MIT.
- [Mono.Cecil 0.10.4](https://github.com/jbevain/cecil/tree/0.10.4), MIT.
- [Unity Doorstop 4.5.0](https://github.com/NeighTools/UnityDoorstop/tree/v4.5.0), LGPL v2.1.

The Doorstop source tarball includes every file of the upstream tree, including `LICENSE`, `README.md`, `build.bat`, `build.ps1`, `build.sh` and platform source. Each unpacked file was checked using Git's blob identity. There are no submodules to fetch. Follow the archive's README and build scripts to build or modify Doorstop; these are upstream source inputs, not a DTMAPI contributor prerequisite. DTMAPI does not modify this library or replace its license.

Changing the binary fixture, a member, license file or source archive requires updating and revalidating the same inventory. A license file beside a mismatched ZIP does not satisfy the export gate.
