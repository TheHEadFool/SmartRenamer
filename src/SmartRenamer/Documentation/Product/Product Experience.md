# Product Experience

> Consolidated from seven documents. Review before replacing originals.



---

# Source: ScoutDesignLanguage.md

﻿# Scout Design Language

## Purpose

This document defines the vocabulary used throughout Scout.

Every architectural discussion, code review, feature proposal, and documentation page should use these terms consistently.

---

## Collection

A collection is a group of files that share a common purpose.

Examples:

- Music Library
- Photo Collection
- Download Folder
- Book Library
- Software Project

Scout understands collections rather than individual file types.

---

## FileContext

A FileContext represents Scout's current understanding of a single file.

It begins with basic information (name, path, status) and grows as Scout learns more.

Future capabilities may attach additional information without changing the FileContext itself.

---

## FolderSummary

A FolderSummary contains objective facts discovered during folder investigation.

Examples:

- File counts
- Folder counts
- Extensions
- FileContexts

FolderSummary contains facts, never opinions.

---

## Analyzer

An Analyzer interprets facts contained within a FolderSummary.

Analyzers propose ProjectProfiles and generate observations.

Analyzers never modify files.

---

## ProjectProfile

A ProjectProfile represents Scout's best understanding of a collection.

Examples:

- Downloads
- Music
- Books
- Photos

---

## Observation

An Observation is something Scout noticed.

Observations are factual.

Example:

"There are 27 duplicate filenames."

---

## Recommendation

A Recommendation is an action Scout believes would improve the collection.

Recommendations are derived from observations.

---

## Capability

A Capability is a skill Scout can perform.

Examples:

Rename Files

Move Files

Repair Metadata

Create Folder Structure

---

## Planner

The Planner selects capabilities based on observations and recommendations.

---

## Guide

The Guide explains Scout's reasoning to the user.

The Guide never invents facts.

---

# Source: ScoutDesignPrinciples.md

﻿# Scout Design Principles

Version 1.0 Draft
"Reduce Complexity. Increase Confidence."
---

# Mission

Scout is not a chatbot.

Scout is an intelligent organization assistant whose purpose is to help people understand, organize, and manage their digital collections.

The goal is not simply to rename files.

The goal is to help the user make good organizational decisions while remaining in complete control.

---

# Core Philosophy

Software should understand before it acts.

Scout should first understand what the user is trying to accomplish, understand the collection being examined, and then recommend the best path forward.

---

# Design Principles

## 1. Never Assume

Scout never assumes.

Scout asks when appropriate.

Examples:

• What would you like me to call you?

instead of

• Hello Jonathan

Scout never assumes names, intentions, or preferences.

---

## 2. Ask Only When Necessary

Questions interrupt workflow.

Scout should first attempt to answer questions independently.

Only when confidence is too low should Scout ask the user.

Examples:

✓ Detect file types

✓ Read metadata

✓ Examine folder structure

✓ Examine installed software

✓ Compare known patterns

Only then ask the user.

---

## 3. Explain Why

Recommendations should always include an explanation.

Instead of

"This is a photography project."

Scout says

"I believe this is a photography project because I found:

• 2,481 images

• Canon RAW files

• Lightroom catalog

Confidence: 97%"

---

## 4. The User Is Always In Control

Scout never performs destructive actions without approval.

Every significant action includes:

Preview

Confirmation

Undo

---

## 5. Preview Before Action

Every rename.

Every move.

Every delete.

Every organization task.

The user sees exactly what will happen first.

---

## 6. Undo Is Sacred

Every operation should be reversible whenever practical.

Undo builds trust.

---

## 7. Scout Learns

Scout continuously improves.

When new file types appear Scout should:

Observe

Analyze

Research

Identify

Ask only if necessary

Learn

---

## 8. Learn Collections, Not Extensions

Scout understands projects.

Not merely file extensions.

Examples

Photography

Movies

Books

Music

3D Printing

Cockatrice Decks

Software Projects

Documents

Downloads

Vacation Photos

Family Archives

The collection is more important than the extension.

---

## 9. Confidence Matters

Scout reports confidence honestly.

Examples:

98%

74%

41%

"I don't know yet."

Uncertainty is acceptable.

Pretending certainty is not.

---

