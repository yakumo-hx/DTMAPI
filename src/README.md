# Source Layout

| Projects | Responsibility |
| --- | --- |
| Abstractions, Authoring.Contracts | Author and tooling contracts; public visibility alone does not imply stable adoption |
| Core | Managed discovery, loading, ownership, events, configuration and diagnostics |
| BepInExBootstrap | Single player bootstrap; BepInExStubs supports source builds |
| GameBridge.DolocTown | Proven shared native adapters |
| GameBridge.DolocTown.Compatibility | Retained compatibility surfaces and old consumer behavior |
| GameBridge.DolocTown.QA | Optional acceptance instrumentation |
| ModConfigMenu | Configuration UI and managed menu integration |
| AuthorSdk, Tooling.Metadata | Author CLI, target selection, validation, build and packaging |
| InstallDoctor, PlayerDoctor, MultiPlatformInstaller | Installation diagnostics and supported installer routes |

ContentPatcher, ConsoleCommands and TemplateMod are README placeholders, not current build projects. Actual build membership belongs to [the solution](../DTMAPI.sln). Independent implementations live under [products/first-party](../products/first-party). Product-owned frozen implementations live in their respective `compatibility/` directories; the optional compatibility host links them without changing their assembly identity.

Game-loaded code stays netstandard2.0; native research is reference-only and never copied into this rebuild. See the [SDK](../author-sdk/README.md) for managed Mod identity, packaging and author contracts. Game-owned objects retain their native lifecycle and save semantics.
