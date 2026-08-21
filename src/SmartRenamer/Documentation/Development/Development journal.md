## 2026-07-19

### Lesson Learned

A runtime failure initially appeared to be an architectural problem.

Tracing the pipeline showed:

Guide
↓
Planner
↓
Preview
↓
Executor

All stages executed correctly.

The actual failure was an operating system exception caused by insufficient disk space.

Lesson:

Always reproduce the failure and trace the data before changing the design.

# Version 0.5 Completed

Date: 2026-07-19

## Goal

Create an organized copy of a folder by grouping related assets while preserving the originals.

## Result

SUCCESS

Scout now:

- Scans a folder.
- Detects related assets.
- Builds an execution plan.
- Shows a preview.
- Creates a "(Scout Organized)" copy.
- Renames related assets consistently.
- Preserves all original files.

## Lessons Learned

The architecture was largely correct.

The primary debugging lesson was to trace the data through the pipeline instead of redesigning components.

A runtime failure was ultimately traced to insufficient disk space rather than a planning error.

The Constitution was updated to reinforce:

- Follow the data before changing the design.
- Replace complete files instead of editing fragments.
- Finish the active feature before pursuing architecture improvements.

## Next Improvement

Scout should clearly communicate where the organized folder was created and offer convenient actions such as:

- Open Organized Folder
- Open Parent Folder

This becomes the first task for Version 0.6

## Today 7/19/26 12:58pm

Completed Version 0.5.

Discovered that organization already worked.

Failure caused by insufficient disk space.

Added completion card to Version 0.6 roadmap.

Captured drag-and-drop as a backlog item.

## Development Journal — Architectural Reset and Recovery Point

Date: August 19, 2026
Status: Architectural clarification / migration checkpoint
Purpose: Establish a durable recovery point for future development sessions.

# 1. Why This Entry Exists

The project has reached a point where several major architectural changes are happening simultaneously.

During development, it became increasingly difficult to distinguish:

intentional duplication during migration,
legacy code that is temporarily retained,
new code that is becoming authoritative,
the two different Conversation Engines,
Expert architecture,
execution architecture,
Workspace presentation,
conversational presentation,
and the state that must keep all of these synchronized.

This entry establishes the current architectural intent so future development sessions do not have to reconstruct that reasoning by searching file after file.

This document is a recovery point.

When returning to the project after a break, read this entry before changing architecture.

# 2. The Larger Mission

Scout is not simply a file-renaming application.

The long-term architecture is intended to make Scout a general-purpose system that can:

understand a user's project,
discover useful findings through domain Experts,
present those findings structurally,
discuss those findings conversationally,
create plans,
obtain appropriate approval,
execute approved operations through a controlled execution architecture,
report results,
maintain state throughout the user's expedition,
and eventually help create new domain Experts.

The current work therefore has several interconnected goals.

# 3. Current Major Development Tracks

The project currently contains several active architectural tracks.

Track A — New Execution Architecture

The application is being migrated from the old execution system to the new execution architecture.

The intended flow is:

Observation
    ↓
Recommendation
    ↓
Plan
    ↓
Preview
    ↓
Approval
    ↓
New Execution
    ↓
Result

The migration strategy is deliberate.

We are NOT deleting the old execution code while migrating.

Instead:

OLD CODE
   │
   │ remains temporarily
   │
   ▼
NEW CODE
   │
   │ becomes authoritative
   ▼
PROVE ALL ROUTES
   │
   ▼
REMOVE OLD CODE

This allows each path to be migrated and tested independently.

The old implementation is not considered acceptable simply because the new implementation exists.

The new implementation must first become the path actually used by every relevant part of Scout.

Only after that has been demonstrated should the legacy implementation be removed.

# 4. The Two Conversation Engines

There are intentionally two Conversation Engines.

They are not accidental duplicates and should not be merged merely because they have similar responsibilities.

They serve different presentations of the same Scout expedition.

## 4.1 Structured / Workspace Conversation Engine

One engine provides information needed by Scout's structured user interface.

Its information supports things such as:

observations displayed in the Workspace,
buttons and navigation,
Review All,
evidence,
facts,
structured findings,
links back to underlying discoveries,
and information required by the user when they already understand what they want to do and how to get there.

This engine is concerned primarily with structured presentation and navigation.

