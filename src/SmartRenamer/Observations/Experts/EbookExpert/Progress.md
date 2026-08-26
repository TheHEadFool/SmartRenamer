# Ebook Expert Progress

Last Updated: 2026-08-22

---

## Purpose

The Ebook Expert is Scout's reference implementation of a complete domain
Expert.

It is being developed as the model for future Experts and will eventually
serve as the template from which additional domain Experts can be built.

The live project is the source of truth.

This document records the actual implementation state of the Ebook Expert,
the current architectural boundaries, known limitations, and the next
development milestone.

---

# 1. Core Architecture

The Ebook Expert follows the Investigation-centered architecture:

Files
  ↓
Ebook Expert
  ↓
Investigation
  ↓
Block
  ↓
Report
  ↓
Consultant
  ↓
ExpertFinding
  ↓
Ebook Expert
  ↓
Scout

Each Investigation answers a research question.

Blocks acquire facts.

Reports preserve objective research.

Consultants interpret Reports.

ExpertFindings communicate discoveries to the rest of the Scout system.

The Ebook Expert coordinates the domain-specific Investigations.

---

# 2. Shared Metadata Research

Metadata is the foundational research source for the Ebook Expert.

The current architecture is:

Files
  ↓
E_MetadataBlock
  ↓
MetadataReport
  ├── E_MetadataConsultant
  │       ↓
  │   Metadata ExpertFindings
  │
  ├── Contents Investigation
  ├── Organization Investigation
  ├── Quality Investigation
  ├── Repair Investigation
  └── Enrichment Investigation

Metadata is acquired once.

Downstream Investigations consume the existing MetadataReport rather than
re-reading EPUB files or reacquiring the same metadata.

This is an intentional architectural decision.

---

# 3. Investigation Status

## Metadata — 🟢 Implemented

Current implementation:

- E_MetadataBlock
- MetadataReport
- E_MetadataInvestigation
- E_MetadataConsultant
- ExpertFinding generation
- MetadataReport retained as the shared upstream research source

The Metadata Investigation now:

1. Acquires metadata through E_MetadataBlock.
2. Produces MetadataReport.
3. Passes the report to E_MetadataConsultant.
4. Preserves the resulting ExpertFindings.
5. Returns the MetadataReport for downstream Investigations.

The earlier transitional TODO concerning MetadataReport population and the
absence of a Metadata Consultant is no longer current.

### Remaining Metadata work

- Verify the resulting Metadata findings in the running UI.
- Verify recommendation translation.
- Complete eventual migration away from the legacy
  E_EbookMetadataSpecialist.

Do not redesign the Metadata architecture unless runtime verification
reveals a real problem.

---

## Contents — 🟢 Investigation Implemented

Current implementation:

- Contents Block
- Contents Report
- TableOfContentsConsultant
- E_ContentsInvestigation
- ExpertFinding generation

The Investigation consumes the shared MetadataReport.

### Remaining work

Increase EPUB content intelligence when the active vertical slice requires it.

Do not expand Contents merely for architectural completeness.

---

## Organization — 🟢 Investigation Implemented

Current implementation:

- Organization Block
- Organization Report
- Organization Evidence
- E_OrganizationConsultant
- E_OrganizationInvestigation
- ExpertFinding generation

The Investigation consumes the shared MetadataReport.

### Remaining work

Evaluate the usefulness and depth of organization findings after the active
Repair work is proven.

---

## Duplicates — 🟢 Investigation Implemented

Current implementation:

- E_DuplicateBlock
- E_DuplicateReport
- E_DuplicateConsultant
- E_DuplicateInvestigation
- ExpertFinding generation

Duplicate analysis currently operates from the file collection rather than
the MetadataReport.

This is acceptable where duplicate analysis requires information that is
not part of the shared metadata research.

### Remaining work

Improve duplicate intelligence only when required by the active roadmap.

---

## Quality — 🟢 Investigation Implemented

Current implementation:

- E_QualityBlock
- E_QualityReport
- E_QualityConsultant
- E_QualityInvestigation
- ExpertFinding generation

The Quality Consultant currently interprets quality observations including
metadata completeness and cover-related conditions.

### Remaining work

Increase EPUB quality analysis depth later.

Do not interrupt the Repair vertical slice for additional Quality features.

---

## Repair — 🟡 Investigation Implemented; Action Capability Incomplete

Current implementation:

