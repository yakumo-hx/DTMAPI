# Author SDK third-party payload notice

The desktop SDK includes NuGet.Protocol, NuGet.Packaging, NuGet.Configuration, NuGet.Common, NuGet.Frameworks and NuGet.Versioning 7.9.0, copyright Microsoft Corporation, under Apache-2.0. The exact upstream NuGet.Client commit is `977537e19c6be57fead1411e6cf05f936bf1baf4`; its license is `licenses/NuGet-7.9.0-LICENSE.txt`. These desktop-only components supply NuGet version/framework parsing and package content/signature validation. Restore is performed by the bundled standard .NET SDK. They are not game Runtime dependencies.

NuGet's desktop dependency Newtonsoft.Json 13.0.3 is included under the MIT license in `licenses/Newtonsoft.Json-13.0.3-LICENSE.txt`. Microsoft System.Security.Cryptography.Pkcs 8.0.1 and ProtectedData 8.0.0 are covered by the SDK's distributed .NET license and third-party notices. These support the desktop tooling, not ordinary Mod payloads. Restored author dependencies must include their own explicitly selected license material; tool notices do not license arbitrary author libraries.

The SDK's .NET 8 metadata tools include Mono.Cecil 0.11.6, licensed under MIT/X11 by Jb Evain and contributors. The SDK carries the upstream license in `licenses/Mono.Cecil-0.11.6-LICENSE.txt`. It is used only by author tooling and Doctor; the five game-loaded Runtime assemblies do not depend on Mono.Cecil. Locally generated game reference surfaces remain on the author's machine and are excluded from Mod and SDK distributions.

This first-public-release SDK candidate includes the reference assemblies from Microsoft's `NETStandard.Library` package version `2.0.3` solely as offline compiler references. The release payload must carry that package's `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`, and both files must be covered by `compatibility.json` hashes. Earlier SDK builds were internal and unpublished.

The source tree does not track the `.nupkg` or reference DLL binaries. Release staging must use the pinned package version, preserve its notices, and never source `DTMAPI.Abstractions.dll` from a player game installation.

The complete SDK also carries the unmodified .NET SDK 8.0.421 toolchain under toolchain/dotnet, including MSBuild, Roslyn and their supplied licenses/notices. The selected base/analyzer development nupkgs in offline-packages preserve their upstream license metadata. The SDK203 analyzer targets Roslyn 4.11.0; the MSBuild adapter uses the host MSBuild task API. These are author build tools, not Mod runtime dependencies.
