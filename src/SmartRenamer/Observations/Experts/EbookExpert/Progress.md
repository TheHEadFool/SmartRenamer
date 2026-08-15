Metadata        ✅ Complete

Contents        ⬜ Skeleton

Organization    ⬜ Skeleton

Duplicates      ⬜ Skeleton

Quality         ⬜ Skeleton

Repair          ⬜ Skeleton

Enrichment      ⬜ Skeleton

# Architecture Migration

☐ Ebook Specialists moved into Expert

☐ Music Specialists moved into Expert

☐ Photo Specialists moved into Expert

☐ Global Specialists folder removed

# Ebook Expert Progress Last Updated: 2026-08-14
---------------------------------------------------- 
Purpose 
---------------------------------------------------- 
The Ebook Expert is Scout's reference implementation of a complete domain Expert. 
It is being developed as the model for future 
Experts and will eventually serve as a template that Scout 
can use when creating new domain Experts. The current project
is the source of truth. This document records the implementation
state of the Ebook Expert as it currently exists in the project. 
---------------------------------------------------- 
Architecture 
---------------------------------------------------- 
The Ebook Expert follows the Investigation-centered architecture: 

Scout 
↓ 
Expert 
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
Expert 
↓ 
Scout 

Each Investigation answers one research question. 
Blocks acquire facts. Reports preserve objective research. 
Consultants interpret Reports and produce ExpertFindings. 
The Expert coordinates the Investigations and combines their findings. 
---------------------------------------------------- 
Investigation Status 
---------------------------------------------------- 

