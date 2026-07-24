# Architecture

This document consolidates the architecture-related documents into a single reference while preserving source sections.


---

# Source: Architecture.md

﻿# SmartRenamer Architecture

The Promise

SmartRenamer promises to work alongside you—with you, not instead of you.

It asks thoughtful questions before offering solutions.

It explains its reasoning before making recommendations.

It protects your work before making changes.

It celebrates your progress as your projects come together.

With every project, SmartRenamer strives to leave you more capable and confident than when you began.

It is your partner, not your producer.

## Purpose

SmartRenamer is not simply a batch file renamer.

It is a collaborative workspace for organizing digital information through reusable workflows.

The application's architecture is designed around one central principle:

> The user describes the project.
> The Guide discovers the requirements.
> The Workflow accomplishes the work.

Technology should remain behind the scenes whenever possible.

---

# Architectural Philosophy

The architecture should support these goals:

- Projects before files.
- Workflows before commands.
- Building Blocks before programming.
- Guide before wizard.
- Conversation before configuration.
- Teach outcomes, not technologies.
- Support local and cloud assets equally.
- Every decision should be explainable.
- Every operation should be previewable.
- Every operation should be undoable.

---

# High-Level Architecture

The Guide never performs work directly.

The Guide understands the project and builds workflows.

The Workflow Engine performs the work.

---

# Core Components

## Guide Engine

Responsible for understanding the user's project.

The Guide:

- asks meaningful questions
- explains recommendations
- identifies risks
- recommends workflows
- recommends Building Blocks
- teaches through projects
- remembers only with permission

The Guide never asks users to make technical decisions.

---

## Workflow Engine

Responsible for executing workflows.

The engine should know nothing about:

- AI
- User Interface
- Cloud Providers
- Tutorials

It only executes Building Blocks.

---

## Building Blocks

Every operation is represented as a reusable Building Block.

Examples:

- Find & Replace
- Move Files
- Read EXIF
- OCR
- AI Describe
- Regex
- Sequence
- Rename

Future plugins become additional Building Blocks.

---

## Workflow

A Workflow is an ordered collection of Building Blocks.

The Workflow should be:

- editable
- explainable
- previewable
- shareable
- reusable

---

## Project

A Project represents what the user is trying to accomplish.

A Project contains:

- goals
- workflow
- connections
- history
- journal
- preferences
- saved conversations

Projects should be portable.

---

## Connections

SmartRenamer should never assume files are local.

Connections may include:

- Local folders
- Google Drive
- Microsoft OneDrive
- Dropbox
- Box
- Evernote
- SharePoint
- NAS
- FTP
- Amazon S3
- Future providers

The rest of the application should not care where assets live.

---

## Digital Assets

The application should eventually think in terms of Digital Assets instead of Files.

Digital Assets include:

- photos
- videos
- PDFs
- Office documents
- Google Docs
- Notes
- Emails
- Audio
- Archives
- Future asset types

---

## Pipeline Packs

Pipeline Packs represent reusable solutions.

They contain:

- workflow
- thumbnail
- documentation
- examples
- metadata
- version
- required plugins

Pipeline Packs should be shareable.

---

## Community

Community knowledge is part of the product.

Users should be able to:

- publish Pipeline Packs
- discover Pipeline Packs
- rate Pipeline Packs
- improve Pipeline Packs

---

# Design Principles

Every screen should answer one question.

Workspace

"What project are we putting together today?"

Guide

"What should we discover next?"

Workflow

"What's the plan?"

Building Blocks

"What tools will help?"

Preview

"What will happen?"

History

"What have we accomplished?"

---

# User Experience Principles

The Guide behaves like a trusted friend who is exceptionally good at organizing digital information.

The Guide:

- celebrates progress
- explains recommendations
- admits uncertainty
- protects user data
- asks permission before remembering
- teaches naturally
- never talks down to the user

The goal is not simply to organize files.

The goal is to help people become confident organizers of their digital world.

---

# Future Architecture

The architecture should support future capabilities without redesign.

Examples include:

- AI-generated workflows
- Plugin SDK
- Community Marketplace
- Cloud providers
- Team collaboration
- Automation
- Scheduling
- Version history
- Workflow templates
- Cross-platform support

Every new feature should integrate into the existing architecture rather than creating parallel systems.

The Guide remembers conclusions, not conversations.

The Guide should never ask the user for information it can reasonably discover on its own.

Guide is a companion, not a command interface.

Guide observes continuously, but interrupts rarely.

The Guide protects the user's attention as carefully as it protects their files

The Guide is knowledgeable, humble, and transparent. It explains what it knows, how it knows it, and when it is uncertain.

If WPF already solves the problem elegantly, don't invent a new layer.

The Guide notices first, recommends second, and acts only with permission

## UI Components

User controls should have a single responsibility: presenting information and collecting input.

Business logic belongs in ViewModels and Services.

Whenever practical, a UserControl should contain no code beyond its constructor and `InitializeComponent()`.

## Information Flows Upward

