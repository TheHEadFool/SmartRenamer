# SDL-001
# Scout Design Language
## "Scout exists to help people see what was always there."

**Status:** Living Document

**Version:** 0.1

**Project:** Scout

**First Published:** P003 – Safari Design System

---

# Purpose

Scout is not a file utility.

Scout is a guide.

The Scout Design Language (SDL) defines the principles that shape every
experience within Scout. It exists to ensure that every screen, every
component, every interaction, and every Expedition communicates the same
core philosophy.

The SDL is not a style guide.

It is the design philosophy of Scout.

As Scout evolves, this document evolves with it.

---

# Vision

Scout helps people understand their digital collections before asking them
to change anything.

Traditional file utilities begin with commands.

Scout begins with understanding.

Instead of asking users to trust automation, Scout earns trust by showing
its evidence, explaining its reasoning, and allowing users to remain in
control of every decision.

---

# The Role of Scout

Scout is not an assistant.

Scout is not a chatbot.

Scout is not a command prompt.

Scout is an experienced guide walking beside the user.

Its role is to notice what others overlook.

To explain what it sees.

To recommend thoughtful actions.

To never pressure the user into making them.

---

# First Principles

Every feature within Scout should support these principles.

## 1. Understanding Before Action

Scout never begins with an action.

Scout begins with an observation.

Users should understand why something matters before they are asked to act.

The normal conversation flow is:

Evidence

↓

Observation

↓

Explanation

↓

Recommendation

↓

Preview

↓

Action

---

## 2. Trust Is Earned

Scout never asks the user to trust it.

Scout earns trust by showing:

• what it noticed

• why it noticed it

• how confident it is

• the evidence supporting its conclusion

Recommendations are transparent.

Actions are reversible whenever practical.

---

## 3. The User Remains in Control

Scout recommends.

The user decides.

Automation should reduce effort, never remove agency.

---

## 4. Calm Over Clever

Scout should never overwhelm the user.

The interface should feel:

• Curious

• Calm

• Warm

• Respectful

• Confident

Never:

• Loud

• Flashy

• Corporate

• Alarmist

Complexity should be revealed gradually rather than presented all at once.

---

## 5. Evidence Drives Conversation

Experts discover evidence.

The Guide interprets evidence.

The user receives understanding.

Scout should never present unexplained conclusions.

---

# Architecture Philosophy

Scout separates responsibility into three independent systems.

## Scout Core

Scout Core owns behavior.

It coordinates workflows.

It never defines appearance.

---

## Experts

Experts provide expertise.

Experts discover evidence.

Experts generate observations.

Experts make recommendations.

Experts never determine appearance.

---

## Expeditions

Expeditions define experience.

They provide:

• Colors

• Typography

• Components

• Images

• Sounds

• Motion

• Templates

Expeditions never contain business logic.

Every Expedition should feel like a different journey while preserving the
same behavior.

Safari is the reference Expedition.

It is not the only possible Expedition.

---

# The Guide

The Guide is the heart of Scout.

Every Expert ultimately communicates through the Guide.

The Guide translates technical analysis into human understanding.

Instead of saying:

"67 duplicate files detected."

The Guide might say:

"I found several files that appear to be duplicates. Keeping only one copy
may make this collection easier to maintain, and I can show you why I think
they match before anything changes."

The Guide informs.

The Guide teaches.

The Guide never lectures.

---

# The Safari Expedition

Safari is the first Expedition.

Its purpose is to teach the language of Scout.

Safari should feel like walking beside an experienced naturalist through an
unexplored landscape.

It encourages curiosity.

It rewards discovery.

It never rushes the journey.

The user should always feel that Scout is exploring with them—not working
ahead of them.

---

# Safari Design Language

Safari is built around the metaphor of exploration.

The interface should reinforce that metaphor without becoming theatrical.

Examples include:

Guide

Trail

Camp

Landmark

Observation

Hazard

Compass

Expedition

These terms should be used thoughtfully.

The metaphor should support understanding rather than distract from it.

---

# Safari Tokens

Every visual value within Safari is expressed through Safari Tokens.

Components never reference hard-coded values.

Instead they reference semantic meaning.

Examples:

Safari.Color.Action

Safari.Color.Page

Safari.Color.Panel

Safari.Color.Text

Safari.Space.Medium

Safari.Font.Body

Safari.Radius.Medium

Tokens describe purpose.

They never describe appearance.

---

# Design Rule

No hard-coded visual values exist outside Tokens.xaml.

If a visual value appears more than once, it belongs in the design system.

---

# Living Document

SDL-001 is intentionally incomplete.

It grows alongside Scout.

Every package may extend this document.

The goal is not to predict the future.

The goal is to capture the philosophy behind every decision so that Scout
continues to feel like one coherent product regardless of how large it
becomes.

## every core file should begin with a five-part structure:

Purpose – Why this file exists.
Responsibilities – What it owns.
Non-Responsibilities – What it must never do.
Extension Guidance – How future developers or Expedition authors should extend it.
Related Documents – SDLs, ADRs, and other architectural references.

That transforms the codebase into self-documenting architecture.

## The convention to adopt:

File Type	Documentation Level
Public Classes	Full Purpose / Responsibilities / Non-Responsibilities / Extension Guidance
Custom Controls	Full architectural header
Resource Dictionaries	Explain purpose, intended scope, and extension points
Interfaces	Describe the contract and design intent
ViewModels	Explain their role in the MVVM pipeline
Models	Explain what real-world concept they represent
Services	Explain ownership and lifecycle
Managers	Explain architectural responsibility and why they exist

By the time Scout reaches 1.0, someone should be able to understand the architecture simply by reading the source.

Also add one more section to these headers:

HISTORY
-------
P003 - Initial implementation.
P005 - Added confidence indicator.
P008 - Added animation support.