## 4.2 Conversational Conversation Engine

The second engine provides information for Scout's actual dialogue with the user.

It supports:

conversational discussion,
questions,
explanations,
contextual reasoning,
following up on findings,
discussing recommendations,
understanding what the user wants,
and keeping the conversation aligned with the user's current activity.

This engine is concerned primarily with dialogue.

# 5. The Two Engines Are Two Views of One Expedition

The engines should not independently own the user's overall expedition.

Conceptually:

                         EXPEDITION STATE
                                │
                 ┌──────────────┴──────────────┐
                 │                             │
                 ▼                             ▼
       STRUCTURED ENGINE              CONVERSATIONAL ENGINE
                 │                             │
                 ▼                             ▼
       Workspace / Review All              Scout dialogue
                 │                             │
                 └──────────────┬──────────────┘
                                │
                                ▼
                               USER
                                │
                                ▼
                         USER'S ACTIONS
                                │
                                ▼
                         EXPEDITION STATE

The important architectural principle is:

The two Conversation Engines are consumers of the same expedition state; neither should become the sole owner of the expedition itself.

An external coordinating layer or program is intended to keep track of what the user is doing and provide appropriate state to both engines.

The two engines therefore need to remain synchronized without becoming one engine.

# 6. The Expedition State

The system needs a coherent concept of the user's current expedition.

Conceptually, this includes information such as:

Current Project
Current Folder
Current Findings
Current Recommendations
Current Selected Finding
Current Conversation Topic
User Intent
Current Plan
Current Approval State
Current Operation
Current Operation State

The exact implementation of this coordinating state still needs to be determined from the existing code.

Do not create a new giant coordinator simply because this document describes one conceptually.

First inspect the existing state ownership and determine what already exists.

The goal is to identify the proper existing owner or smallest appropriate missing abstraction.

# 7. Current State-Ownership Warning

During inspection of the current project, an important problem was identified.

There are currently multiple places creating a stateful CV_ConversationEngine.

For example, MainWindowViewModel has its own:

private readonly CV_ConversationEngine conversationEngine = new();

while ProjectWorkspaceViewModel also owns another instance:

public CV_ConversationEngine ConversationEngine { get; }
    = new();

This means the application can currently have:

MainWindowViewModel
        │
        └── ConversationEngine A

and:

ProjectWorkspaceViewModel
        │
        └── ConversationEngine B

These are separate stateful objects.

This is a likely contributor to synchronization and Review All problems.

Important:

Do not immediately delete one.

First determine:

which engine instance should own which state,
which object should coordinate the engines,
which consumers currently depend on each instance,
whether the existing architecture already contains the intended coordinator,
and how state is supposed to travel between the structured and conversational systems.

This is an architectural investigation item, not a reason for a blind code replacement.

# 8. Identity Is a First-Class Architectural Requirement

An ExpertFinding represents an actual discovery.

That discovery may subsequently be represented in several different forms.

The intended identity chain is:

ExpertFinding
      │
      │ same identity
      ▼
ProjectObservation
      │
      │ same identity
      ▼
CV_Recommendation
      │
      │ same identity
      ▼
CV_ReviewAllItem

These are different representations of the same underlying discovery.

They must not accidentally become separate discoveries.

The ExpertFinding owns the original identity:

public Guid Id { get; init; } = Guid.NewGuid();

The translator must preserve it.

For example:

Id = finding.Id

and must not generate another Guid during translation.

# 9. Why Review All Has an Identity Problem

The identity problem is more subtle than simply "Review All has the wrong Guid."

The current architecture contains a potential many-to-one transformation.

For example, multiple ExpertFindings may have the same summary:

Finding A → "Missing metadata"
Finding B → "Missing metadata"
Finding C → "Missing metadata"

If the Observation mapping groups findings by summary, the resulting Workspace representation may become:

one ProjectObservation

while the Conversation Framework may still contain:

three CV_Recommendations

Therefore, the representations may no longer be one-to-one.

This must be resolved deliberately.

The fundamental question is:

What constitutes the identity of a discovery, and how does that identity survive every representation of that discovery?

The answer must not simply be based on display text such as Summary.

# 10. Identity Rule

The following principle should govern future development:

Display text is not identity.

These are not reliable identity mechanisms:

Summary
Title
Question
Filename
Position in list
Index

