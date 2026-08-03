# Build a New Scout Expert
Scout Expert Development Kit (EDK)
Expert Construction Template

# Mission

I am extending the Scout platform by creating a new Expert plugin.

This Expert should be completely self-contained.

Scout should be able to load or remove this Expert without requiring changes to any other Expert.

The completed Expert should serve as a reusable plugin.

"When in doubt, follow the Ebook Expert architecture."

# Scout Architecture

Every Scout Expert follows the same pipeline.

Reader
    ↓
Extracts facts from one file.

Block
    ↓
Builds understanding from many files.

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
Coordinates Investigations and reports discoveries.

Scout
    ↓
Combines discoveries from every Expert and communicates with the user.

When in doubt, follow this pipeline.

# Plugin Independence Test

Before writing any code, answer this question.

Can this Expert be:

Zipped
Emailed to another developer
Registered with Scout
Loaded without modifying another Expert

YES / NO

If NO, identify exactly what leaks outside the Expert.

No Expert is considered complete until it passes this test.

# Domain

Replace this section.

Expert Name

Examples

Comic Book Expert
Trading Card Expert
CAD Expert
Recipe Expert
Tax Document Expert

# Purpose

Describe what this Expert understands.

This Expert understands...

# Collection Types

What files belong to this Expert?

Extensions

Examples

.cbz
.cbr
.pdf

# Analyzer

How does Scout recognize this collection?

What evidence tells Scout this folder belongs to this Expert?

Examples

File extensions
Folder structure
Embedded metadata
Naming conventions
Companion files

# Scout Personality

When Scout discovers this collection...

What personality should Scout adopt?

Examples

Librarian
Historian
Teacher
Archivist
Detective
Coach
Assistant

Describe how Scout should communicate with the user.

# Investigations

For each Investigation, answer one question.

## Metadata Investigation

What should Scout discover?

## Contents Investigation

What should Scout discover?

## Organization Investigation

What should Scout discover?

## Duplicate Investigation

What should Scout discover?

## Quality Investigation

What should Scout discover?

## Repair Investigation

What should Scout discover?

## Enrichment Investigation

What should Scout discover?

## Specialists

For each Investigation list every Specialist.

Example

Metadata Investigation

    Metadata Specialist

        Uses

            Metadata Reader Block

    ISBN Specialist

        Uses

            ISBN Lookup Block

Repeat as needed.

# How a Block Thinks

A Block does more than read data.

Every Block follows the same pattern.

Observe
    ↓
Measure
    ↓
Collect Evidence
    ↓
Understand
    ↓
Build Report

Readers extract facts.

Blocks build understanding.

Blocks never communicate directly with Scout.

Blocks never create ExpertFindings.

# Blocks

Describe every reusable Block.

For every Block specify:

Block Name

Purpose

Inputs

Outputs

Dependencies

Can another Expert reuse this Block?

YES / NO

# Reports

Reports are private to the Expert.

Reports preserve understanding.

Reports contain:

Statistics

Evidence

Relationships

Intermediate reasoning

Reports are never shown to the user.

Consultants interpret Reports.

Scout never reads Reports directly.

Scout never sees Reports.

Describe every Report produced by this Expert.

Example

MetadataReport

Contains

    Title Count

    Author Count

    ISBN Count

    Cover Count

    Missing Metadata

Repeat for every Report.

# Evidence

Blocks should preserve supporting evidence whenever practical.

Examples

Duplicate ISBN

Duplicate Title

Missing Series Book

Broken Table of Contents

Missing Cover

Corrupt Metadata

Evidence should answer:

"What caused the Block to reach this conclusion?"

Consultants use Evidence to explain discoveries.

# Observation Signals

What reusable ObservationSignals can this Expert produce?

Examples

MissingISBN
MissingCover
DuplicateBook
SeriesDetected
BrokenArchive
PublisherDetected
LanguageDetected
MissingMetadata

These Signals become Scout's shared language between Experts.

# Expert Findings

What discoveries should this Expert report to Scout?

Examples

Missing Covers
Missing Metadata
Duplicate Files
Broken Books
Incomplete Series

Remember:

Experts discover.

Scout explains.

# Recommendations

What actions should Scout recommend?

Examples

Repair Metadata
Download Covers
Merge Duplicate Books
Rename Series
Rebuild Library

# Knowledge Sources

Where can this Expert learn information?

Examples

Embedded Metadata
Filename
Folder Structure
Internet
ISBN Database
Open Library
User Input
AI Reasoning

# User Experience

How should Scout present this Expert?

Observation Panel

Sidebar

Workflow

Conversation Examples

Give three examples of how Scout should naturally explain discoveries.

Example

I found several books that appear to belong to the same series.

I noticed many books are missing ISBNs. This may reduce duplicate detection.

Would you like me to repair this metadata?

# Coding Standards

Every class answers exactly one question.

Every method should perform one step.

Every Block should preserve evidence whenever possible.

Prefer understandable code over clever code.

Optimize for readability before optimization.

## Analyzer

What kind of collection is this?

## Expert

What do I know about this collection?

## Investigation

What should I investigate?

## Specialist

How do I investigate this?

## Block

How do I perform one reusable technical task?

## Report

What did I learn while investigating?

## ExpertFinding

What should Scout know?

## ObservationMapper

How should Scout explain it?

## Scout

What should happen next?

# Expert Checklist

Before this Expert is complete:

□ Analyzer

□ Expert

□ Metadata Investigation

□ Contents Investigation

□ Organization Investigation

□ Duplicate Investigation

□ Quality Investigation

□ Repair Investigation

□ Enrichment Investigation

□ Specialists

□ Blocks

□ Reports

□ Observation Signals

□ Expert Findings

□ Recommendations

□ Documentation

□ Plugin Independence Test

Nothing ships until every box is checked.

# Development Workflow

Build Experts in small packages.

Each package contains:

Goal

Files

New

Modified

Replaced

Implementation Steps

Expected Build Result

Commit Message

Never continue until the build is green.

Commit after every successful package.

# Lessons Learned

After completing this Expert, record improvements that should benefit every future Expert.

Examples

New reusable Blocks
Better naming conventions
Improved Investigation patterns
Shared Observation Signals
Coding standards
Architectural improvements

Every completed Expert should make the Scout platform stronger.

# Final Question

Ask ChatGPT:

Using this Expert Construction Template, generate a complete Scout Expert 
plugin that follows the Scout architecture, coding standards, and plugin 
independence rules. The Expert should be completely self-contained and require 
no modifications to existing Experts.