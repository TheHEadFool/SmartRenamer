# Decision 0001

Originals are never modified.

Reason

Trust is more important than convenience.

Date

2026-07-19

-----------------------------------

Decision 0002

Observe → Reason → Act

Reason

Keeps intelligence separate from planning and execution.

-----------------------------------

Decision 0003

Related assets are grouped only when multiple files share the same base filename.

Reason

Avoids creating thousands of unnecessary one-file folders.

-----------------------------------

Decision 0004

The planner decides.

Capabilities never decide.

Decision 0005

When debugging,
follow the data before changing the design.

Reason

Existing code deserves the benefit of the doubt.
The first incorrect value is almost always easier
to find than the best new architecture.

## Decision 0005

Follow the data before changing the design.

Reason

Evidence is more reliable than assumptions.
The earliest incorrect value almost always identifies the real bug.

## Decision 0006

Complete source files are preferred over code fragments during development.

Reason

Whole-file reviews preserve context and reduce accidental syntax and formatting
errors.

Decision 0008

Organized copies are created beside the source folder.

Reason

Keeps source and organized copy separate while avoiding recursive processing.

Future UI should clearly communicate the output location.

-----------------------------------

Decision 0009

Repaired EPUBs use a protected working copy and are handed to Organization
through a semantic Repair Handoff.

Reason

Repair and Organization have different responsibilities. Organization should
not need to know why an EPUB was repaired, and the original source must remain
protected.

-----------------------------------

Decision 0010

Initial ebook organization will be incremental and safe: copy the repaired
working representation to the organized destination, verify it, then release
the temporary working copy.

Reason

The durable result is the organization operation and its status, not a growing
collection of repaired EPUBs left in Temp. This also supports interruption and
resume for large collections.