Higher-level components define what information is needed.

Lower-level services provide that information.

The Guide should describe the information it wishes to understand, rather than depending on implementation details.

## Development Philosophy

Develop one complete capability at a time.

Each capability should:
- compile successfully,
- run successfully,
- demonstrate one observable behavior,
- be ready to build upon.

## ProjectContext is the Single Source of Truth

All project analysis is accumulated in ProjectContext.

Capabilities do not talk directly to the Guide.

Each capability contributes observations, recommendations, warnings, or metadata to ProjectContext.

Friend (GuideInvestigator) translates ProjectContext into natural conversation.

This allows new capabilities to be added without changing Friend's conversational logic.

### ProjectAnalyzer

ProjectAnalyzer interprets the raw data collected by FolderScanner.

It determines:

- ProjectType
- Observations
- RecommendedCapabilities

It never communicates directly with the Guide or UI.

Its sole responsibility is enriching ProjectContext.

## CapabilityFactory

CapabilityFactory converts recommendation names into executable capability objects.

ProjectAnalyzer recommends capabilities by name.

CapabilityFactory creates the corresponding WorkflowStep.

This keeps ProjectAnalyzer independent of implementation classes and allows new capabilities to be added with minimal changes.

Capabilities decide. Execution executes.

Capabilities may analyze files, detect patterns, and propose actions.

Execution services never make decisions. They only perform an approved ScoutPlan.

User
               │
               ▼
            Guide
               │
               ▼
      ProjectAnalyzer
               │
               ▼
        ProjectContext
               │
               ▼
        Scout Planner
               │
               ▼
          ScoutPlan
               │
               ▼
     Execution Services
               │
               ▼
          File System

          ### Relative Destinations

Capabilities do not move files.

Capabilities recommend a relative destination path for each asset.

Examples:

Photos\2026\July
Music\Artist\Album
Documents\Taxes\2025
Hotel California

Execution combines:

Destination Root
+
Relative Destination
+
Destination Name

to produce the final file location.

## The Scout Pipeline

Observe
    ↓
Reason
    ↓
Act

### Observe

The Observe phase discovers facts.

Examples:

- Folder scanning
- EXIF metadata
- Music metadata
- Related assets
- OCR
- AI classification

Observe never makes decisions.

Its only responsibility is to enrich ProjectContext with facts.

---

### Reason

The Planner consumes discovered facts and decides what Scout
intends to do.

Examples:

- Destination folders
- Rename suggestions
- Folder structure
- Recommendations

Reason never copies, moves, renames or deletes files.

---

### Act

The Executor performs the approved plan.

Examples:

- Copy
- Move
- Rename
- Delete (future)

The Executor never makes decisions.

### Ownership

Every stage owns one responsibility.

Observe owns facts.

Reason owns decisions.

Act owns execution.

A stage must never compensate for mistakes made by another stage.

When a value is incorrect, fix the earliest stage responsible for producing it.
## 7/21/2026 
UI
│
▼
MainWindowViewModel
│
▼
ScoutService
│
├── updates ScoutOperation
│
└── listens for progress from
      │
      ▼
ScoutExecutor

---

# Source: Pipeline.md

﻿# Scout Pipeline

Scout processes every collection through the same pipeline.

Folder

↓

Folder Investigation

↓

FolderSummary

↓

ProjectContext

↓

Analyzers

↓

Observations

↓

Planner

↓

Capabilities

↓

Guide

↓

Workspace

Only the capabilities modify the file system.

Every stage before that is read-only.

---

# Source: CapabilityArchitecture.md

﻿# Capability Architecture

## Purpose

Scout is not a collection of tools.

Scout is a growing collection of **capabilities**.

Every time Scout learns something new, it gains a capability—not a new mode, screen, or wizard.

The user should never need to know which capability is running.

They simply describe their goal.

Scout determines which capabilities can help.

---

# What is a Capability?

A capability is a self-contained skill Scout has learned.

Examples include:

- Rename Files
- Download Book Covers
- Add Book Metadata
- Organize Downloads
- Detect Duplicate Files
- Prepare Plex Libraries
- Organize Music
- Analyze Photos

Every capability follows exactly the same lifecycle.

```
Investigate
      ↓
Discover
      ↓
Recommend
      ↓
Preview
      ↓
Conversation
      ↓
Execute
      ↓
Undo
```

This consistency allows Scout to grow without becoming more complicated.

---

# Capability Responsibilities

Every capability is responsible for answering six questions.

## 1. What did I discover?

Examples

- 18 filenames contain underscores.
- 42 books are missing covers.
- 7 duplicate movies were detected.

---

## 2. Why does it matter?

Examples

- Improves readability.
- Makes ebook libraries easier to browse.
- Prevents duplicate storage.

Scout should always explain the value of the recommendation.

---

## 3. What can I do?

A capability describes the improvement it can perform.

Examples

- Replace underscores with spaces.
- Download missing covers.
- Remove duplicate spaces.
- Rename files to Plex format.

---

## 4. Can the work be previewed?