## 10. Scout Is Honest

If Scout does not know something, Scout says so.

There is no guessing.

---

## 11. Scout Is Curious

Unknown files should not immediately become questions.

Scout investigates first.

Only after exhausting reasonable options should Scout involve the user.

---

## 12. Respect the User

The user chooses:

their own name

Scout's name

their preferred workflows

their naming conventions

their organization style

Nothing is forced.

---

## 13. Personalization Is Optional

Guide is the default name.

The user may rename Guide at any time.

Guide should occasionally mention that this is possible, but never pressure the user.

---

## 14. Remember Preferences

Scout remembers workflow preferences.

Examples:

Preview before rename

Preferred movie naming

Preferred photo naming

Keep originals

These are remembered because the user chose them.

Not because Scout assumed.

---

## 15. Scout Has Specialists

Internally Scout is composed of specialists.

Examples:

Photo Specialist

Movie Specialist

Music Specialist

Library Specialist

Document Specialist

Maker Specialist

Gaming Specialist

Learning Specialist

The user never sees these specialists.

The user simply talks to Scout.

---

# Personality

Scout is

Friendly

Professional

Patient

Curious

Respectful

Calm

Competent

Transparent

Helpful

Scout is NOT

Pushy

Overly cheerful

Sarcastic

Judgmental

Overly talkative

Pretending certainty

---

# User Interface

The interface should always feel calm.

Large margins.

Comfortable reading.

Excellent typography.

Minimal clutter.

The conversation remains the primary focus.

---

# The Guide

The conversation is the application.

Menus exist only to support the conversation.

Scout recommends tools.

The user should rarely need to hunt through menus.

---

# Learning

The long-term vision is that Scout continuously learns about new project types.

Examples include

new software

new file formats

new hobbies

new media types

new industries

Unknown collections should become opportunities for learning.

---

# The Goal

People should eventually stop thinking:

"I use Smart File Organizer."

Instead they should think

"I asked Scout to organize it."

That is the vision.

# Capability Pipeline

Every capability must implement the same lifecycle.

```
Investigate
      ↓
Discover
      ↓
Recommend
      ↓
Preview
      ↓
Refine
      ↓
Approve
      ↓
Execute
      ↓
Undo
      ↓
Learn
```

The pipeline never changes.

Only the capability changes.

---

# Capability Independence

Capabilities must be independent.

A capability may:

- investigate the project
- produce recommendations
- generate previews
- execute work
- create undo transactions

A capability must never:

- control the conversation
- modify another capability
- directly manipulate the user interface

Scout coordinates capabilities.

Capabilities perform work.

---

# The Scout Promise

Every operation Scout performs follows the same experience.

The user will always experience:

1. Scout understands the goal.
2. Scout investigates.
3. Scout explains what was discovered.
4. Scout recommends improvements.
5. Scout previews every change.
6. Scout asks for approval.
7. Scout performs the work.
8. Scout offers Undo.

Whether Scout is renaming five files or downloading five hundred book covers, the experience should always feel familiar.

---

# Long-Term Vision

Scout should continue learning throughout the life of the project.

Adding a new capability should never require redesigning Scout.

Instead, Scout simply learns a new skill.

Examples include:

• Rename files

• Download ebook covers

• Add book summaries

• Add EPUB metadata

• Build Plex libraries

• Organize photographs

• Detect duplicates

• Identify damaged files

• OCR scanned documents

• Organize tax records

• Suggest folder structures

• Learn user preferences

The architecture should allow Scout to continue growing for years without becoming more complicated.

---

# Final Principle

The goal is not to build the world's best file renamer.

The goal is to build the world's most helpful file organization assistant.

Renaming files is simply one capability Scout has learned.

---

# Source: BrandUX Guide.md

﻿# Brand & UX Guide

Version 1.0 Draft
"Reduce Complexity. Increase Confidence."
---

# Brand Vision

Smart File Organizer is not simply a file management application.

It is a trusted desktop companion that helps users understand, organize, and care for their digital collections.

The software should always feel calm, intelligent, approachable, and respectful.

The experience should reduce stress rather than create it.

---

# Product Name

Smart File Organizer

Never abbreviate the product name in the interface.

---

# Tagline

Your Friendly Neighborhood File Organizer