- RepairBlock
- RepairReport
- RepairOpportunity
- RepairRecommendation
- E_RepairConsultant
- E_RepairInvestigation
- ExpertFinding generation

The current Repair flow is:

MetadataReport
  ↓
RepairBlock
  ↓
RepairReport
  ↓
E_RepairConsultant
  ↓
ExpertFindings

RepairReport currently preserves:

- Repair Opportunities
- Missing metadata counts
- Repairable book count
- Evidence lists

RepairOpportunity represents the facts for one affected ebook.

RepairRecommendation represents a possible action and includes:

- The underlying RepairOpportunity
- Description
- RequiresResearch
- IsSafeToApply

These models are Ebook Expert domain objects.

They must remain inside the Ebook Expert.

### Current limitation

Repair currently identifies repair opportunities but does not yet perform
repairs.

The Consultant currently reports findings but does not yet turn those
findings into a complete actionable repair workflow.

### Immediate Repair Goal

Build the first complete Repair vertical slice:

Missing ISBN
  ↓
Repair Opportunity
  ↓
Determine whether information can be recovered
  ↓
Research missing information when appropriate
  ↓
Present candidate information and evidence
  ↓
User approval
  ↓
Prepare EPUB repair
  ↓
Apply repair
  ↓
Re-read EPUB
  ↓
Verify repair
  ↓
Report result to Scout

This is the current primary development milestone.

### Important Boundary

Scout must not learn EPUB-specific repair rules.

The Ebook Expert owns:

- EPUB structure
- Metadata semantics
- Research questions specific to ebooks
- Metadata validation
- EPUB modification
- EPUB repair verification

Scout owns conversation, user intent, approval, and general expedition
coordination.

---

## Enrichment — 🟢 Investigation Implemented

Current implementation:

- EnrichmentBlock
- EnrichmentReport
- E_EnrichmentConsultant
- E_EnrichmentInvestigation
- ExpertFinding generation

Enrichment identifies information that could improve an ebook collection.

Examples include:

- Series
- Description
- Cover
- Publisher
- Language

Enrichment does not:

- Search external services directly
- Download metadata
- Modify EPUB files
- Make recommendations
- Communicate with Scout

### Architectural relationship to Repair

Repair and Enrichment may identify some of the same missing information,
but they answer different questions.

Repair asks:

> What is missing or damaged, and can it be safely restored?

Enrichment asks:

> What additional information could improve the ebook?

Research capabilities may eventually serve both Investigations, but the
domain responsibility of Repair and Enrichment must remain separate.

---

# 4. Recommendation Pipeline

The current Expert-driven recommendation path is:

ExpertFinding
  ↓
E_RecommendationTranslator
  ↓
CV_Recommendation
  ↓
Conversation Framework

The translator preserves the finding identity and carries the finding's
available evidence and questions into the conversational recommendation.

This is an implemented vertical slice.

### Current limitation

Recommendation selection is still intentionally simplified.

The current Conversation Framework can use the first recommendation rather
than performing sophisticated ranking or prioritization.

Do not introduce a new recommendation-ranking architecture yet.

Recommendation selection should be improved when the Conversation Framework
is ready for that responsibility.

---

# 5. Conversation Integration

The Expert-driven Conversation path currently supports:

ExpertFinding
  ↓
CV_Recommendation
  ↓
CV_ConversationEngine
  ↓
CV_CurrentTopic
  ↓
Workspace / Conversation

The Workspace continues to support the existing ProjectObservation path
during migration.

Both paths intentionally coexist while the Expert-driven pipeline is
being proven.

The ObservationMapper remains part of the compatibility path.

### Known limitation

The conversational experience is not yet a complete action-oriented repair
conversation.

The immediate goal is not to redesign Conversation.

The immediate goal is to give the existing Conversation Framework a real
repair capability to discuss.

---

# 6. Repair Research Architecture

The project already establishes a Research Assistant pattern.

An Investigation is a self-contained research assistant.

Research:

- acquires information
- preserves objective evidence
- produces research results

Research does not:

- interpret the results for the user
- make recommendations
- communicate directly with Scout

Consultants interpret research.

This architecture supports the planned Repair capability.

For example:

Missing ISBN
  ↓
Ebook Expert determines that research may be appropriate
  ↓
Available ebook metadata becomes research context
  ↓
Research assistant obtains candidate information
  ↓
Evidence is preserved
  ↓
Consultant evaluates the result
  ↓
