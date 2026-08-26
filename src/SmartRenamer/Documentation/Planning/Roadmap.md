Phase 1

Workflow Engine

✅

Phase 2

Visual Workspace

🚧

Phase 3

Guide

Phase 4

Pipeline Packs

Phase 5

Community

Phase 6

Cloud Providers

Phase 7

Plugin SDK

# Version 1

## ✔ Completed

- Workflow Engine
- Guide Engine
- Conversation Panel
- Folder Summary

## 🚧 In Progress

- Folder Scanner

## Next

- Folder Picker
- Project Discovery
- Workflow Suggestions

Version 0.5

Finish Related Asset Organizer

DONE means:

Files with matching base names are grouped correctly.

----------------------------------------

Version 0.6

Capability Framework

DONE means:

Capabilities execute before planning.

----------------------------------------

Version 0.7

RelatedAssetCapability

DONE means:

Planner consumes RelatedGroup instead of discovering it.

----------------------------------------

Version 0.8

PhotoMetadataCapability

----------------------------------------

Version 0.9

MusicMetadataCapability

## Version 0.6 – Complete the Experience

### Goals

- [ ] Completion Card
- [ ] Open Organized Folder
- [ ] Open Parent Folder
- [ ] Improved progress reporting
- [ ] Better error messages
- [ ] More descriptive Suggested Actions

Success Criteria

A first-time user can successfully organize a folder without wondering what happened.

Version 0.2 — Custom Organization
Natural language organization
"Put every Monday into a folder named Monday."
"Separate Christmas music."
"Group by fiscal year."
"Group by school year."
"Put vacation photos together."
User-defined folder naming

Future Intelligence
Scout generates organization rules from conversation.
Scout explains why it recommends those rules.
Scout previews custom folder structures before execution.


I think every analyzer should produce the same output:

Analyzer
    ↓
Observations
    ↓
Recommendations
    ↓
Suggested Actions

That consistency is going to make Scout feel cohesive. Whether it's looking at a PDF, an MP3, a photo, or a 3D model, the rest of the application doesn't need to know the file type—it just receives a set of observations and suggested actions.

I also have one architectural goal

I want us to get to the point where adding a new analyzer feels almost routine:

Create a new analyzer class.
Register it.
Scout immediately starts using it.

If we achieve that, the project becomes highly extensible. New capabilities won't require touching the rest of the application—they'll just plug into the existing pipeline.

## Never solve a visual problem by adding complexity until you've checked whether changing the initial conditions solves it.

## 8/25/26

Step 1 — Open Library ISBN Research

E_IsbnResearchResource.cs

Step 2 — Repair domain orchestration

RepairRecommendation + new repair execution/service layer if needed

Step 3 — Research result → Scout

Candidate + evidence → ExpertFinding → conversation

Step 4 — Approval

YES and hotlink converge on the same domain operation.

Step 5 — EPUB repair

Already largely implemented in E_EpubRepairResource.

Step 6 — Verification

Re-read EPUB and verify the approved ISBN.

Step 7 — Make ISBN repair the reference pattern

Then systematically finish:

missing title
missing author
missing publisher
missing language
missing description
missing cover
navigation repair
enrichment/research capabilities
deeper duplicate intelligence
deeper quality intelligence
contents/resource repair
Step 8 — Only after that

Remove E_EbookMetadataSpecialist and other obsolete paths once behavioral parity is demonstrated.