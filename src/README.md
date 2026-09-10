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

ContentPatcher, ConsoleCommands and TemplateMod are README placeholders, not current build projects. Actual build membership belongs to [the solution](../DTMAPI.sln). Product implementations are located by the [Catalog](../tools/release/dtmapi-product-catalog.json).

[PROJECT](../PROJECT.md) owns native responsibility and Mod identity. Game-loaded code stays netstandard2.0; native research is reference-only and never copied into this rebuild.
