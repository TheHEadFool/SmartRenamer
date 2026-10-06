# Repair Investigation — Current Implementation (2026-10-04)

> This section is authoritative and supersedes the older prototype notes below.

## Current Repair Loop

Repair is no longer only a recommendation model. The implemented flow is:

`RepairOpportunity`
→ local evaluation/evidence
→ external recovery when needed
→ confidence decision
→ safe automatic repair or user choice
→ protected working-copy repair
→ re-observation
→ verification
→ handoff to organization

## Current external recovery

- **ISBN:** Open Library research and candidate evaluation are implemented.
- **Publisher:** background metadata research is implemented.
- **Book Blurb/Synopsis:** background metadata research is implemented using Open Library description/first-sentence evidence and Google Books synopsis evidence.
- **Cover:** investigation/consulting exists, but a complete automatic external cover download/apply vertical slice is not yet declared complete.

The external metadata resource gathers candidates only. `E_RepairDecisionEngine` remains the safety gate. With Automatic Repairs ON, a candidate at or above the automatic confidence threshold can be applied; weaker evidence remains a user/review decision.

## Important distinction

Research success does not equal repair success. Scout may legitimately report that research found evidence and then re-observe before deciding whether that evidence is safe enough to apply.

## Safety boundary

The original EPUB is not the repair target. Repair works against a protected working representation and re-reads the result before organization.

---

﻿# Repair Investigation

## Purpose

Determine which improvements can be safely performed.

---

## Questions Answered

- Can metadata be repaired?
- Can navigation be rebuilt?
- Can covers be restored?
- Can damaged information be recovered?
- Is repair safe?

---

## Produces

RepairReport

---

## Future Consultants

- Metadata Repair Consultant
- Navigation Repair Consultant
- Cover Repair Consultant

---

## Future Blocks

- Metadata Repair Engine
- Navigation Builder
- Cover Recovery Block

---

## Possible Expert Findings

- Metadata can be repaired.
- Navigation can be rebuilt.
- Repair not recommended.

---

## Possible Recommendations

- Repair metadata.
- Repair navigation.
- Restore missing covers.

---

## Ownership

Repair owns safe improvement opportunities.

---

## Out of Scope

Repair never:

- Performs repairs automatically.
- Downloads information.
- Renames files.
- Talks to Scout.

---

## Notes

Repair recommends safe repairs.

It never performs repairs automatically.

state as of September 12, 2026.

Updated
Documents the real ISBN repair implementation.
Documents the working-copy safety model.
Documents E_RepairHandoff.
Clarifies that verification uses re-observation.