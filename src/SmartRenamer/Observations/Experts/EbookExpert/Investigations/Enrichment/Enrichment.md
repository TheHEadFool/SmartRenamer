# Enrichment Investigation — Current Implementation (2026-10-04)

> This section is authoritative and supersedes older statements that describe external metadata recovery as future work.

Enrichment identifies information that could improve an ebook collection. Current examples include Series, Description/Book Blurb, Cover, Publisher, and Language.

The implemented external metadata recovery path is currently owned by the Ebook Expert repair/recovery pipeline rather than by this Investigation itself. This preserves the boundary: Enrichment can identify an opportunity, while Repair/Recovery determines whether and how a missing value can be safely restored.

### Current external metadata sources

- Open Library Search API — existing ISBN and metadata evidence source.
- Google Books Volumes API — public book metadata source used for publisher and synopsis evidence when required.

Goodreads is not currently an automated Scout provider. No Goodreads scraping is implied by this document.

### Current boundary

Enrichment does not directly modify EPUBs. It reports opportunities. The Repair pipeline owns candidate evaluation, authorization, protected working-copy repair, and re-observation.

---

﻿# Enrichment Investigation

## Purpose

Determine how the ebook library can be improved.

---

## Questions Answered

- Is better metadata available?
- Are improved covers available?
- Is series information available?
- Is publisher information available?
- Can additional information improve the collection?

---

## Produces

EnrichmentReport

---

## Future Consultants

- Metadata Lookup Consultant
- Cover Lookup Consultant
- Series Lookup Consultant

---

## Future Blocks

- Open Library Client
- ISBN Lookup
- Cover Download Block
- Author Collection Completeness
- Series Completeness
- Missing Works
- Recommended Next Books
- Similar Authors

---

## Possible Expert Findings

- Additional metadata available.
- Improved cover available.
- Series information found.
- Publisher information found.

---

## Possible Recommendations

- Download improved metadata.
- Download better cover.
- Complete series information.

---

## Ownership

Enrichment owns knowledge that exists outside the current library.

---

## Out of Scope

Enrichment never:

- Downloads information automatically.
- Repairs books.
- Renames files.
- Talks to Scout.

---

## Notes

Enrichment discovers opportunities.

It never downloads information without Scout's permission.