The stable identity of the discovery must travel through the system.

The intended relationship is:

Discovery Identity
       ↓
all representations

rather than:

Representation
       ↓
new identity
# 11. Expert Architecture

The Expert system is being developed separately from the Conversation Framework.

The Expert owns domain knowledge.

Scout owns the conversation.

Conceptually:

DOMAIN EXPERT
    │
    ├── Investigations
    ├── Specialists
    ├── Domain analysis
    ├── Evidence
    └── ExpertFindings
             │
             ▼
      Recommendation Translator
             │
             ▼
      CV_Recommendation
             │
             ▼
      Conversation Framework

The Expert should not become responsible for:

deciding what Scout says next,
rendering UI,
managing the conversation,
executing operations,
or owning the user's expedition.
# 12. Ebook Expert Is the Reference Expert

EbookExpert is currently being developed as the reference implementation.

The goal is not merely to make Ebook Expert work.

The goal is to make it the model for future Experts.

It should demonstrate:

Expert structure,
domain specialists,
investigations,
evidence gathering,
findings,
recommendation translation,
domain-specific terminology,
independent operation,
and the correct boundary between Expert and Scout.

Once Ebook Expert is complete, its architecture becomes the pattern against which future Experts should be evaluated.

# 13. Specialists Are Domain Specialists

The Expert architecture was deliberately changed from narrowly scoped "specialists" toward domain specialists.

A domain specialist owns the functions necessary for its portion of the domain.

The intended model is:

Ebook Expert
   │
   ├── Domain Specialist A
   │      ├── relevant functions
   │      └── relevant knowledge
   │
   ├── Domain Specialist B
   │      ├── relevant functions
   │      └── relevant knowledge
   │
   └── Domain Specialist C
          ├── relevant functions
          └── relevant knowledge

This should become the model for future Experts.

# 14. Ebook Expert → Music Expert → Expert Template

The intended progression is:

Complete Ebook Expert
        ↓
Use Ebook Expert as reference
        ↓
Build Music Expert
        ↓
Compare what transfers cleanly
        ↓
Identify genuine reusable architecture
        ↓
Create Expert template / SDK
        ↓
Build future Experts

Music Expert should therefore not be used to prematurely invent the architecture.

Ebook Expert comes first.

Music Expert validates whether the Ebook architecture is genuinely reusable.

# 15. Future Expert Builder

After the Expert pattern has been proven, Scout should eventually be able to help create new Experts.

The intended concept is:

Human Domain Knowledge
        ↓
Scout interview
        ↓
Structured domain knowledge
        ↓
Expert Construction Document
        ↓
Expert Factory
        ↓
Standalone Expert

The goal is not for Scout to invent domain knowledge.

The domain expert supplies the knowledge.

Scout supplies the structured process and architecture.

# 16. Current Development Priority

The immediate priority is not to clean up the entire codebase.

The immediate priority is to make the architecture work.

The order is:

1. Understand current state ownership
2. Establish correct data flow
3. Fix the new path
4. Migrate consumers to the new path
5. Prove the new path works
6. Finish Ebook Expert
7. Use Ebook Expert as the reference
8. Build Music Expert
9. Generalize the Expert architecture
10. Build future Experts / Expert Builder
11. Remove legacy code only after migration is proven
# 17. New Development Rule: Stop Rummaging

Future work should not require the developer to search randomly through dozens of files looking for clues.

When a problem is identified:

First

Inspect the project as a system.

Trace:

INPUT
 ↓
OWNER
 ↓
TRANSFORMATION
 ↓
OUTPUT
 ↓
CONSUMER
Then

Identify the smallest vertical slice that can prove the architecture.

Then

Make one controlled change.

Then

Build.

Then

Test the result.

Then

Continue.

# 18. Required Data-Flow Map

Before major architectural changes, construct an actual map of the current implementation:

Folder Selection
      ↓
ProjectContext
      ↓
WorkflowResult
      ↓
ExpertFindings
      ↓
ProjectObservations
      ↓
CV_Recommendations
      ↓
Structured Conversation Engine
      ↓
Conversational Conversation Engine
      ↓
Workspace
      ↓
Guide
      ↓
User Intent
      ↓
Plan
      ↓
Preview
      ↓
Approval
      ↓
