# Author SDK examples

These projects demonstrate ordinary Author SDK usage. They are examples, not products, QA fixtures,
or publishable Workshop packages. Their Catalog role is `Example` and release eligibility is
`NeverPublish`.

The source-only [OwnedCleanup](OwnedCleanup/README.md) and [LegacyConfig](LegacyConfig/README.md)
examples use `dtmapi-author new` to generate a complete project and identity. Copy their entry
source into the generated `src/ModEntry.cs`, then restore and pack. They demonstrate the existing
platform services and do not ship a product or an independently maintained package manifest.