Metadata 🟡 Foundation implemented; completion requires architectural review. 
Current implementation: 
• E_MetadataBlock exists. 
• E_MetadataInvestigation exists. 
• MetadataReport exists. 
• Metadata research is acquired once and passed downstream. Outstanding: 
• E_MetadataInvestigation still contains a TODO to populate MetadataReport 
from specialist findings. 
• Metadata currently has no Consultant. 
• The relationship between the transitional metadata specialist and the 
Generation 2 Metadata pipeline must be resolved. 
Do not assume Metadata is complete until this is resolved. 
---------------------------------------------------- 
Contents 🟢 Investigation implemented. Current implementation: 
• Contents Block 
• Contents Report 
• TableOfContentsConsultant 
• E_ContentsInvestigation 
• ExpertFinding generation The Investigation consumes MetadataReport 
rather than reacquiring metadata. 
---------------------------------------------------- 
Organization 🟢 Investigation implemented. 
Current implementation: 
• Organization Block 
• Organization Report 
• Organization Evidence 
• E_OrganizationConsultant 
• E_OrganizationInvestigation 
• ExpertFinding generation The Investigation consumes 
MetadataReport rather than reacquiring metadata. 
---------------------------------------------------- 
Duplicates 🟢 Investigation implemented. 
Current implementation: 
• E_DuplicateBlock 
• E_DuplicateReport 
• E_DuplicateConsultant 
• E_DuplicateInvestigation 
• ExpertFinding generation Duplicate research is currently performed
from the file collection rather than the MetadataReport. 
---------------------------------------------------- 
Quality 🟢 Investigation implemented. Current implementation: 
• E_QualityBlock 
• QualityReport 
• E_QualityConsultant 
• E_QualityInvestigation 
• ExpertFinding generation E_QualityConsultant currently interprets:
• NeedsAttention • MissingCovers 
---------------------------------------------------- 
Repair 🟢 Investigation implemented. Current implementation: 
• Repair Block 
• Repair Report 
• E_RepairConsultant 
• E_RepairInvestigation 
• ExpertFinding generation 
---------------------------------------------------- 
Enrichment 🟢 Investigation implemented. 
Current implementation: 
• Enrichment Block 
• Enrichment Report 
• E_EnrichmentConsultant 
• E_EnrichmentInvestigation 
• ExpertFinding generation The Investigation is currently coordinated by EbookExpert. 
---------------------------------------------------- 
EbookExpert Coordination 
---------------------------------------------------- 
EbookExpert currently coordinates: 
Metadata 
↓ 
MetadataReport 
↓ 
Contents Organization Quality Repair Enrichment And separately: 
Files 
↓ 
Duplicates The resulting ExpertFindings are combined by EbookExpert. 
The Expert also contains the recommendation translation path: 
ExpertFinding 
↓ 
E_RecommendationTranslator 
↓ 
CV_Recommendation 
---------------------------------------------------- 
Conversation Integration 
---------------------------------------------------- 
The Expert-driven Conversation path has been implemented as a vertical slice. 
The intended path is: 
ExpertFinding 
↓ 
CV_Recommendation 
↓ 
CV_ConversationEngine 
↓ 
CV_CurrentTopic 
↓ 
Workspace The current implementation intentionally uses the 
first recommendation through the existing Conversation selector. 
This is a known temporary limitation. 
Do not add ranking, prioritization, scoring, or conversational 
selection logic until the Conversation Framework is ready for that responsibility. 
---------------------------------------------------- 
Workspace Integration 
---------------------------------------------------- 
The existing Workspace continues to receive ProjectObservation objects. 
The new Expert-driven path produces CV_Recommendations for the Conversation Framework. 
Both paths are intentionally supported during migration. 
The ObservationMapper remains the compatibility bridge between the 
ExpertFinding model and the existing ProjectObservation UI model. 
The legacy Recommendation infrastructure must not be removed until 
the Expert-driven pipeline has been proven in the UI.
---------------------------------------------------- 
Legacy Specialist Migration 
---------------------------------------------------- 
The Ebook Expert still contains a legacy metadata specialist: 
E_EbookMetadataSpecialist This remains an outstanding migration item. 
The current Expert therefore contains both: Generation 2 Investigations 
and Legacy Specialist infrastructure Do not remove the legacy specialist 
until the Generation 2 metadata pipeline has been resolved and verified. 
--------------------------------------------------- 
Current Completion Definition 
----------------------------------------------------
An Investigation is considered complete when it has:
✔ Block 
✔ Report 
✔ Evidence where required 
✔ Consultant 
✔ Investigation 
✔ ExpertFinding generation 
✔ Green Build This definition follows the Expert Development Kit. 
Metadata currently requires additional work before it can be declared 
complete under this definition. 
---------------------------------------------------- 
Immediate Priority 
----------------------------------------------------
1. Resolve the Metadata Investigation's transitional state. 
2. Determine whether Metadata should: 
	1. • remain a foundational report-producing Investigation, or 
	1. • gain its own Consultant and ExpertFindings. 
3. Resolve the TODO in E_MetadataInvestigation. 
4. Verify that the resulting ExpertFindings reach the existing UI 
   and Conversation Framework correctly. 
5. Update this document after the Metadata decision and implementation. 
---------------------------------------------------- 
Future Work 
---------------------------------------------------- 
 After the current vertical slice is verified: 
