# Current Sprint

Last Updated:
2026-08-22

----------------------------------------------------
Current Goal
----------------------------------------------------

Make the Ebook Expert capable of actually fixing a problem it discovers.

The Ebook Expert is the reference implementation for future Scout Experts.

The next milestone is the first complete Repair vertical slice:

Missing ISBN
    ↓
Research
    ↓
User Approval
    ↓
EPUB Repair
    ↓
Verification

----------------------------------------------------
Current Architecture
----------------------------------------------------

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
Recommendation
    ↓
Conversation
    ↓
User Action
    ↓
Domain Operation
    ↓
Verification

Metadata research is acquired once and shared with downstream
Investigations through MetadataReport.

----------------------------------------------------
Completed
----------------------------------------------------

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

✔ Organization
    - Block
    - Report
    - Evidence
    - Investigation
    - Consultant

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

✔ Repair Investigation
    - RepairBlock
    - RepairReport
    - RepairOpportunity
    - RepairRecommendation
    - E_RepairConsultant
    - E_RepairInvestigation

✔ Enrichment
    - Block
    - Report
    - Investigation
    - Consultant

✔ Recommendation Pipeline
    - ExpertFinding
    - E_RecommendationTranslator
    - CV_Recommendation

✔ Conversation integration
    - Findings can become recommendations.
    - Recommendations can become conversation topics.
    - Review All can present recommendations.

----------------------------------------------------
Current Repair State
----------------------------------------------------

Repair can currently identify missing ebook information.

RepairReport preserves:

- Repair opportunities
- Missing metadata counts
- Collection statistics
- Evidence

RepairOpportunity identifies the specific missing information
for an ebook.

RepairRecommendation describes a possible repair and records:

- Description
- RequiresResearch
- IsSafeToApply

Repair does NOT yet perform the repair.

----------------------------------------------------
Next Task
----------------------------------------------------

Complete the first real Repair capability:

Missing ISBN.

The first implementation should:

1. Identify an ebook with a missing ISBN.

2. Use the existing MetadataRecord as the research context.

3. Determine whether the missing ISBN can reasonably be researched.

4. Produce a research request containing the available identifying
   information.

5. Obtain a candidate ISBN and supporting evidence.

6. Allow Scout to present the candidate to the user.

7. Require user approval before changing the EPUB.

8. Have the Ebook Expert perform the EPUB-specific repair.

9. Re-read the EPUB.

10. Verify that the ISBN was actually written correctly.

11. Report the result back through the existing Scout pipeline.

----------------------------------------------------
Research Rules
----------------------------------------------------

Research belongs to the domain Investigation that owns the
research question.

Research assistants:

• Acquire information.
• Preserve objective evidence.
• Do not interpret findings.
• Do not make recommendations.
• Do not communicate directly with Scout.

Consultants interpret research.

The Ebook Expert owns all EPUB-specific research and repair knowledge.

Scout must remain domain-neutral.

----------------------------------------------------
After First Repair Works
----------------------------------------------------

Extend the Repair pattern to:

- Missing titles
- Missing authors
- Missing publishers
- Missing languages
- Missing descriptions
- Missing covers
- Navigation repair
- Other safely recoverable EPUB defects

Do not build all repair types simultaneously.

Prove one complete vertical slice first.

----------------------------------------------------
Legacy Migration
----------------------------------------------------

E_EbookMetadataSpecialist remains legacy infrastructure.

Do not remove it until the new Metadata Investigation has been
verified to replace all required behavior.

Migration is a later task.

----------------------------------------------------
Known Future Work
----------------------------------------------------

Do not interrupt the current Repair milestone for:

- Recommendation ranking
- Conversation redesign
- New Experts
- Music migration
- Photo migration
- Drag and drop
- Favorites
- Recent folders
- Keyboard shortcuts
- Folder watching
- Scheduled organization
- Semantic duplicate improvements
- STL support
- Cockatrice support
- Other backlog or parking-lot features

These remain valid future work.

----------------------------------------------------
Do NOT Change
----------------------------------------------------

Architecture:

Files
    ↓
Block
    ↓
Report
    ↓
Consultant
    ↓
ExpertFinding

Research:

Research once.
Preserve objective evidence.
Reuse existing research downstream.

Domain ownership:

Ebook-specific knowledge remains inside Ebook Expert.

Scout owns:

- Conversation
- User intent
- User approval
- General coordination

Ebook Expert owns:

- EPUB knowledge
- EPUB research
- EPUB repair
- EPUB verification

Development rules:

• One file.
• One logical change.
• Build immediately.
• Green before continuing.

Whole file ≤150 lines.
Method only >150 lines.

The live project is the source of truth.

----------------------------------------------------
Definition of the Next Milestone
----------------------------------------------------

The Repair milestone is complete when Scout can demonstrate:

"I found an ebook missing an ISBN."

↓

"I researched the available information."

↓

"Here is the ISBN I found and the evidence supporting it."

↓

"Would you like me to add it?"

↓

User approves.

↓

Ebook Expert modifies the EPUB.

↓

Ebook Expert verifies the modification.

↓

Scout reports:

"The ISBN was successfully added and verified."

----------------------------------------------------
Current Focus
----------------------------------------------------

Do not add another architectural layer.

Do not recreate existing Report properties.

Do not redesign the Conversation Framework.

Do not start another Expert.

Make the Ebook Expert actually fix one real problem.

First:

Missing ISBN.

Then:

Research.

Then:

Approval.

Then:

Repair.

Then:

Verification.