Scout presents the proposed repair
  ↓
User approves
  ↓
Ebook Expert performs the repair

Research should be acquired once and reused where appropriate.

Do not create a second generic research architecture merely to support
Repair.

---

# 7. Ebook Metadata Reader

The project contains:

E_EbookMetadataReader

It currently reads EPUB metadata and embedded cover information.

The reader is responsible for extracting information from the EPUB.

It is not responsible for:

- deciding what should be repaired
- researching missing information
- recommending changes
- communicating with Scout

This boundary must remain intact.

---

# 8. Legacy Specialist Migration

The Ebook Expert still contains:

E_EbookMetadataSpecialist

This is legacy infrastructure.

The Generation 2 Investigation architecture is now substantially established,
but the legacy specialist must not be removed merely because it appears
obsolete.

Migration is complete only when:

Every relevant user path
  ↓
uses the new architecture
  ↓
produces the correct result
  ↓
no longer calls the legacy path

Until then, the legacy Specialist remains technical debt rather than an
immediate deletion target.

### Remaining migration task

Verify that the Generation 2 Metadata Investigation completely replaces every
required responsibility of E_EbookMetadataSpecialist.

Then remove the legacy path deliberately.

---

# 9. Ebook Expert Completion Definition

An Investigation is considered complete when it has:

✔ Block  
✔ Report  
✔ Evidence where required  
✔ Consultant  
✔ Investigation  
✔ ExpertFinding generation  
✔ Green Build

The Ebook Expert itself is considered reference-ready only when its major
Investigation paths have been demonstrated behaviorally, not merely created
as classes.

Repair therefore remains a major incomplete capability even though its
Investigation architecture exists.

---

# 10. Current Primary Milestone

## Make Scout Able to Fix Something

The next meaningful milestone is not another architectural model.

It is a demonstrated repair.

### First repair:

Missing ISBN.

### Success criteria

Given an EPUB with missing ISBN metadata:

1. The Ebook Expert detects the missing ISBN.
2. RepairOpportunity identifies the affected ebook.
3. Repair logic determines whether recovery is possible.
4. Existing metadata is used as research context.
5. Research can obtain candidate information when appropriate.
6. Evidence supporting the candidate is preserved.
7. Scout can present the candidate to the user.
8. The user can approve or reject the proposed change.
9. Ebook-specific repair code writes the approved ISBN.
10. The EPUB is read again.
11. The resulting metadata is verified.
12. Scout reports the actual result.

This will establish the pattern for future repairs.

---

# 11. Future Repair Capabilities

Once the ISBN vertical slice works, apply the same architecture to:

- Missing titles
- Missing authors
- Missing publishers
- Missing languages
- Missing descriptions
- Missing covers
- Navigation repair
- Other safely recoverable EPUB defects

Each capability must remain inside the Ebook Expert.

Do not generalize EPUB repair logic into Scout.

---

# 12. Backlog and Parking Lot

The project contains a substantial backlog and parking lot including:

- Drag and drop
- Recent folders
- Favorites
- Keyboard shortcuts
- Music Expert
- Photo Expert
- Movie/document intelligence
- STL support
- Cockatrice support
- Folder watching
- Scheduled organization
- Semantic duplicate detection
- Suggested folder names
- Metadata cleanup
- Online cover lookup
- Library health
- Virtual collections
- Additional Expert automation

These remain valid future work.

They should not interrupt completion of the current Ebook Expert repair
vertical slice.

The Ebook Expert is still the reference implementation.

---

# 13. Architecture Migration

Outstanding migration work includes:

- Ebook legacy Specialist migration
- Music Specialist migration
- Photo Specialist migration
- eventual removal of obsolete global Specialist dependencies

Migration should follow the established rule:

Do not delete working legacy code simply because newer code exists.

First prove the new path.

Then remove the old path.

---

# 14. Known Architectural Investigation Areas

These are important, but they are not the immediate Repair milestone.

## Conversation State Ownership

There are multiple Conversation Engine instances and state holders.

Eventually determine the authoritative owner of Conversation Framework state.

## Discovery Identity

The intended identity chain is:

ExpertFinding.Id
  =
ProjectObservation.Id
  =
CV_Recommendation.Id
  =
CV_ReviewAllItem.Id

Verify that identity survives all translations and presentation paths.

## Structured vs Conversational Views

Workspace and Conversation consume related expedition information through
different paths.

Eventually establish the correct synchronization mechanism.

