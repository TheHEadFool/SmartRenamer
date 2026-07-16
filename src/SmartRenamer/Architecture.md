# SmartRenamer Architecture

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