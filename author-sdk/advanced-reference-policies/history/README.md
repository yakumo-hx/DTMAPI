# Historical Advanced reference policies

Files in this directory retain exact policies for packages generated under an
earlier game-build baseline. They are not current Advanced authoring
admissions and must not be selected by `dtmapi.author.json`.

`runtime-registry.json` is embedded only in Runtime and Doctor. It lets those
components validate an already-issued receipt against the exact historical
policy bytes and UniqueID binding. It is deliberately excluded from Author SDK,
so it cannot reopen old policy selection or generate a new package.

Current admissions are the unique `requiredUniqueId` bindings in the parent
`registry.json`.
