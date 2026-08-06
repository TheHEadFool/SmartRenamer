## The EDK is descriptive, not prescriptive. 
It captures proven patterns from completed Experts. 
New patterns are added only after they have been validated in working code.


## Build Together

The AI is responsible for navigation.

The developer is responsible for implementation.

The AI should inspect the current project before proposing changes.

The developer should not spend time searching for files or reconstructing architecture from memory.

Both participants work from the current project as the shared source of truth.




1. Investigation-Centered Architecture

Replace the old concept of shared Blocks/Reports with:

An Investigation is the fundamental unit of development.

Each Investigation owns everything needed to answer a single research question.

Example:

Organization
│
├── Organization.md
├── Blocks
├── Reports
├── Evidence
├── Findings
└── Consultants
2. Research Assistant Philosophy ⭐

This came directly from your insight today.

Replace the "mini expert" concept with:

An Investigation is a self-contained research assistant.

Responsibilities:

Gather evidence
Organize observations
Preserve findings
Never make final conclusions

Its Consultant interprets the research.

Scout synthesizes the Consultants.

3. Responsibility Hierarchy

Document the flow as:

Scout
    │
Expert
    │
Investigation
    │
    ├── Block(s)
    ├── Report
    └── Consultant
            │
            ▼
      ExpertFinding(s)

The consultant isn't "after" the report in a pipeline—it's part of the investigation that interprets the report.

Every Report should contain:

• Facts

• Statistics

• Evidence

Reports never make decisions.
Reports never produce recommendations.
Reports simply describe what was discovered.

Scout
    │
asks a question
    │
    ▼
Expert
    │
coordinates Investigations
    │
    ▼
Investigation
    │
coordinates Blocks and Consultants
    │
    ├──────────────┐
    ▼              ▼
Block         Consultant
    │              │
collects      interprets
facts          facts
    │              ▲
    ▼              │
 Report ───────────┘
    │
    ▼
ExpertFinding
    │
    ▼
Scout

The Consultant interprets the Report and prepares ExpertFindings for the Expert. The Expert combines findings from all Investigations and presents them to Scout.

4. Investigation Design Rule

Each Investigation answers exactly one question.

Examples:

Investigation	Research Question
Metadata	What metadata exists?
Organization	How is the collection organized?
Contents	What is inside the books?
Duplicates	Where are conflicting or duplicate books?
Quality	How healthy is the collection?
5. Extensibility Principle

Document the design goal:

Experts are extended by adding Investigations, not by modifying existing ones.

That means future work like:

Accessibility
OCR
DRM
Reading Level
Illustrations

can simply be added as new Investigation folders without changing Metadata or Organization.

Once an Investigation has paid the cost of acquiring information, it should exhaust all objective observations available from that information before requiring additional infrastructure.

Once our reference implementation is complete, I'd like every build instruction to follow this template:

EE-005.004
-----------------------------------------

File
-----
OrganizationBlock.cs

Location
--------
Line 138

Action
------
Replace ResearchSeries()

Reason
------
Adds missing series research while we already have the metadata.

Expected Build
--------------
0 Errors
0 Warnings

Commit Message
--------------
Research publisher and language organization

That feels almost like a professional engineering change order. It's easy to follow, easy to audit, and if we ever need to revisit a change, we know exactly what was modified and why.

I actually think this should become part of the Expert Development Kit as well. The SDK shouldn't just define how to write Experts—it should define how we evolve them, one deliberate, verifiable step at a time.

6. Pipeline Principle ⭐⭐⭐
## Pipeline Principle

Research is acquired once.

Reports may be consumed by any Investigation that can
objectively use the information.

Investigations should never reacquire information that is
already available in an upstream Report.

Example

Files
    ↓
Metadata Block
    ↓
MetadataReport
    ├── Organization
    ├── Quality
    ├── Duplicates
    ├── Repair
    └── Enrichment

This minimizes duplicated work, improves performance,
and keeps responsibilities clearly separated.

7. Reports Are Immutable Research
## Reports

Reports preserve facts.

Reports never:

- interpret
- recommend
- communicate
- modify files

Reports exist solely to preserve objective research for later
consumption by Consultants.

8. Consultant Rule
## Consultant Responsibilities

Consultants consume Reports.

Consultants:

- identify observations
- identify opportunities
- identify warnings
- produce recommendations
- create ExpertFindings

	## Consultant Output

	A Consultant returns one or more ExpertFindings.

	Investigations coordinate Blocks and Consultants.

	Investigations should not translate recommendations into

ExpertFindings.

This keeps interpretation and conclusions together.

Consultants never acquire raw data.

9. Vertical Slice Completion ⭐⭐⭐

## Vertical Slice Development

Complete one Investigation before beginning the next.

Definition of Complete

✔ Block
✔ Report
✔ Evidence (if required)
✔ Consultant
✔ Investigation
✔ Green Build

Avoid partially implementing multiple Investigations simultaneously.

10. Development Workflow

## Development Workflow

Every implementation follows this rhythm:

Understand
↓

Navigate

↓

One file

↓

One logical change

↓

Build

↓

Green

↓

Repeat

11. Navigation Standard ⭐⭐⭐
## Navigation Standard

Every implementation instruction should include:

File

Location

Action

Expected Build

Reason

Commit Message (when appropriate)

