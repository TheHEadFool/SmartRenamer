This document overrides all design discussions until Version 0.1 is complete.

how you (ChatGPT) and I work together.

Inspect Before Invent

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

Rule 7: One Implementation

Once we choose an implementation for the active feature, we don't revisit alternatives unless it fails to meet the success criterion.

Example:

Chosen:
Create "<Folder> (Scout Organized)"

Not Allowed:

"What if we asked the user every time?"
"What if we used timestamps instead?"
"What if we made it configurable?"

Those ideas go into the backlog.

Rule 8: The Simplest Thing That Works

Every feature is implemented with the smallest amount of code that satisfies the success criterion.

Not:

the most reusable

Not:

the most elegant

Not:

the most future-proof

Only:

enough to make the feature work.

We can improve it after Version 0.1.

Rule 9: No Premature Generalization

Never build for hypothetical future features.

Bad:

AbstractExecutionPipeline
ExecutionStrategy
ExecutionContext
ExecutionFactory

Good:

CopyFiles()

If a second feature later needs the same code, then extract the common parts.

Rule 10: Finish Before Optimizing

Version 0.1 is allowed to be:

ugly
repetitive
inefficient

It is not allowed to be unfinished.

Rule 11: Every Feature Must Be Demonstrable

When we say a feature is complete, we should be able to demonstrate it in less than one minute.

For example:

"Watch me point Scout at this folder."

Click.

"Scout created Downloads (Scout Organized)."

Done.

If we can't demonstrate it, it probably isn't finished.

Rule 12: Preserve User Trust

I think this one belongs because it's central to Scout's identity.

Scout must never surprise the user.

Until explicitly approved:

never overwrite files
never delete files
never rename originals
always preserve data

This is already reflected in the "Organized Copy" approach, but I'd make it an explicit principle.

Rule 13: Decisions Are Sticky

This one would have saved us several conversations.

Once we decide something like:

Destination folder:

<Folder> (Scout Organized)

that decision stays in effect unless:

it prevents the feature from working, or
the user explicitly changes the requirement.

We don't reopen settled questions every few sessions.

Product Principles. 

1. Safety before convenience. Originals are never modified unless the user explicitly requests it.
2. Scout explains its reasoning. Every recommendation should be understandable.
3. Every action is previewed before execution.
4. The user stays in control. Scout recommends; the user approves.
5. Defaults should be safe. If there are multiple reasonable actions, choose the one that minimizes the risk of data loss.

These aren't coding rules—they're product rules. Whenever we're unsure between two designs, we can ask which one better reflects these principles.


Extend the existing pipeline whenever practical.

Today we discovered the real pipeline is:

Guide
    ↓
WorkflowResult
    ↓
RenameService
    ↓
RenameExecutor

Not

Guide
    ↓
ScoutPlanExecutor

That discovery came from following the data instead of our assumptions.

3. Existing Code Gets the Benefit of the Doubt

This one is subtle.

## Existing Code Gets the Benefit of the Doubt

Assume existing code has a reason for existing.

Before replacing it, understand:

- what problem it solves
- what assumptions it makes
- whether it can be extended

Working software is valuable.

I think this is especially important because we're both enthusiastic designers. It's easy to get excited about a cleaner architecture. This rule reminds us to respect code that's already delivering value.

Working software has priority over elegant architecture.