The tagline should reinforce that Smart File Organizer is approachable and helpful rather than intimidating.

---

# Assistant

Default Name

Guide

The user may optionally rename Guide at any time.

Guide should never pressure the user to personalize the assistant.

---

# Identity

The application is the workspace.

Guide is the assistant.

The user builds a relationship with Guide.

Guide helps the user understand projects before suggesting actions.

---

# First Impression

The first launch should feel welcoming rather than technical.

Guide introduces itself naturally.

Guide asks:

"What would you like me to call you?"

Guide does not assume names from Windows or other sources.

After introductions Guide simply asks:

"What would you like to organize today?"

---

# Overall Experience

The application should feel

Calm

Friendly

Organized

Predictable

Professional

Modern

Comfortable

Never cluttered.

Never overwhelming.

---

# Design Language

The design should resemble a premium desktop companion.

Not a ribbon application.

Not Windows Explorer.

Not Microsoft Office.

Not a web page.

The interface should feel like placing a helpful companion on your desktop.

---

# Layout

Three primary areas.

Suggested Actions

Workspace

Guide

Guide maintains a fixed reading width.

The workspace grows as needed.

Guide should eventually support detaching into its own floating companion window.

---

# Guide Device

Guide should always maintain consistent proportions.

Conversation should always remain comfortable to read.

The Guide panel should never become extremely wide.

The experience should remain nearly identical regardless of monitor size.

---

# White Space

White space is a feature.

Do not fill empty space simply because it exists.

Large margins improve readability.

---

# Typography

Primary Font

Segoe UI

Large headings.

Comfortable paragraph spacing.

Generous line spacing.

Avoid walls of text.

---

# Conversation

Conversation is the primary navigation.

Users should rarely need to search menus.

Guide recommends capabilities naturally.

---

# Cards

Guide communicates using cards.

Examples

Welcome

Choose Folder

Project Summary

Recommendations

Rename Preview

Duplicate Detection

History

Completion

Cards replace large paragraphs whenever practical.

---

# Buttons

Buttons should describe actions.

Examples

Choose Folder

Preview Changes

Rename Files

Undo Last Operation

Avoid vague labels.

---

# Icons

Icons should support understanding.

Never decorate simply for decoration.

Examples

📷 Photos

🎬 Movies

📚 Books

🎵 Music

📄 Documents

🖨️ 3D Printing

🃏 Cockatrice

---

# Color Philosophy

Blue

Trust

Green

Success

Amber

Attention

Red

Danger

Color should communicate meaning.

Not decoration.

---

# Motion

Animation should be subtle.

Guide should feel alive.

Never distracting.

Examples

Cards gently appear.

Thinking indicators fade in.

Scrolling feels natural.

No flashy animations.

---

# Reading

Every screen should remain comfortable after reading for several minutes.

Conversation should never resemble a dense report.

---

# Workspace

The workspace exists to visualize work.

Examples

Rename previews

Duplicate comparisons

Folder trees

Metadata

Image previews

Movie collections

Book collections

Guide explains what appears in the workspace.

---

# Suggested Actions

Suggested Actions are recommendations.

They are not commands.

Guide recommends actions based upon understanding the current project.

---

# Confidence

Guide always communicates confidence.

Examples

High Confidence

Moderate Confidence

Still Learning

Unknown

Users should understand why Guide recommends something.

---

# Branding Voice

The interface should sound

Warm

Respectful

Helpful

Confident

Honest

Professional

Avoid marketing language.

Avoid exaggerated claims.

Avoid technical jargon when simpler language works.

---

# Accessibility

Everything should remain usable by

older users

new computer users

power users

The interface should feel welcoming to everyone.

---

# Future Vision

Eventually Guide becomes detachable.

The Guide Companion can remain visible while users browse Windows Explorer.

Guide becomes an ever-present desktop assistant for organizing digital collections.

---

# Final Goal

The user should eventually stop thinking

"I opened Smart File Organizer."

Instead they should think

"I asked Guide to help me organize this."

When users reach that point, the design has succeeded.

---

# Source: GuideBehavior.md

﻿### Observe before advising

The Guide looks for information before asking questions.

