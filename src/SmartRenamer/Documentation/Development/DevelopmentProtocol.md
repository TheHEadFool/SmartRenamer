This is the process I'd like us to follow for project.

Phase 0 — Inspect

Before implementation, inspect the current project.

Use the actual implementation as the source of truth.

Do not assume file names, methods, or properties from memory.

---

Instruction Standard

Every coding instruction should include:

File

Method

Approximate line number

Find

Action

Expected Build

This eliminates ambiguity and prevents searching for insertion points.

---

Implementation Size

Prefer:

One method

One responsibility

One build

over

Multiple files

Large rewrites

Unverified changes

---

Architecture Discipline

Do not interrupt implementation for architectural improvements unless they block progress.

Record architectural ideas in documentation or the Parking Lot.

Return immediately to completing the current Expert.

---

Expert Completion Priority

The objective is to complete one exemplary Expert.

Future Experts should be built by following that completed implementation 
rather than inventing new patterns.

Phase 1 — Read

Before changing anything, 
I inspect the existing code to understand how it currently works.

Phase 2 — Design

We agree on a single, demonstrable feature.

Phase 3 — Implement

I give precise instructions:

exact file
approximate line number
exact code to replace
explanation of why
Phase 4 — Build

You compile and run.

Phase 5 — Verify

We demonstrate the feature working.

Phase 6 — Commit

Only after it's working do we move on.

No speculative refactoring.
No "while we're here..."
No changing unrelated code.