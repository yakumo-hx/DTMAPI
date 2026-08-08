# DTMAPI Update Index

This root file is a stable router. Year files route to monthly ledgers, and each Update row is written only once in its matching month.

## Annual Ledgers

- [2026 update ledger](INDEX-2026.md)

## Write Policy

- Add every non-trivial Update row to its matching monthly ledger through the year router above.
- Do not duplicate the row in this root router.
- New records use the normalized metadata in [Document Governance](../workflows/document-governance.md).
- Historical status strings remain unchanged; normalization is prospective.

Run `tools/scripts/check-doc-governance.ps1` before committing documentation-governance changes.
