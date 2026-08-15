Step 1 — Define what every Expedition must provide

This isn't code yet. It's the checklist that every expedition must satisfy.

I'd put this in Documentation, and then we can implement it one item at a time.

Expedition Contract v1

Every expedition provides:

Identity
Expedition Name
Description
Version
Author (optional)
Camp
Header layout
Camp animation
Discovery window
Progress trail
Theme
Colors
Typography
Icons
Illustrations
Atmosphere
Ambient sound (optional)
Animation
Motion
Accessibility options
Scout Presentation

(Notice I didn't call this "Scout.")

The expedition provides how Scout is presented:

Typography
Colors
Avatar style (if any)
Tone
Vocabulary

Scout himself never changes.

Components

Reusable UI pieces.

Examples:

Observation Cards
Discovery Cards
Recommendation Cards
Headers
Buttons
Layouts

How everything fits together.

Camp

Observation panel

Workspace

Conversation

Status bar

Journey

The expedition defines:

Progress animation
Discovery transitions
Completion message
Assets

Images

Icons

Animations

Sounds

# Expedition Presentation Contract

## Purpose

The Expedition Presentation Contract defines how an Expedition supplies
visual identity and presentation information to Scout without requiring
Scout's application shell to know which Expedition is active.

This is part of Scout's Expedition architecture.

The goal is to keep:

- Scout's application shell generic.
- Expedition identity inside the Expedition.
- Expedition-specific presentation inside the Expedition.
- Future Expeditions independent from Safari.
- The Safari Expedition suitable as a reference/template for future
  Expeditions.

---

# Architectural Principle

Scout provides the application framework.

An Expedition provides the experience.

The relationship is:

    Scout
      |
      v
    ExpeditionManager
      |
      v
    ExpeditionManifest
      |
      +-------------------+
      |                   |
      v                   v
    Scout Shell       Expedition Resources
                          |
                          +-- Theme
                          +-- Header
                          +-- Artwork
                          +-- Controls
                          +-- Future Expedition resources

Scout should not contain hard-coded references to individual Expeditions.

For example, MainWindow should not contain:

    SafariHeader

Instead, Scout provides a generic presentation host:

    ExpeditionHeaderHost

The active Expedition determines which header is loaded.

---

# Manifest Contract

The Expedition manifest is the declarative description of an Expedition.

Current manifest fields include:

| Field | Purpose |
|---|---|
| `ManifestVersion` | Version of the manifest schema |
| `Name` | Human-readable Expedition name |
| `Author` | Expedition author |
| `Version` | Expedition version |
| `Description` | Human-readable Expedition description |
| `SupportsDarkMode` | Indicates dark-mode support |
| `ThemeResource` | Expedition ResourceDictionary |
| `HeaderResource` | Expedition-provided header |
| `HeaderTitle` | Title supplied to the Expedition header |
| `HeaderSubtitle` | Subtitle supplied to the Expedition header |
| `ProgressControl` | Expedition progress visualization extension point |

---

# Header Contract

## HeaderResource

`HeaderResource` identifies the visual header supplied by the Expedition.

The path is relative to the Expedition directory.

Example:

    "HeaderResource": "Components/SafariHeader.xaml"

The Scout application shell does not need to know that the resulting control
is `SafariHeader`.

This is intentional.

---

# HeaderTitle

`HeaderTitle` supplies the title displayed by the Expedition header.

Example:

    "HeaderTitle": "Scout"

The value belongs to the Expedition manifest rather than MainWindow.xaml.

This allows each Expedition to define its own presentation language.

A future Expedition might use:

    "HeaderTitle": "Music Scout"

without requiring a modification to Scout's MainWindow.

---

# HeaderSubtitle

`HeaderSubtitle` supplies the descriptive subtitle displayed by the
Expedition header.

Example:

    "HeaderSubtitle": "Your Guide Through THIS FILE SAFARI"

Again, the wording belongs to the Expedition.

Scout provides the mechanism for displaying it, but the Expedition supplies
the content.

---

# Header Loading Flow

The current header architecture is:

    Expedition.json
          |
          v
    ExpeditionManifest
          |
          v
    ExpeditionManager
          |
          v
    CurrentManifest
          |
          v
    ExpeditionHeaderHost
          |
          v
    HeaderResource
          |
          v
    Expedition Header
          |
          v
    HeaderTitle / HeaderSubtitle

This allows Scout to remain independent of Safari.

---

# Scout Responsibilities

Scout is responsible for:

- Loading the active Expedition.
- Reading the Expedition manifest.
- Resolving Expedition resources.
- Providing generic presentation hosts.
- Providing the application shell.
- Providing shared workflow infrastructure.
- Providing Expert infrastructure.

Scout should not:

- Hard-code Safari-specific headers.
- Hard-code Expedition-specific wording.
- Assume that every Expedition uses Safari artwork.
- Reference a specific Expedition from MainWindow.
- Duplicate Expedition presentation logic.

---

# Expedition Responsibilities

An Expedition is responsible for:

- Its identity.
- Its description.
- Its presentation language.
- Its artwork.
- Its theme resources.
- Its header implementation.
- Its Expedition-specific controls.
- Its domain-specific resources.
- Its future domain-specific capabilities.

The Expedition should be self-contained enough that another Expedition can be
introduced without rewriting Scout's application shell.

---

# Safari as the Reference Expedition

Safari is currently the default Scout Expedition.

Safari therefore serves two purposes:

1. It is the first working Expedition.
2. It is the reference implementation for future Expeditions.

The Safari Expedition should demonstrate the conventions that future
Expeditions are expected to follow.

This makes the completed Safari Expedition part of the template for future
Scout development.

---

# Example Safari Manifest

The current Safari manifest is:

    {
      "ManifestVersion": 1,
      "Name": "Safari",
      "Author": "Jonathan R Holman",
      "Version": "1.0.0",
      "Description": "The default Scout Expedition.",
      "SupportsDarkMode": true,
      "ThemeResource": "Foundation/SafariTheme.xaml",
      "HeaderResource": "Components/SafariHeader.xaml",
      "HeaderTitle": "Scout",
      "HeaderSubtitle": "Your Guide Through THIS FILE SAFARI",
      "ProgressControl": ""
    }

---

# Future Expeditions

A future Expedition should be able to provide its own values.

For example:

    {
      "ManifestVersion": 1,
      "Name": "Music",
      "Author": "Example Author",
      "Version": "1.0.0",
      "Description": "A Music Expedition.",
      "SupportsDarkMode": true,
      "ThemeResource": "Foundation/MusicTheme.xaml",
      "HeaderResource": "Components/MusicHeader.xaml",
      "HeaderTitle": "Music Scout",
      "HeaderSubtitle": "Your Guide Through the Music Expedition",
      "ProgressControl": ""
    }

The important point is that Scout's MainWindow does not need to change.

Only the Expedition manifest and Expedition resources change.

---

# Design Rule

When adding a new piece of presentation information, ask:

> Does this describe Scout, or does this describe the active Expedition?

If it describes Scout, it belongs in the Scout shell.

If it describes the Expedition, it should normally belong in the Expedition
manifest or Expedition resources.

This distinction should be maintained throughout the project.

---

# Relationship to Future Expert Creation

The Expedition architecture is intended to support a larger Scout goal:

Scout should eventually be able to use the completed Expedition structure as
a model for creating or assisting with future Experts and future Expeditions.

Therefore, the Expedition structure should remain:

- Explicit.
- Documented.
- Consistent.
- Discoverable.
- Self-contained.
- Reusable as a template.

The Safari Expedition is being developed as the reference implementation for
that purpose.

---

# Current Status

Completed:

- Expedition manifest loading.
- Active Expedition tracking.
- Expedition-relative resource resolution.
- Expedition theme loading.
- Expedition-provided header loading.
- Generic Scout header hosting.
- Expedition-owned header title.
- Expedition-owned header subtitle.
- Removal of direct Safari header dependency from MainWindow.

Next areas of Expedition development should continue from the existing
Safari Expedition documentation and implementation rather than creating
parallel architecture.