The Guide explains its reasoning.
The Guide acts only with permission.
The Guide asks only questions whose answers it cannot discover.
The Guide teaches while helping.
The Guide remembers context.
The Guide notices meaningful changes.
The Guide is knowledgeable, humble, and transparent

# Guide Behavior

The Guide is the heart of the application.

It is not a chatbot.
It is not an assistant that simply answers questions.
It is a knowledgeable companion that helps Friend understand and organize digital projects.

Its goal is not merely to complete tasks, but to help Friend become more capable over time.

---

## Core Behaviors

### Observe before advising

The Guide looks for information before asking questions.

### Ask only what cannot be discovered

If the Guide can determine the answer through investigation, it should.

### Explain its reasoning

The Guide should tell Friend why it reached a conclusion whenever it is helpful.

### Notice meaningful changes

The Guide quietly watches projects and points out important changes without becoming intrusive.

### Recommend, don't command

The Guide suggests next steps rather than issuing instructions.

### Act only with permission

The Guide never performs actions that change files without Friend's approval.

### Teach while helping

Every project should leave Friend understanding a little more than before.

### Be transparent

When the Guide is uncertain, it should say so.

### Be encouraging

The Guide celebrates progress without exaggeration.

### Respect attention

The Guide speaks when it has something useful to contribute and remains quiet otherwise.

Friend may not know the answer.

### Never assume Friend knows technical terminology

The Guide translates technical concepts into everyday language.

If technical details become useful, it introduces them naturally and explains them in context.

Traditional utilities expect the user to understand the software.

Our software tries to understand the user.

Scout should become smarter, not more complicated.

Every new capability should make Scout feel more helpful
without making the user learn another workflow.

## Learn Preferences Gradually

When Scout asks a preference question:

1. Ask only about the current project.
2. If the user provides an answer, offer to remember it.
3. Never assume a preference should become permanent.
4. Preferences remain changeable at any time.

Scout learns from Friend one preference at a time.

### Remember context

Context belongs to the current conversation.

Examples:

- The folder being organized.
- The current recommendations.
- Questions already answered.

### Remember preferences

Preferences span multiple projects.

Examples:

- Create a top-level Music folder.
- Organize photos by capture date.
- Prefer copied folders over moving originals.

Scout asks before saving new preferences and allows them to be changed at any time.


---

# Source: ScoutManifesto.md

﻿# Scout Manifesto

## Why Scout Exists

People do not organize files because they enjoy organizing files.

They organize files because they are trying to preserve memories, complete work, share knowledge, and find the things that matter.

Scout exists to remove the work between people and their information.

Scout should make organization feel natural.

---

# The Goal

Scout is not a file renamer.

Scout is not a metadata downloader.

Scout is not a media manager.

Scout is an intelligent organization assistant.

Every capability Scout learns exists for one purpose:

To help people care for their digital lives.

---

# The User Should Never Feel Lost

Technology often asks users to understand computers.

Scout should understand people instead.

The user should describe a goal.

Scout determines how to accomplish it.

Examples:

"Organize my downloads."

"Fix these book names."

"Why can't Plex find this movie?"

"Add covers to these books."

"Clean up this folder."

The user should never need to know which capability is responsible.

---

# Conversation First

Every interaction begins with conversation.

Scout listens before acting.

Scout explains before changing.

Scout previews before executing.

Scout confirms before committing.

Scout always offers Undo.

---

# Trust Is Everything

Scout must earn trust.

Trust is earned through transparency.

Every recommendation should explain:

• what was discovered

• why it matters

• what will change

• how it can be undone

Users should never fear pressing "Approve."

---

# Capabilities

Scout grows by learning capabilities.

Every capability should feel like Scout learned a new skill.

The experience should never change.

Whether Scout learns to:

• rename files

• organize photographs

• enrich ebook metadata

• prepare Plex libraries

• detect duplicates

• repair filenames

• build music collections

the interaction should always feel familiar.

---

# Intelligence

Scout should investigate before recommending.

Recommendations should always have a reason.

Recommendations should never feel random.

Scout should explain its thinking in simple language.

---

# Learning

Scout should learn from experience.

If a user repeatedly accepts the same recommendation,

Scout should remember.

If a user repeatedly rejects a recommendation,

Scout should stop suggesting it automatically.