New Executor
      ↓
Execution Result

For every transition, identify:

Owner
Input
Output
State
Who creates it
Who modifies it
Who consumes it
Identity
Legacy or new path

This map should be created from the actual code.

It should not be based on assumptions.

# 19. Vertical-Slice Rule

Whenever possible, work through one complete vertical slice rather than changing an entire layer at once.

For example:

ExpertFinding
    ↓
Translator
    ↓
CV_Recommendation
    ↓
Conversation Engine
    ↓
Workspace
    ↓
Review All

Make that path correct.

Build it.

Test it.

Then move to the next path.

This gives us demonstrable progress instead of accumulating partially completed architectural changes.

# 20. What We Are NOT Doing

Until the architecture has been proven, do not:

Do not:
delete legacy systems simply because they appear obsolete,
merge the two Conversation Engines,
create duplicate identity systems,
generate new IDs during translation,
move code merely because a class "looks like it belongs somewhere else,"
replace working architecture with speculative abstractions,
create a giant new coordinator without tracing existing state ownership,
refactor unrelated code while fixing a specific vertical slice,
or assume similar class names mean two classes have the same responsibility.
# 21. Migration Rule

The migration is complete only when we can demonstrate:

EVERY RELEVANT USER PATH
          ↓
USES NEW CODE
          ↓
PRODUCES CORRECT RESULT
          ↓
OLD CODE IS NO LONGER CALLED

Only then:

REMOVE LEGACY CODE

The presence of old code is therefore not currently considered a defect by itself.

Unused legacy code is technical debt.

Actively competing implementations are an architectural defect.

The immediate job is to establish which is which.

# 22. Current Architectural Smoking Guns

The following are current investigation targets.

Smoking Gun #1 — Multiple Conversation Engine Instances

Current code contains separate CV_ConversationEngine instances in different ViewModels.

Question to resolve:

Who should own the authoritative Conversation Framework state?

Smoking Gun #2 — Review All Identity

The intended identity chain is:

ExpertFinding.Id
      =
ProjectObservation.Id
      =
CV_Recommendation.Id
      =
CV_ReviewAllItem.Id

But current mapping behavior may collapse findings based on summary.

Question to resolve:

How do we preserve discovery identity through many-to-one presentation mappings?

Smoking Gun #3 — Two Presentation Systems

Workspace and conversational dialogue consume related information but currently have separate state paths.

Question to resolve:

What is the correct synchronization mechanism between the structured and conversational views of the expedition?

Smoking Gun #4 — Legacy/New Execution Coexistence

Old and new execution paths exist simultaneously.

Question to resolve:

Which user-facing paths have already migrated to the new execution system, and which still call legacy execution?

Smoking Gun #5 — State Ownership Is Distributed

State currently appears across several ViewModels and services.

Question to resolve:

Which object is actually responsible for expedition state, and which objects are merely views/controllers/consumers?

# 23. What Success Looks Like

We should stop measuring progress primarily by the number of files changed.

Instead, progress should be measured by demonstrated behavior.

For example:

Milestone 1
ExpertFinding
     ↓
CV_Recommendation

Identity and evidence survive translation.

Milestone 2
CV_Recommendation
     ↓
Conversation Engine

Scout can discuss it.

Milestone 3
CV_Recommendation
     ↓
Workspace

The corresponding structured observation appears correctly.

Milestone 4
CV_Recommendation
     ↓
Review All

Review All reports the correct discovery.

Milestone 5
User action
     ↓
Expedition state
     ↓
both engines

The structured and conversational systems remain synchronized.

Milestone 6
Plan
 ↓
Preview
 ↓
Approval
 ↓
New Execution

The actual operation uses the new execution architecture.

Milestone 7
Ebook Expert

works independently as the reference Expert.

Milestone 8
Music Expert

can be built using the same architecture without inventing a second Expert architecture.

# 24. The Development Philosophy Going Forward

The project is no longer primarily in the "add features" phase.

It is in a convergence phase.

We have accumulated enough architecture that the next goal is to make the pieces agree about:

identity,
ownership,
state,
data flow,
execution,
and responsibility.

The correct question when encountering confusing code is therefore not:

"What should this class do?"

It is:

"What role does this piece play in the complete Scout expedition, and who owns the state that it is using?"

That question should guide future architectural decisions.

