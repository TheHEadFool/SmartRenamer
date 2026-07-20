# Changelog

All notable changes to SmartRenamer are documented here.

The format follows Keep a Changelog.

---

## [Unreleased]

### Added

- Scout conversation framework.
- Project investigation workflow.
- Rename preview model.
- Scout planning model.
- Intent interpreter foundation.
- Folder investigation summary.
- Conversation memory.

### Changed

- Replaced button-driven workflow with conversational workflow.
- Refactored planning around ScoutPlan.
- Moved toward complete workflow architecture.

### Fixed

- Duplicate WorkflowResult definitions.
- ProjectWorkflow architecture.
- RenamePreview namespace conflicts.
- FolderSummary integration.

### Planned

Sprint 1

- Remove underscores capability.
- Live rename preview.
- Demonstrable end-to-end workflow.

# v0.1.0-alpha
## Demo 1 – Scout Understands

### Added

- Scout conversational startup.
- Intent recognition.
- First natural-language capability:
  - "Remove underscores"
- Rename preview generation.
- Safe preview workflow.

### Changed

- Conversation is now the primary interface.
- Scout now asks what Friend wants to accomplish before proposing actions.

### Not Yet Implemented

- Applying rename operations.
- Undo.
- Multi-step workflows.
- Multiple capabilities.