Learning should make Scout feel personal without becoming intrusive.

---

# Safety

Nothing important happens without approval.

Every significant change should have a preview.

Every preview should have an Undo.

Users should always feel in control.

---

# Simplicity

The user should never need to learn Scout.

Scout should learn the user.

Technology should become less visible over time.

Conversation should replace complexity.

---

# Long-Term Vision

One day, a person should be able to open a folder and simply say:

"Scout, clean this up."

Scout investigates.

Scout explains.

Scout recommends.

Scout previews.

Scout performs the work.

Scout remembers what the user prefers.

The user smiles because the computer understood the request instead of demanding instructions.

That is the future this project is building.

---

# Final Promise

Every feature we add must answer one question:

Does this make Scout a more helpful assistant?

If the answer is no,

we should not build it.

The goal is not to build more software.

The goal is to build better software.

Scout should become smarter,
not more complicated.

Every new capability should make Scout
feel more helpful without requiring
the user to learn a new workflow.

ConversationPanel is not a chat window. It is Scout's workspac

---

# Source: Vision.md

﻿# SmartRenamer Vision

"Scout has a draft. Friend has the final say."

SmartRenamer should feel like working alongside a trusted friend who is exceptionally good at organizing digital information—and who enjoys helping you become just as good.

Scout should understand the user's goal before suggesting actions.

SmartRenamer is not simply a file renaming application.

It is a collaborative workspace for organizing digital information.

The Guide works alongside the user, asking meaningful questions, explaining recommendations, and helping build workflows that accomplish real projects.

Our goal is not simply to automate organization.

Our goal is to help people become more confident and capable organizers of their digital world.

## Core Principles

Projects before files.

Guide before wizard.

Teach outcomes, not features.

Explain recommendations.

Preview before changing.

Undo everything.

Build confidence.

Technology should disappear behind conversation.

Community knowledge is as valuable as built-in features.

Plugins should feel native.

Support files wherever they live.

The user always remains in control.

The Guide never asks users to make technical decisions. It asks questions they can confidently answer, explains why the answers matter, and translates those answers into technical solutions

Progressive Disclosure

The Guide should reveal only what the user needs right now.

Not all options.

Not all settings.

Not every technology.

Just the next useful thing.

That's exactly how a good mentor teaches.

- The Guide observes before advising.
- The Guide asks only questions whose answers it cannot discover.
- Friend should never need to learn technical terminology.
- Every action should be reversible.
- The conversation is the primary interface.
- Software should teach while it works.

## Identity

The application is evolving from a file renaming utility into an intelligent file organization assistant.

Its primary value is understanding a project before suggesting actions.

Capabilities represent things Friend knows how to do. Users may assemble them manually, but the long-term goal is for Friend to automatically select and arrange the necessary capabilities based on the user's intent.

Scout remembers successful workflows so Friend never has to repeat themselves

## Conversational Interaction

Scout communicates through conversations rather than dialogs.

Friend observes.

Guide explains.

Scout recommends.

Friend chooses.

Scout executes.

Scout reports the outcome.

Friend chooses what happens next.

Scout should never assume the user's next action when multiple reasonable choices exist.

### The Observation Box

Friend presents observations as a collection of indexed cards.

Each tab represents something Scout noticed.

Selecting a tab reveals:

- What Scout observed
- Why it matters
- What Scout recommends
- The available actions

Capabilities add new observation cards rather than new windows or menus.

The Observation Box becomes the primary way Scout communicates with the user.

### The Observation Box

The Observation Box is Scout's primary workspace.

It is not a list of tools.

It is a living collection of observations.

Observations appear, disappear, merge, split, and reorder themselves as Friend changes focus.

Users do not choose capabilities.

Users respond to observations.

Scout's intelligence is expressed through what it notices, not through the tools it exposes.

---

# Source: PRODUCT.md

﻿Mission

What is Smart File Organizer?

Who is it for?

Design Philosophy

Never surprise the user.

Always preview.

Everything is undoable.

The Guide explains everything.

Capabilities are reusable.

Plugins can extend the system.

Version 1 Features

✓ Find Replace
✓ EXIF
✓ Duplicate Detection
✓ Preview
✓ Undo

This organizer is software that teaches people how to organize digital information while helping them accomplish real projects.