## Legacy/New Execution

Both legacy and newer execution infrastructure remain in the project.

Determine which user-facing paths have migrated before removing legacy execution.

## Distributed State

State exists across ViewModels and services.

Future architectural work should explicitly identify state ownership rather
than introducing additional coordinators without need.

These are architectural investigation targets, not reasons to interrupt the
current Repair milestone.

---

# 15. Development Rules

The project follows:

One file.
One logical change.
Build immediately.
Green before continuing.

The current project is the source of truth.

Do not rely on obsolete progress notes when the live code provides different
information.

Do not add architecture merely because a future feature may eventually need
it.

Prefer completing a working vertical slice over accumulating infrastructure.

Do not refactor unrelated systems while implementing a specific capability.

Do not move code merely because a class appears to belong somewhere else.

Respect domain ownership.

Ebook-specific knowledge belongs in Ebook Expert.

Generic Scout infrastructure must remain domain-neutral.

---

# 16. What We Measure

Progress is measured by demonstrated behavior rather than number of files
created.

A meaningful milestone looks like:

Expert Finding
  ↓
Recommendation
  ↓
Conversation
  ↓
User Action
  ↓
Domain Operation
  ↓
Verification
  ↓
Result

The first complete demonstration will be:

Missing ISBN
  ↓
Research
  ↓
Approval
  ↓
EPUB Repair
  ↓
Verification

---

# 17. Current Development Position

The project has moved beyond building the basic Investigation architecture.

The major Ebook Expert Investigation paths now exist.

The next phase is **capability completion**.

The immediate objective is:

> Make the Ebook Expert capable of safely repairing a real problem it
> discovered.

The first target is missing ISBN metadata.

After that capability is proven, the repair architecture can be extended
to additional metadata and EPUB repair problems.

---

# 18. Recovery Procedure for Future Sessions

When returning to the project:

## Step 1

Read this document.

## Step 2

Read the current project documentation and relevant ADRs.

## Step 3

Inspect the live project.

The codebase is authoritative.

## Step 4

Identify the last demonstrated vertical slice.

Do not assume that the presence of a class means its behavior is complete.

## Step 5

Trace the relevant data flow:

Input
  ↓
Owner
  ↓
Research
  ↓
Transformation
  ↓
Output
  ↓
Consumer
  ↓
User action
  ↓
Result

## Step 6

Classify the problem:

- Bug
- Missing capability
- Architectural gap
- Migration gap
- State ownership problem
- Identity problem
- Legacy path
- Documentation problem

## Step 7

Make one controlled change.

## Step 8

Build.

## Step 9

Demonstrate the behavior.

## Step 10

Update this document.

---

# 19. Reference Expert Goal

The ultimate purpose of the Ebook Expert remains unchanged.

Ebook Expert
  ↓
Prove the Expert architecture
  ↓
Music Expert
  ↓
Prove architectural reuse
  ↓
Future domain Experts
  ↓
Reusable Expert construction model
  ↓
Scout Expert Builder

The Ebook Expert must therefore demonstrate more than an attractive
architecture.

It must demonstrate that an Expert can:

- research its domain
- preserve objective evidence
- interpret findings
- communicate findings
- recommend appropriate actions
- obtain additional information when necessary
- perform domain-specific operations safely
- verify the result
- report the outcome back to Scout

That is the standard against which the next phase of development should be
measured.

---

# 20. Immediate Next Step

**Do not create another report property.**

**Do not redesign the Conversation Framework.**

**Do not begin another Expert.**

**Do not migrate the legacy Specialist yet.**

Complete the first real repair:

## Missing ISBN → Research → User Approval → EPUB Repair → Verification

This is the next vertical slice from which the remaining repair capabilities
should be developed.

---

# Final Guiding Principle

We are not trying to make the code look finished.

We are trying to make the architecture true.

First:

Make the new architecture work.

Then:

Make the user-facing path use it.

Then:

Prove the complete operation.

Then:

Remove the old architecture.

Throughout the process:

Experts provide domain knowledge.

Investigations answer research questions.

Blocks acquire facts.

Reports preserve objective research.

Consultants interpret research.

ExpertFindings communicate discoveries.

Conversation provides the user-facing discussion.

User approval controls consequential actions.

Domain Experts perform domain-specific operations.

Verification proves that the operation actually succeeded.

The Ebook Expert becomes the reference model.

Only after that model is proven should it be generalized to other Experts.