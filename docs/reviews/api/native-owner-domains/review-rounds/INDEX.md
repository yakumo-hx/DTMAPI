# Native Owner Domain Review Rounds

Status: active, docs-only
Created: 2026-06-13
Parent library: [Native Owner Domain Library](../INDEX.md)

This directory records the three-agent review loop requested after the initial native-owner domain library was created. The loop is a review artifact only: it does not promote public APIs, does not add GameBridge code, and does not prove runtime behavior.

## Round Files

| Round | Purpose | Report |
| --- | --- | --- |
| Round 1 | Parallel challenge review of the 12 domain reports, including overclaims, missing native owners, and initial confidence scoring. | [Round 1 Challenges](ROUND-1-challenges.md) |
| Round 2 | Parallel response and supplemental owner report answering the Round 1 questions without repeating the initial scan. | [Round 2 Supplemental Report](ROUND-2-supplemental-report.md) |
| Round 3 | Final parallel review and confidence scoring of the supplemented report conclusions. | [Round 3 Final Confidence](ROUND-3-final-confidence.md) |

## How To Use

1. Start with the domain report in the parent directory.
2. Read [Round 3 Final Confidence](ROUND-3-final-confidence.md) for the latest downgrade/blocked status.
3. Use [Round 2 Supplemental Report](ROUND-2-supplemental-report.md) when a report needs more exact native ownership detail.
4. Use [Round 1 Challenges](ROUND-1-challenges.md) to understand why an apparently supported feature was downgraded.

## Scope Boundary

- These records are review notes derived from read-only reverse inspection and parallel sub-agent review.
- Evidence is limited to paths, symbols, maps, and report text. No decompiled source is copied here.
- Runtime behavior still needs a separate GameBridge implementation record, third-save game evidence, and public API matrix update before any stability claim.