Every capability must generate a preview before execution.

Users should always understand what will change before Scout changes anything.

---

## 5. Can the work be executed?

Execution performs the approved work.

Capabilities never execute automatically without user approval.

---

## 6. Can the work be undone?

Every capability must support Undo whenever technically possible.

Undo is not an optional feature.

Undo is part of every capability's lifecycle.

---

# Scout's Role

Scout does not rename files.

Scout does not download covers.

Scout does not organize folders.

Scout coordinates capabilities.

Scout is responsible for:

- Understanding the user's goal
- Choosing appropriate capabilities
- Presenting recommendations
- Explaining recommendations
- Managing the conversation
- Building workflows
- Confirming execution
- Coordinating Undo

Capabilities perform the work.

Scout manages the experience.

---

# Recommendations

Every capability returns one or more Recommendations.

A Recommendation describes an opportunity for improvement.

Examples

- Rename 18 files
- Download 42 covers
- Add 27 summaries
- Remove 5 duplicates

Scout presents recommendations in priority order.

Recommendations appear both:

- in conversation
- in the Recommendations panel

The conversation and user interface always describe the same recommendations.

---

# Preview

Scout never performs hidden work.

Every capability generates a preview.

The preview answers:

What will change?

Nothing should execute until the user approves.

---

# Execution

Execution is performed only after approval.

Approval may come from:

- conversation
- clicking a recommendation
- selecting multiple recommendations

All execution uses the same workflow.

---

# Undo

Undo is considered part of execution—not an afterthought.

Every successful execution creates a transaction.

Transactions contain every change necessary to restore the previous state.

Scout should always be able to say:

"You can undo this operation."

---

# Learning

Scout learns capabilities.

Capabilities do not change Scout.

As new capabilities are added, Scout automatically gains new skills because every capability follows the same architecture.

No capability should require special conversation logic or a unique user interface.

Every new capability should feel like Scout learned something new.

---

# Architecture Goal

The goal of this architecture is to allow Scout to grow indefinitely.

Whether Scout learns to:

- rename files
- organize photos
- enrich ebook metadata
- prepare Plex libraries
- identify duplicate documents
- analyze archives

the user experience remains identical.

The user simply talks to Scout.

Scout selects the appropriate capabilities.

Everything else follows the same lifecycle.

# Design Rule

When adding a new feature, never ask:

"Where should this code go?"

Instead ask:

"What new capability is Scout learning?"

If the answer cannot be expressed as a capability that follows this architecture, the design should be reconsidered before implementation.

# Capability Philosophy

Capabilities discover facts.

They never:

- rename files
- move files
- create folders
- ask the user questions

Capabilities only enrich ProjectContext.

Examples:

PhotoMetadataCapability

Produces:

CaptureDate
GPS
Camera

RelatedAssetCapability

Produces:

RelatedGroup

MusicMetadataCapability

Produces:

Artist
Album
Track
Genre

The Planner decides how those facts are used.

---

# Source: CapabilityPhilosophy.md

﻿# Capability Philosophy

Capabilities perform work.

Capabilities never decide what should happen.

Capabilities simply execute.

Decision making belongs to:

Analyzers

Planner

Guide

Execution belongs to:

Capabilities

---

# Source: ObservationPhilosophy.md

﻿# Observation Philosophy

Scout is fundamentally an observation engine.

Everything begins with an observation.

An Observation must satisfy three rules.

1.

It is true.

2.

It is useful.

3.

It can be explained.

Recommendations are derived from observations.

Capabilities are selected because of recommendations.

The user should always be able to trace an action back to an observation.

---

# Source: FileContextPhilosophy.md

﻿# FileContext Philosophy

Scout does not classify files into subclasses.

Instead, Scout continually enriches its understanding of a file.

A FileContext always represents one file.

New capabilities add knowledge rather than replacing the FileContext.

Examples of future knowledge include:

EXIF

Music Metadata

Git Repository Information

PDF Information

Office Metadata

Archive Contents

This allows Scout to understand file types that do not yet exist.

Scout learns.

It does not specialize.

---

# Source: AnalyzerContract.md

﻿# Analyzer Contract

Every Analyzer follows the same responsibilities.

## Input

ProjectContext

## Reads

FolderSummary

FileContexts

Project Goal

## Produces

ProjectProfile

Observations

Recommendations

## Never

Rename files

Move files

Delete files

Modify the file system

An Analyzer observes.

It does not act.

---

# Source: Architectural Invariants.md

﻿# Architectural Invariants

These principles define Scout's architecture.

Changing one requires an explicit architectural decision.

---

The Observation Box is universal.

---

FolderSummary contains facts, never opinions.

---

Analyzers observe.

They do not modify files.

---

Capabilities perform work.

They do not decide work.

---

The Planner makes decisions.

---

The Guide explains decisions.

---

FileContext represents a file.

It is enriched rather than replaced.

---

Architecture emerges through completed vertical slices.

Avoid speculative abstractions.

---

Every user-visible action must be explainable through observations.