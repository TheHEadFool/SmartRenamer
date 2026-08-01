## Decision:

The Intelligence Engine detects what a collection is.
The Observation Engine asks the appropriate Experts to investigate.
Experts produce ExpertFinding objects.
The UI displays findings without knowing which Expert created them.

## Consequences:

New modules require no UI changes.
New modules require no changes to existing Experts.
Scout grows by adding Experts, not modifying existing code.