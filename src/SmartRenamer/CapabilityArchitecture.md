# Capability Architecture

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