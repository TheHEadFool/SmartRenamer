# Session Notes

Current Status

✓ Workflow Engine completed

✓ Pipeline architecture completed

✓ FileContext introduced

✓ StepCard control created

✓ WorkflowCanvas created

✓ Visual workspace beginning

✓ Vision.md started

✓ Roadmap.md started

Next Session

- Build Step Catalog
- Dynamic Building Blocks panel
- Replace hardcoded buttons
- Begin Guide Engine
- Design Project Discovery conversation model

Major Decisions

- AI is a Guide, not a chatbot.
- Guide asks questions users can answer.
- Teach outcomes, not technologies.
- Projects before files.
- Support local and cloud data equally.
- Build confidence rather than automation.

Today we shifted from building infrastructure to building complete end-to-end features.

Future work will be organized around user-visible capabilities rather than internal components.

## Lesson Learned

When compiler errors point to duplicate class definitions, search the solution before changing code.

Using **Ctrl+Shift+F** to locate duplicate class declarations quickly identified an accidental copy/paste error that would have been difficult to diagnose by inspection alone.

Following the evidence is faster than guessing.

Scout always drives the experience.

# 2026-07-19

Major discovery:

We were about to build a complex plugin architecture.

The Constitution stopped us.

Instead we finished the active feature first.

We realized:

Observe
↓
Reason
↓
Act

Capabilities discover facts.

The planner makes decisions.

The executor performs actions.

Important lesson:

Architecture should emerge from working software.

Do not wire the Capability Framework into the pipeline until
Related Asset Organizer is complete.

Backlog:

- Capability Framework
- IntelligenceEngine
- RelatedAssetCapability
- Typed Facts

# 2026-07-19

Lesson Learned

Today's debugging session reinforced several important engineering principles.

The architecture was correct.

The implementation was largely correct.

The difficulty came from assuming which component owned the bug instead of
tracing the data through the pipeline.

Successful debugging came from following:

Observe
↓
Reason
↓
Act

stage by stage until the first incorrect value was located.

Future debugging should always follow this workflow before considering
architectural changes.

Additional lesson:

Replacing complete source files proved significantly safer than editing partial
methods during collaborative development.