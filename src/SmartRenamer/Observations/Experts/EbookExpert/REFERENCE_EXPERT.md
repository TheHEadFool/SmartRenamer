# REFERENCE_EXPERT.md

# The Scout Reference Expert

## Purpose

The Ebook Expert is the reference implementation for every Scout Expert.

It exists to demonstrate the architecture, coding standards, responsibilities,
and design philosophy used throughout the Scout platform.

Future Experts should resemble this Expert more than they resemble any other
code in the project.

The goal is consistency.

Not cleverness.

If another developer—or ChatGPT—can understand the Ebook Expert, they should
be able to build any future Expert.

---

# Scout Philosophy

Scout is not a file organizer.

Scout is an investigator.

Scout learns.

Scout explains.

Scout recommends.

Scout asks permission.

Every Expert exists to increase Scout's understanding of one type of collection.

Experts do not organize files.

Experts understand files.

---

# Plugin Philosophy

Every Expert is a completely self-contained plugin.

Scout should be able to:

• Load an Expert
• Remove an Expert
• Replace an Expert
• Update an Expert

without modifying another Expert.

If changes are required outside the Expert,
the architecture is leaking.

Fix the architecture.

Do not work around it.

---

# Folder Layout

Each Expert contains only the knowledge required for its own domain.

Example

Expert

    Blocks

    Data

        Reports

        Models

        Resources

    Investigations

    Specialists

    Documentation

Everything inside belongs only to this Expert.

Nothing outside should know how the Expert works.

---

# Responsibilities

## Analyzer

Question answered:

"What kind of collection is this?"

Responsibilities

• Recognize a collection
• Measure confidence
• Never inspect deep content
• Never organize files

---

## Expert

Question answered:

"What do I know about this collection?"

Responsibilities

• Coordinate Investigations
• Combine findings
• Return ExpertFindings

The Expert does NOT

• Read files
• Parse metadata
• Talk to the UI
• Rename files

---

## Investigation

Question answered:

"What should I investigate?"

Responsibilities

• Coordinate Specialists
• Collect Reports
• Produce ExpertFindings

An Investigation owns a single topic.

Examples

Metadata

Contents

Organization

Duplicates

Quality

Repair

Enrichment

---

## Specialist

Question answered:

"How do I investigate this?"

Responsibilities

• Perform one investigation
• Use reusable Blocks
• Produce findings

A Specialist should have one reason to change.

---

## Block

Question answered:

"How do I perform one reusable technical task?"

Responsibilities

• Perform one technical operation
• Be reusable by multiple Specialists
• Contain no business logic

Examples

Metadata Reader

Hash Calculator

Image Reader

ISBN Lookup

Filename Parser

ZIP Reader

---

## Report

Question answered:

"What did I learn?"

Reports are private.

Scout never sees Reports.

Reports exist only inside the Expert.

Reports may change without affecting Scout.

---

## ExpertFinding

Question answered:

"What should Scout know?"

ExpertFindings are the public language spoken by every Expert.

Every Expert returns ExpertFindings.

Nothing more.

Nothing less.

---

## ObservationMapper

Question answered:

"How should Scout explain this?"

Experts never generate conversations.

Experts never generate UI.

ObservationMapper translates ExpertFindings into observations
that Scout can present naturally.

Experts discover.

Scout explains.

---

## Scout

Question answered:

"What should happen next?"

Scout owns the conversation.

Scout owns recommendations.

Scout owns permission.

Scout owns workflow.

Scout never owns domain knowledge.

---

# Information Flow

The complete pipeline is:

Scout

↓

ObservationEngine

↓

Expert

↓

Investigation

↓

Specialist

↓

Block

↓

Report

↓

ExpertFinding

↓

ObservationMapper

↓

ProjectObservation

↓

Scout

Every class has one responsibility.

---

# Design Rules

Every class should answer exactly one question.

If a class answers two unrelated questions,
it should probably become two classes.

Favor many small classes over a few large classes.

Prefer composition over inheritance.

Prefer coordination over duplication.

Keep dependencies pointing downward.

Never create circular dependencies.

---

# Coding Style

Methods should be short.

Names should describe intent.

Comments should explain why.

Code should explain how.

Avoid clever code.

Prefer readable code.

Future developers should understand the code
without reading this document.

---

# Investigation Pattern

Every Investigation should follow the same pattern.

Specialists

↓

Private Report

↓

Reason

↓

ExpertFinding

↓

Return Findings

Repeat this pattern consistently.

---

# Self-Containment Test

Before an Expert is considered complete ask:

Can this Expert be:

✓ Zipped

✓ Shared

✓ Installed

✓ Removed

✓ Updated

without modifying another Expert?

If not...

The architecture is leaking.

---

# Future Experts

Every new Expert should begin with:

ExpertConstructionTemplate.md

Do not write code first.

Design the Expert first.

Once the template is complete,
ChatGPT should be able to generate
the entire Expert.

---

# Lessons Learned

As the Scout platform evolves,
this document should evolve.

Every completed Expert should improve
the next Expert.

The Ebook Expert is not simply an Expert.

It is the teacher of every future Expert.

---

# Final Principle

The architecture exists to make Experts simple.

Experts exist to make Scout smarter.

Scout exists to make people more confident
working with their files.

# An empty implementation is acceptable. A misleading implementation is not.

Returning an empty list is honest.

Pretending to investigate chapters before we've built that capability is not.

## Progressive Construction

Every Investigation goes through four stages:

Stage 1
--------
Purpose
Responsibilities
Boundaries

↓

Stage 2
--------
Investigation
(compiles and returns findings)

↓

Stage 3
--------
Consultants
(real intelligence)

↓

Stage 4
--------
Blocks
(reusable technical implementation)

# define what a Block is:

A Block encapsulates one area of domain knowledge.

A Block knows how to gather information, interpret it, validate it, and present its conclusions.

A Consultant asks questions.

A Block provides answers.