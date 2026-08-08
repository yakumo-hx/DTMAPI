# Author SDK third-party payload notice

The published Author SDK includes the reference assemblies from Microsoft's `NETStandard.Library` package version `2.0.3` solely as offline compiler references. The release payload must carry that package's `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`, and both files must be covered by `compatibility.json` hashes.

The source tree does not track the `.nupkg` or reference DLL binaries. Release staging must use the pinned package version, preserve its notices, and never source `DTMAPI.Abstractions.dll` from a player game installation.