• Complete legacy Specialist migration. 
• Verify recommendation quality and usefulness. 
• Improve Conversation recommendation selection when the framework is ready. 
• Continue improving individual Investigation depth. 
• Use the completed Ebook Expert as the reference implementation for future Experts. 
---------------------------------------------------- 
Development Rules
---------------------------------------------------- 
One file. One logical change. Build immediately. Green before continuing. 
The current project is the source of truth. Do not rely on obsolete progress 
notes when the live code provides different information. 
Do not add architecture merely because a future feature may need it. 
Complete the current Investigation before beginning another major Investigation change.
``` --- # 2. `Investigations/Consultants/CurrentFocus.md` 
This one should be much shorter and more operational. Replace the entire file with: ```
markdown # Current Focus Last Updated: 2026-08-14 ---------------------------------------------------- Current Goal ---------------------------------------------------- Finish and verify the Ebook Expert's Investigation pipeline. The project has moved beyond the earlier Quality Consultant milestone. The live code is now the source of truth. ---------------------------------------------------- Completed ---------------------------------------------------- ✔ Metadata Block exists. ✔ Metadata Report exists. ✔ Contents Investigation implemented. - Block - Report - Consultant - Investigation - ExpertFinding generation ✔ Organization Investigation implemented. - Block - Report - Evidence - Consultant - Investigation - ExpertFinding generation ✔ Duplicate Investigation implemented. - Block - Report - Consultant - Investigation - ExpertFinding generation ✔ Quality Investigation implemented. - Block - Report - Consultant - Investigation - ExpertFinding generation ✔ Repair Investigation implemented. - Block - Report - Consultant - Investigation - ExpertFinding generation ✔ Enrichment Investigation implemented. - Block - Report - Consultant - Investigation - ExpertFinding generation ✔ EbookExpert coordinates the Investigations. ✔ ExpertFindings are translated into CV_Recommendations. ✔ Conversation Framework receives the Expert-generated recommendations. ✔ Workspace receives the existing ProjectObservation path. ---------------------------------------------------- Current Investigation ---------------------------------------------------- Metadata The Metadata Investigation is the remaining architectural question. Current implementation: Files ↓ E_MetadataBlock ↓ MetadataReport E_MetadataInvestigation currently contains a TODO to populate the MetadataReport from specialist findings. The Investigation currently does not have a Metadata Consultant. ---------------------------------------------------- Immediate Task ---------------------------------------------------- Resolve the intended role of Metadata. Determine whether Metadata is: A. A foundational research Investigation whose MetadataReport is consumed by other Investigations, without producing its own ExpertFindings, or: B. A complete Investigation that must also contain a Metadata Consultant and produce ExpertFindings. Do not assume the answer. Review the existing Metadata Architecture documentation and the live implementation before changing code. ---------------------------------------------------- Why This Comes First ---------------------------------------------------- The MetadataReport is upstream research used by: MetadataReport ├── Contents ├── Organization ├── Quality ├── Repair └── Enrichment The project architecture requires research to be acquired once and reused rather than reacquired by downstream Investigations. Therefore the Metadata design should be settled before modifying the downstream pipeline. ---------------------------------------------------- After Metadata ---------------------------------------------------- Once Metadata is resolved and verified: 1. Verify the complete Ebook Expert produces meaningful ExpertFindings. 2. Verify ExpertFindings become meaningful CV_Recommendations. 3. Verify recommendations appear correctly in the existing Scout UI. 4. Verify the Conversation Framework receives the same recommendations. 5. Resolve the remaining legacy E_EbookMetadataSpecialist migration. ---------------------------------------------------- Known Conversation Limitation ---------------------------------------------------- The Conversation Framework currently receives the complete set of CV_Recommendations but intentionally selects the first recommendation. This is a known limitation of the initial Conversation Framework vertical slice. Do NOT add recommendation ranking, prioritization, scoring, or selection logic here yet. That responsibility belongs to the Conversation Framework when its recommendation-selection stage is ready. ---------------------------------------------------- Architecture Rule ---------------------------------------------------- Each Investigation answers one research question. Blocks acquire facts. Reports preserve facts. Consultants interpret Reports. Consultants produce ExpertFindings. Investigations coordinate Blocks and Consultants. Experts coordinate Investigations. Scout presents the resulting understanding. ---------------------------------------------------- Development Rhythm ---------------------------------------------------- Understand ↓ Navigate ↓ One file ↓ One logical change ↓ Build ↓ Green ↓ Repeat The developer should never have to search for where a change belongs. ---------------------------------------------------- Source of Truth ---------------------------------------------------- The current project is authoritative. These notes describe the current state of the implementation and must be updated when the implementation changes. Do not preserve obsolete milestones merely because they appeared in an earlier version of this document.