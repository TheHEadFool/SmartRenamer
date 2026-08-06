# Every coding package should look like this:

PACKAGE EE-00X

Goal

Files

NEW
...

MODIFIED
...

REPLACED
...

Step 1

Step 2

Step 3

Expected Build

Commit Message

No ambiguity.

No guessing.

No "where does this go?"

## Instruction Format

Every implementation step should be presented using this template.

File

Method

Approximate Line

Find

Action

Expected Build

Stop

The developer should never have to search for where code belongs.

Every step should identify the precise location before giving code.

---

## Build Frequency

Every meaningful change should compile.

Preferred rhythm:

One change

↓

Build

↓

Fix

↓

Repeat

Avoid batching multiple unrelated edits into a single build.

---

## Current Project Is The Source Of Truth

Always work from the latest project.

Never rely on memory when the current source code can answer the question.

Inspect existing code before proposing changes.

Extend existing implementations instead of creating parallel ones.

---

## Finish Vertical Slices

Complete one Investigation before beginning the next.

Preferred order:

Investigation

↓

Report

↓

Consultant

↓

Commit

Only after one Investigation is complete should another Investigation begin.

---

## Small Consistency Rule

Immediately perform changes that:

- take less than one minute
- improve consistency
- have essentially no architectural impact

Examples:

- namespace corrections
- missing using statements
- typo fixes
- simple naming consistency

Larger architectural changes belong in the Parking Lot.

---

## Compiler Driven Development

Treat compiler errors as the next task list.

Fix the current error before introducing additional changes.

Avoid speculative fixes.