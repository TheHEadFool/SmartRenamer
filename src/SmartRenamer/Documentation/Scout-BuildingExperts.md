## Scout -- Building Experts

# Purpose

This document captures the long-term vision for how new Scout Expertswill be created. 
It is not part of the current implementationsprint. 
The immediate priority remains completing the Ebook Expert,
using it as the reference implementation for the Scout Expert SDK.

# Vision

Scout should eventually be able to help people create new Expertsthrough a natural conversation.

The person teaching Scout should not need to understand softwarearchitecture, programming, or AI.

They should only need to understand their files.

Scout's job is to translate that knowledge into a complete ExpertConstruction document that can 
be given to ChatGPT to generate a newstandalone Expert.

# Design Philosophy

The Expert Builder should feel like a conversation, not a technicalform.

Instead of asking:

MIME Types

Namespaces

Interfaces

Class Design

Scout should ask simple questions such as:

What are these files?

What program normally opens them?

What do people use them for?

What information is important?

What problems do people usually have?

What would you like Scout to help people discover?

Scout should infer the technical details whenever possible.

# Conversation Example

Scout: > I'd love to help build a new Expert.

Scout: > What kind of files are these?

User: > Magic decks.

Scout: > What program usually opens them?

User: > Cockatrice.

Scout: > What do people use them for?

User: > Building and playing Magic: The Gathering decks.

Scout: > What problems do people usually run into?

User: > Illegal decks, duplicate decks, missing cards and poororganization.

The conversation continues until Scout has enough understanding of thedomain.

# Confirmation

Before producing the construction document, Scout summarizes itsunderstanding.

Example:

These files are used by Cockatrice.

They store Magic: The Gathering decks.

Users want help finding illegal decks, duplicate decks, and missingcards.

Important information includes deck names, formats, commanders andcard lists.

Scout asks:

Did I understand correctly?

The user can correct or expand Scout's understanding before continuing.

# Output

After confirmation, Scout generates a completed Expert Constructiondocument.

The user can copy that document directly into ChatGPT.

ChatGPT uses it to generate a complete standalone Scout Expert thatfollows the Scout Expert SDK.

# Goals

The finished Expert should:

Be self-contained.

Be independently distributable.

Follow the Scout Expert SDK.

Require no manual architectural design by the user.

Be ready to plug into Scout.

# Guiding Principle

People should never have to think like programmers to teach Scout.

They should only have to think like experts in the files they alreadyunderstand.

Scout is responsible for translating human expertise into softwarearchitecture.

# Current Status

This document records a future capability.

It belongs in the project roadmap and should remain in the Parking Lotuntil the Ebook Expert is complete.

Current priority:

Finish the Ebook Expert.

Use it as the reference implementation for the Expert SDK.

Migrate the Music Expert to the new architecture.

Build additional Experts.

Return to implement the conversational Expert Builder.

## Research Assistant Pattern

Each Investigation consists of one or more research assistants.

Research assistants:

- acquire information
- measure
- observe
- preserve evidence
- build reports

Research assistants never:

- interpret findings
- produce recommendations
- communicate with Scout

Consultants consume reports.

Consultants provide:

- observations
- recommendations
- warnings
- opportunities

Scout communicates the final experience to the user.

This separation keeps research independent from interpretation 
and allows additional research assistants to be added without 
changing existing investigations.

/// =========================================================================
/// Scout Design Law #1
/// =========================================================================
///
/// Every class that represents domain knowledge must be generatable
/// from an interview.
///
/// If Scout cannot generate the class from structured interview
/// responses, then either:
///
/// • the class is incomplete,
/// • the interview is incomplete,
/// • or the architecture needs improvement.
///
/// Every completed class produces:
///
/// • the implementation,
/// • the generation template,
/// • the interview definition,
/// • the ChatGPT prompt schema.
///
/// The implementation is never considered complete until all four
/// artifacts exist.
///
/// =========================================================================

Scout Design Law #2

Scout never asks for information it can reasonably infer.

For example:

If the user says:

"I'm organizing MP3 files."

Scout already knows:

there will probably be artists,
albums,
genres,
years,
track numbers.

It shouldn't ask all of those immediately.

Instead it says:

"I believe artist, album, genre, year and track number are common pieces of music metadata. Are those important for your collection?"

Now the user is confirming or correcting, not inventing.

Another design law.
Scout Design Law #3

Information is collected incrementally.

Never try to build an Expert in one conversation.

Instead:

Interview 1
    ↓
Create Investigation

Interview 2
    ↓
Create Consultant

Interview 3
    ↓
Create Translator

Interview 4
    ↓
Create Signals

That solves the context window problem naturally.

 Scout Design Law #4

 ChatGPT never generates Scout architecture. It generates structured domain knowledge.

Scout owns:

    Architecture
    Templates
    Assembly
    Validation
    Integration

ChatGPT owns:

    Domain knowledge
    Taxonomies
    Metadata locations
    File formats
    Common organization strategies
    Common repair strategies
    Best practices

   That separation of responsibilities gives you something very powerful: 
as Scout's architecture evolves, you update the templates once, 
while the same Knowledge Package format can continue to populate 
those templates. It keeps the software engineering decisions inside 
Scout and uses ChatGPT where it provides the most leverage—building 
comprehensive, structured knowledge that can be dropped into the 
Expert Factory in a single copy/paste operation. I think that's the 
architecture that will scale.

Scout Design Law #5

The user supplies intent. ChatGPT supplies domain knowledge. Scout supplies architecture.

That means responsibility is perfectly divided:

User: "I want to organize..."
Scout: "I know how Experts are built."
ChatGPT: "I know this domain."

None of them are asked to do the other's job.