# 25. Recovery Procedure for Future Sessions

When returning to this project after a break:

## Step 1 — Read this journal entry.

Do not immediately edit code.

## Step 2 — Read the current project documentation.

Especially:

Architecture
ADRs
EDK / Expert documentation
Development Protocol
Progress
## Step 3 — Determine current milestone.

Ask:

What was the last vertical slice that actually worked?
## Step 4 — Inspect the current project.

Do not rely on memory.

## Step 5 — Rebuild the relevant data-flow path.
Input
 ↓
Owner
 ↓
Transformation
 ↓
Output
 ↓
Consumer
## Step 6 — Identify whether the problem is:
BUG
ARCHITECTURAL GAP
MIGRATION GAP
STATE OWNERSHIP PROBLEM
IDENTITY PROBLEM
LEGACY PATH
OR
MISSING FEATURE
Step 7 — Make one controlled change.
Step 8 — Build.
Step 9 — Demonstrate the result.
Step 10 — Update this journal.
26. Current Point of Departure

At this checkpoint, the most important unresolved architectural questions are:

Who owns the authoritative expedition state?
How do the structured and conversational Conversation Engines receive synchronized state?
Should there be one shared CV_ConversationEngine instance, or should the engines be separated from the state they currently own?
How does discovery identity survive the Observation → Conversation → Review All pipeline?
Which current user paths still use legacy execution?
Which paths have already migrated to the new execution architecture?
What remains necessary to make Ebook Expert a truly independent reference Expert?
What architecture should become the reusable Expert template after Ebook Expert is proven?

These questions should be answered from the actual codebase before making large architectural changes.

# 27. The Ultimate Architecture

The long-term picture is:

                         ┌──────────────────────┐
                         │        SCOUT         │
                         │                      │
                         │  Expedition State    │
                         │  User Intent         │
                         │  Coordination        │
                         └──────────┬───────────┘
                                    │
                   ┌────────────────┴────────────────┐
                   │                                 │
                   ▼                                 ▼
          ┌─────────────────┐              ┌─────────────────┐
          │ STRUCTURED      │              │ CONVERSATIONAL  │
          │ CONVERSATION    │              │ CONVERSATION    │
          │                 │              │                 │
          │ Workspace       │              │ Dialogue        │
          │ Review All      │              │ Questions       │
          │ Evidence        │              │ Discussion      │
          │ Navigation      │              │ Intent          │
          └────────┬────────┘              └────────┬────────┘
                   │                                │
                   └──────────────┬─────────────────┘
                                  │
                                  ▼
                             USER ACTION
                                  │
                                  ▼
                            PLAN / APPROVAL
                                  │
                                  ▼
                         NEW EXECUTION SYSTEM
                                  │
                                  ▼
                               RESULT
                                  │
                                  ▼
                           EXPEDITION STATE

And behind the findings:

                       EXPERT SYSTEM
                            │
             ┌──────────────┴──────────────┐
             │                             │
       Ebook Expert                  Future Experts
             │                             │
       Reference Model                Music Expert
             │                             │
             └──────────────┬──────────────┘
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
                  Conversation Framework

The ultimate objective is:

Ebook Expert
      ↓
proves Expert architecture
      ↓
Music Expert
      ↓
proves reuse
      ↓
Expert Template / SDK
      ↓
future Experts
      ↓
Scout Expert Builder
## 28. Final Guiding Principle

We are not trying to make the code look finished.

We are trying to make the architecture true.

    First:

Make the new architecture work.

    Then:

Make every consumer use it.

    Then:

Prove the complete system.

    Then:

Remove the old architecture.

And throughout the process:

Experts provide domain knowledge.
Conversation Engines provide different views of the expedition.
Expedition state keeps those views synchronized.
Identity follows the discovery.
The new execution path becomes authoritative.
Ebook Expert becomes the reference model.
Music Expert proves the model can be reused.
Only then do we generalize and automate Expert creation.

This is the architectural direction from which future development should resume.

## The file documentation explains what the code is doing.

The Development Journal explains why we chose to do it.

So we maintain both:

      CODE FILE
         ↓
    What / responsibility / boundaries / temporary status


    DEVELOPMENT JOURNAL
          ↓
    Why / architectural decision / problem / result / next step

That gives future us two levels of recovery.