# Scout Expert Development Kit (EDK)

Version 2.0

Reference implementation for the entire platform
"When in doubt, follow the Ebook Expert architecture—not necessarily the Ebook Expert code."

Reference Implementation:
Ebook Expert

The Ebook Expert is the reference implementation for all future Scout Experts.

Every new Expert should follow the architecture, coding standards, and development workflow established by the Ebook Expert.

Every completed Expert should improve this SDK.

---

# Core Philosophy

Scout does not understand files.

Scout understands collections.

Readers extract facts.

Blocks build understanding.

Reports preserve understanding.

Consultants interpret understanding.

Investigations coordinate understanding.

Experts communicate understanding to Scout.

Scout combines discoveries from every Expert into one conversation.

---

# Scout Architecture

Every Expert follows the same pipeline.

Reader
    ↓
Extracts facts from one file.

Block
    ↓
Builds understanding across many files.

Report
    ↓
Preserves everything the Block learned.

Consultant
    ↓
Interprets one Report.

Investigation
    ↓
Coordinates Consultants within one subject.

Expert
    ↓
Coordinates every Investigation.

Scout
    ↓
Combines discoveries from every Expert.

When in doubt, follow this pipeline.

---

# Plugin Independence

Every Expert must be completely self-contained.

A completed Expert should be able to be:

• Zipped
• Copied
• Shared
• Registered with Scout
• Loaded
• Removed

without requiring modifications to any other Expert.

If another Expert must be modified...

the architecture is wrong.

---

# Reader Responsibilities

Readers answer one question.

"What facts can I extract?"

Readers

• Read files.
• Parse data.
• Extract metadata.
• Return models.

Readers do NOT

• Make decisions.
• Interpret results.
• Build reports.
• Create findings.
• Talk to Scout.

Readers extract facts.

Nothing more.

---

# Block Responsibilities

Blocks answer one question.

"What do these facts mean?"

Blocks

• Measure.
• Compare.
• Detect relationships.
• Detect problems.
• Preserve evidence.
• Build Reports.

Blocks do NOT

• Talk to Scout.
• Produce ExpertFindings.
• Recommend actions.

Blocks build understanding.

---

# How Blocks Think

Every Block follows the same process.

Observe

↓

Measure

↓

Collect Evidence

↓

Understand

↓

Build Report

This pattern should appear throughout every Expert.

---

A Block should completely exploit
the information it already possesses.

A Block should not require new
infrastructure merely to produce
one additional observation.

---

# Reports

Reports preserve understanding.

Reports are private to the Expert.

Reports contain

• Statistics
• Evidence
• Relationships
• Intermediate reasoning

Reports never

• Talk to Scout
• Explain discoveries
• Recommend actions

Consultants interpret Reports.

---

# Evidence

Whenever practical...

Blocks should preserve the evidence supporting every discovery.

Examples

Duplicate ISBN

Duplicate Title

Missing Series Book

Broken Table of Contents

Corrupt Metadata

Missing Cover

Evidence should answer one question.

"What caused this conclusion?"

Consultants use Evidence to explain discoveries.

---

# Consultants

Consultants answer one question.

"What is important?"

Consultants

• Read Reports.
• Interpret evidence.
• Produce ExpertFindings.

Consultants never

• Read files.
• Parse metadata.
• Perform technical work.

---

# Investigations

Investigations answer one question.

"What should I investigate?"

Investigations

• Coordinate Consultants.
• Combine related findings.
• Report results to the Expert.

Investigations never

• Read files.
• Parse data.
• Perform reusable technical work.

---

# Experts

Experts answer one question.

"What do I know about this collection?"

Experts

• Coordinate Investigations.
• Combine findings.
• Report discoveries to Scout.

Experts never

• Read files directly.
• Parse metadata.
• Perform reusable technical work.

---

# Scout

Scout answers one question.

"What should happen next?"

Scout

• Combines discoveries.
• Explains observations.
• Answers questions.
• Makes recommendations.

Scout never

• Reads files directly.

---

# Single Responsibility Principle

Every class answers exactly one question.

Analyzer

"What kind of collection is this?"

Reader

"What facts can I extract?"

Block

"What do these facts mean?"

Report

"What did I learn?"

Consultant

"What is important?"

Investigation

"What should I investigate?"

Expert

"What do I know?"

Scout

"What should happen next?"

---

# Development Workflow

Every feature is built as a package.

Every package contains

Goal

Files

NEW

MODIFIED

REPLACED

Implementation Steps

Expected Build Result

Commit Message

Every package ends with

Green Build

↓

Commit

Never continue development while the build is broken.


# Observations answer:

What do I know?

# Capabilities answer:

What can I do?

---

# Coding Standards

Prefer understandable code over clever code.

Optimize for readability first.

Every method performs one task.

Every class answers one question.

Use complete replacement files for unstable classes.

Use targeted modifications for stable production classes.

Never duplicate responsibilities.

---

# Documentation

Whenever the architecture changes...

Update the documentation immediately.

Do not allow the code and documentation to drift apart.

The documentation should always describe the current architecture.

---

# Reference Expert

The Ebook Expert is the first complete reference implementation.

Future Experts should copy its architecture.

Not necessarily its code.

Every completed Expert should improve the SDK.

The next Expert should be easier to build than the last.

---

# Definition of Complete

An Expert is not complete until it includes

✓ Analyzer

✓ Expert

✓ Investigations

✓ Consultants

✓ Readers

✓ Blocks

✓ Reports

✓ Evidence

✓ Expert Findings

✓ Observation Signals

✓ Recommendations

✓ Documentation

✓ Plugin Independence

Nothing ships until every box is checked.

---

# Final Principle

Readers know facts.

Blocks understand facts.

Reports remember understanding.

Consultants recognize significance.

Experts combine knowledge.

Scout explains everything.