The developer should never have to search for where code belongs.

12. Source of TrutH

## Source of Truth

The current project is always authoritative.

Architecture discussions should begin by inspecting the live project.

Do not rely on memory when the code can answer the question.

## Block Growth Rule

Begin with a single Block.

Split a Block only when:

- it acquires information from a different source,
- it becomes difficult to understand,
- or it performs more than one independent acquisition task.

Do not split Blocks merely because they become long.
Split them because responsibilities diverge.

Blocks are decomposed by acquisition of facts, not by interpretation.

For example:

Good Block split

Metadata
    ├── Read EPUB metadata
    ├── Validate metadata
    ├── Collect evidence

Each acquires different facts.

Bad Block split

Metadata
    ├── Good metadata
    ├── Bad metadata

That's interpretation.

That belongs in the Consultant.

When a simplification removes code and improves responsibility, adopt it early—before it spreads.

That's different from redesigning.

We're not adding abstraction.

We're removing an unnecessary layer.

I think that's exactly the kind of refinement that's worth making while the codebase is still small.

## When a component exists, implement it as completely as practical before moving on.

Do not repeatedly revisit a completed component to add a few lines at a time.

# Complete an Investigation while you are working in it. Avoid revisiting files to make small incremental 
additions unless fixing a defect or extending a proven capability.


# Every domain-specific class is prefixed with its Expert identifier (E_, D_, M_, etc.). This makes ownership immediately obvious and prevents naming collisions across Experts.

# Leave Every File Better Than You Found It.

If you're editing a file anyway:

Fix the easy IDE warnings.
Improve readability.
Keep the architecture consistent.
Do not expand the scope of the task.

That gives us continuous improvement without derailing progress.

# The architecture is allowed to evolve until the first Expert is complete. 
After that, the architecture should become stable and the Experts should evolve instead.

## Doumentation Rule
Prefer improving an existing document over creating a new one.

A new document should only be created when:
• The topic has no natural home.
• Adding it would make an existing document harder to understand.
• It represents a new architectural concept.

# I think we should reserve prefixes.

Prefix	Meaning
Single Letter	Domain (Experts)
Double Letter	Framework
No Prefix	Models and generic objects

# architectural rule.

    Single-letter prefixes identify domain-specific classes.

    Double-letter prefixes identify Scout framework classes.

    Shared models remain unprefixed.

## Experts understand the user's files. 
The Conversation Framework understands the user's goals.

# If renaming a class changes where it belongs,
the prefix was describing ownership correctly.

# Every Expert owns the language used to explain its own knowledge.

Scout owns the conversation. Experts own the vocabulary.

    The Conversation Framework decides when to speak.

    The Expert decides how its knowledge should be expressed.

# Expert Lifecycle
    Scout
    │
    ▼
    ObservationExpert
    │
    ▼
    Investigations
    │
    ▼
    Reports
    │
    ▼
    Consultants
    │
    ▼
    ExpertFindings
    │
    ▼
    Recommendation Translator
    │
    ▼
    CV_Recommendations
    │
    ▼
    Conversation Planner
    │
    ▼
    Conversation


##  Responsibilities

    Observation Framework

Answers one question.

What do I know?

Produces

ExpertFindings

Stops.

Recommendation Translator

Answers one question.

How should my Expert explain this finding?

Produces

CV_Recommendation

Stops.

    Conversation Framework

Answers one question.

Which recommendation should I discuss next?

Produces conversation.

Stops.

    Expert Responsibilities

Every Expert promises to:

✓ Investigate its domain.

✓ Produce consistent ExpertFindings.

✓ Translate those findings into Recommendations.

✓ Never interact with the UI.

✓ Never participate directly in conversation.

    Conversation Responsibilities

Conversation promises to:

✓ Receive Recommendations.

✓ Prioritize Recommendations.

✓ Maintain conversation history.

✓ Understand user intent.

✓ Decide what to discuss next.

✓ Never understand EPUBs.

✓ Never understand MP3s.

✓ Never understand Photos.

    The Expert Template

Every generated Expert contains:

Expert

│

├── Investigations

├── Blocks

├── Reports

├── Consultants

├── Recommendation Translator

├── Resources

└── Documentation

Nothing more.

Nothing less.

That is a complete Expert

## An Expert is a self-contained module that understands one domain.

It discovers facts, reasons about those facts, and recommends actions using the vocabulary of its own domain.

An Expert never manages conversations.

An Expert never renders a user interface.

An Expert communicates exclusively through shared contracts.










Scout begins every Expedition by asking one question:

"What can I help the user accomplish?"

An Expedition has three distinct phases.

Phase 1 — Observe

The Observation Framework investigates the user's files.

Produces:

    ExpertFindings

The Observation Framework stops.

--------------------------------------------

Phase 2 — Translate

Each Expert translates its own ExpertFindings into
CV_Recommendations using the vocabulary of its own domain.

Produces:

    CV_Recommendations

The Experts stop.

--------------------------------------------

Phase 3 — Guide

The Conversation Framework receives all Recommendations,
determines which recommendation should be discussed next,
and guides the user through the Expedition.

Produces:

Conversation

The Conversation Framework never analyzes files.

--------------------------------------------

Ownership

Observation discovers.

Experts explain.

Conversation guides.

Each framework owns one responsibility and communicates
only through shared contracts.


