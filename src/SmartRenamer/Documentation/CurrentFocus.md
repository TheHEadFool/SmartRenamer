# Current Sprint

Last Updated:
2026-09-12

----------------------------------------------------
Current Goal
----------------------------------------------------

Map and complete the Repair → Organization boundary.

The Missing ISBN repair slice is now demonstrated through creation of a
repaired working copy in the temporary workspace. Organization is not yet
executing the final organized copy, so the current workflow stops there.

The next milestone is a single-ebook Organization vertical slice:

Repair Handoff
    ↓
Organization Context
    ↓
Organization Decision
    ↓
Organization Job / Operation
    ↓
Copy to organized destination
    ↓
Verify
    ↓
Release working copy

----------------------------------------------------
Current Architecture
----------------------------------------------------

Observation
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
Recommendation
    ↓
Conversation
    ↓
User Action
    ↓
Domain Action
    ↓
Repair / Organization

MetadataReport is the shared collection-wide metadata source for downstream
Ebook Expert Investigations.

Repair keeps OriginalFullPath as the stable source identity and may place a
repaired representation at CurrentFullPath. Originals remain protected.

----------------------------------------------------
Completed / Demonstrated
----------------------------------------------------

✔ Ebook Expert Investigation architecture

✔ Metadata
    - Block
    - Report
    - Investigation
    - Consultant
    - Shared MetadataReport

✔ Contents
    - Block
    - Report
    - Investigation
    - Consultant

✔ Organization Investigation
    - Block
    - Report
    - Evidence
    - Consultant
    - Investigation

✔ Duplicates
    - Block
    - Report
    - Investigation
    - Consultant

✔ Quality
    - Block
    - Report
    - Investigation
    - Consultant

✔ Enrichment
    - Block
    - Report
    - Investigation
    - Consultant

✔ Repair Investigation
    - RepairBlock
    - RepairReport
    - RepairOpportunity
    - RepairRecommendation
    - E_RepairConsultant
    - E_RepairInvestigation

✔ Missing ISBN action path
    - Research through Open Library
    - Candidate presentation
    - User selection / approval
    - Repair plan
    - Working-copy creation
    - EPUB ISBN modification

✔ Conversation action options are visibly clickable.

✔ Repair actions can return a re-observation requirement.

----------------------------------------------------
Current Repair State
----------------------------------------------------

The Missing ISBN vertical slice is operational through the repaired working
copy. The repaired EPUB is deliberately placed in the temporary workspace.

The current endpoint is:

Original EPUB
    ↓
Research ISBN
    ↓
User selects candidate
    ↓
Repair Plan
    ↓
Create protected working copy
    ↓
Apply ISBN repair
    ↓
Repaired EPUB in Temp

The workflow does not yet have a completed Repair Handoff → Organization →
organized destination path. That is the next architectural connection.

Verification should continue to use the normal ObservationEngine/re-observation
path rather than creating a separate verification architecture.

----------------------------------------------------
Organization State
----------------------------------------------------

Organization Investigation is implemented and can analyze collection metadata.
It can identify available organization dimensions and collection relationships.

Existing domain pieces include:

- OrganizationBlock
- OrganizationReport
- OrganizationEvidence
- OrganizationContext
- OrganizationOptions
- E_OrganizationConsultant
- E_OrganizationInvestigation
- OrganizationOptionsCard

These pieces are not yet connected into the actual filesystem organization
operation.

The old Services/OrganizationPlanner.cs path is legacy generic infrastructure
and is not the new Ebook Expert organization architecture. Do not feed repaired
ebooks into it merely because it already copies files.

----------------------------------------------------
Next Task
----------------------------------------------------

Trace Organization forwards and backwards from the current project source.

Determine the smallest sequence needed to connect:

E_RepairHandoff
    ↓
OrganizationContext
    ↓
Organization decision
    ↓
Organization Job / Operation
    ↓
Safe copy
    ↓
Verification

The first implementation should prove one repaired EPUB can become one
organized EPUB without modifying the original.

Do not build collection-scale organization, bulk queues, or advanced conflict
resolution until this single-item path is proven.

----------------------------------------------------
Safety Rules
----------------------------------------------------

• Originals are never modified.
• Repair works on a protected working copy.
• Organization copies the working representation to a separate destination.
• Copy → verify → release temporary working copy is the initial safe model.
• Destructive move/delete behavior is a separate future capability.

----------------------------------------------------
Development Rules
----------------------------------------------------

• One file.
• One logical change.
• Build immediately.
• Green before continuing.
• The live project is the source of truth.
• Prefer complete-file replacement.
• Do not refactor unrelated systems.
• Trace the data before changing the design.

----------------------------------------------------
Reference Expert Goal
----------------------------------------------------

The Ebook Expert remains the reference implementation for future Scout Experts.

The reference path must demonstrate:

Research
    ↓
Interpretation
    ↓
Conversation
    ↓
Approval
    ↓
Domain Operation
    ↓
Verification
    ↓
Handoff / Completion

Only after the Ebook Expert proves this pattern should the architecture be
generalized to additional Experts.
