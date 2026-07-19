Version 2 Ideas (Roadmap)
Scout Conversation Engine
Ask only questions that cannot be answered automatically.
One question at a time.
Explain why the question matters.
Human Naming

Instead of

Use EXIF DateTimeOriginal

Scout says

Most cameras remember when each picture was taken. I can use that information if you'd like.

Naming Styles

Instead of assuming a naming convention:

Scout asks

"How do you usually remember these photos?"

Examples:

By trip
By date
By people
By place
Keep current names
Learn Preferences

Eventually Scout remembers things like

Jonathan usually prefers dates over sequential numbers.

without asking every time.

Explain Decisions

Instead of

Recommendation: Date Taken

Scout explains

Most of these photos still have camera filenames, so organizing by the day they were taken will probably make them much easier to find.

Progressive Discovery

Never overwhelm Friend.

Observe.

Explain.

Ask one question.

Repeat.

RenamePreviewBuilder
        │
        ▼
IRenameCapability
        ▲
        │
TextReplacementCapability

Later we'll plug in:

DateRenameCapability
NumberFilesCapability
RemoveTextCapability
ChangeCaseCapability
MusicRenameCapability

Phase 1 — Build the Engine (where we are now)
✅ Analyze folders
✅ Generate observations
✅ Build recommendations
✅ Preview renames
⬜ Rename files
⬜ Create organized copies
⬜ Organize by date
⬜ Organize by media type
⬜ Organize by document type
⬜ Find duplicate files
⬜ Detect empty folders
⬜ Detect very large files
⬜ Detect old unused files
⬜ Remove illegal filename characters
⬜ Fix capitalization
⬜ Remove double spaces
⬜ Explain every recommendation
Phase 2 — Intelligence
Confidence scoring
Multiple organization strategies
AI-assisted filename suggestions
Learn user preferences
Undo history
Batch approval/rejection
Phase 3 — Polish
Redesigned UI
Animations
Icons
Better layouts
Drag-and-drop workflows
Keyboard shortcuts
Themes