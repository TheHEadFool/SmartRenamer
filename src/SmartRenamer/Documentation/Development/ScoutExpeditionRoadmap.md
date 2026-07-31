Scout Expedition Roadmap
Building the First Expedition (Safari)
Purpose

This roadmap exists to keep development focused on building complete expedition experiences rather than isolated UI features.

Whenever a new feature is proposed, ask:

Does this make the expedition feel more real?

If not, it probably belongs in the Parking Lot until the core expedition experience is complete.

Guiding Principle

The Safari Expedition is the reference implementation.

Every future expedition should be able to inherit the same framework while expressing its own unique world, personality, and atmosphere.

We are not building a jungle-themed application.

We are building the expedition framework through the Safari Expedition.

Development Milestones
Milestone 1 — Expedition Framework (Foundation)

Goal

Create the architectural foundation that allows Scout to exist inside different worlds.

Focus on:

Expedition abstraction
Expedition Manager
Shared expedition interfaces
Personality separation
Vocabulary separation
Header/content ownership
Clean separation between Scout Core and Expeditions

Definition of Done

Scout Core has no knowledge of jungles, museums, space, snow, or any expedition-specific concepts.

All expedition-specific presentation flows through the Expedition framework.

Milestone 2 — Expedition Timeline

Goal

Create a single timeline that represents the user's journey through an expedition.

Example stages:

Welcome
Discovering
Discoveries Available
Investigation
Recommendations
Preview
Decision
Expedition Complete

Everything in the interface should react to this timeline.

Examples:

Header
Progress indicator
Conversation
Observation panel
Discovery Workspace
Status messages

The timeline becomes the conductor for the entire experience.

Milestone 3 — Discovery Workspace

Goal

Build the heart of every expedition.

Every discovery should tell the same story.

Standard structure:

Current Discovery
Why Scout noticed it
Supporting evidence
Things we could do

Future expeditions may change the presentation, but never the overall flow.

Users should always understand:

What Scout found
Why it matters
Why Scout noticed it
What options are available
Milestone 4 — Living Expedition

Only after the architecture is complete should we focus on atmosphere.

Safari examples:

Animated campfire
Progressive footprints
Organic motion
Expedition artwork
Ambient life
Natural transitions

Animation should support immersion.

It should never distract from the user's work.

What Belongs in Scout Core

Scout Core owns:

Discovery
Analysis
Recommendations
Preview
Safety
User control
Honesty
Consistent behavior

Scout Core should remain completely unaware of the currently selected expedition.

What Belongs to an Expedition

An Expedition owns:

Visual identity
Color palette
Typography
Artwork
Animation
Ambient sound
Vocabulary
Metaphors
Persona
Emotional tone

The expedition determines how Scout expresses itself, never what Scout believes.

The Order Matters

Do not skip ahead.

Build the architecture.
Build the journey.
Build the workspace.
Add atmosphere.
Polish.

Beautiful artwork sitting on top of weak architecture creates a shallow experience.

Strong architecture naturally supports beautiful experiences.

The Litmus Test

Before implementing any feature, ask:

If I switch from Safari to Arctic, should this code change?

If the answer is:

Yes

it belongs inside the Expedition.

If the answer is:

No

it belongs in Scout.

Does this belong in Scout Core or in the Expedition?
Does this help the user feel like they are on an expedition?
Would every future expedition benefit from this design?

If the answer to any of these is "no," reconsider the implementation before writing code.

Long-Term Vision

The Safari Expedition is not the destination.

It is the proof that Scout can become different kinds of guides without ever ceasing to be Scout.

When future users choose an expedition, they are not selecting a theme.

They are choosing the world in which Scout will accompany them.



Never sacrifice the architecture for a clever visual effect. 
Every expedition should be able to inherit what we build today. 
If a feature cannot be generalized to another expedition, 
ask whether it truly belongs in the framework or only in Safari.

The Expedition Timeline is the single source of truth for the user's journey. 
Every expedition component should react to it rather than maintaining its own notion of progress.

Every expedition provides:

Scout Title

Which always completes the sentence:

Scout — Your Guide...

Safari provides:

"...on this File Safari."

Arctic provides:

"...on the Arctic File Exploration."

Museum provides:

"...through this Digital Museum."

The Expedition doesn't rename Scout.

It simply describes where Scout is guiding you.