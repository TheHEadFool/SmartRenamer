This document overrides all design discussions until Version 0.1 is complete.

how you (ChatGPT) and I work together.

Rule 1: Feature Lock

Once we start a feature, we are not allowed to switch features until it is demonstrably working.

Example:

Feature: Organize photos by date.

Until it works:

❌ No AI improvements.
❌ No Scout redesign.
❌ No model renaming.
❌ No architecture discussions.

Everything that isn't required to finish that feature goes onto a backlog.

Rule 2: One Success Criterion

Every feature has exactly one definition of done.

Example:

Done = I can select a folder and SmartRenamer creates a copied folder tree organized by Year/Month.

Not "the code is cleaner."

Not "the model is better."

Not "the architecture is future-proof."

If the user can't do the thing, we're not done.

Rule 3: Bug Fix Exception Only

While implementing a feature, we are only allowed to touch unrelated code if:

it prevents compilation, or
it prevents the feature from working.

Otherwise:

Backlog.

Rule 4: Visible Progress

Every coding session ends with something you can see.

Examples:

Day 1

Folder copies successfully.

Day 2

Preview tree appears.

Day 3

EXIF dates determine folders.

Day 4

GPS names trips.

Never:

"We now have a better abstraction."

Rule 5: Refactoring Moratorium

This is the important one.

Until Version 0.1 works:

No refactoring unless it is required to finish the active feature.

That would have prevented 90% of my detours.

backlog:

□ Better naming engine

□ ScoutUnderstanding model

□ Event detector

□ AI suggestions

□ Duplicate detector

□ OCR

□ Face clustering

□ Pipeline cleanup

Rule 6: The Feature Test

Before suggesting any code change, ask one question:

Does this change directly move the current feature toward its success criterion?

If the answer is no, it goes into the backlog.

Examples:

Current Feature:

Organize photos by date.

Allowed
Read EXIF date.
Create Year folders.
Create Month folders.
Copy files.
Show progress.
Handle missing EXIF by falling back to file date.
Not Allowed
Better AI.
Better conversation engine.
Better planner.
New abstractions.
Better naming.
Better pipeline.
Better models.

Those become backlog items.