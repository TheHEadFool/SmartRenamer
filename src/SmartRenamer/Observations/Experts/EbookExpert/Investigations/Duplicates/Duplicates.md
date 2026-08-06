# Duplicate Investigation

## Purpose

Determine relationships between multiple copies of the same work.

## Version 1 Scope

Version 1 detects:

- Duplicate filenames
- Duplicate ISBNs
- Multiple editions

Future versions may add:

- File hash comparison
- Fuzzy title matching
- Duplicate content detection

---

## Questions Answered

- Are exact duplicates present?
- Are duplicate ISBNs present?
- Are multiple editions present?
- Are duplicate filenames present?
- Are books duplicated in multiple folders?

---

## Produces

DuplicateReport

---

## Future Consultants

- Duplicate Consultant
- Edition Consultant
- ISBN Comparison Consultant

---

## Future Blocks

- Hash Calculator

- ISBN Matcher

- Filename Comparison

- Title Comparison

- Edition Detector

---

## Possible Expert Findings

- Duplicate books detected.
- Multiple editions detected.
- Duplicate ISBN detected.
- No duplicates found.

---

## Possible Recommendations

- Merge duplicates.
- Review multiple editions.
- Archive duplicate copies.

---

## Ownership

Duplicate Investigation owns relationships between copies of the same work.

---

## Out of Scope

Duplicate Investigation never:

- Deletes books.
- Chooses which copy to keep.
- Repairs metadata.
- Talks to Scout.

---

## Notes

Duplicate detection identifies relationships.

It never deletes files.