## our standards.

Identify the file being edited - and the path to it BEFORE we paste code into it.
Classes under ~200 lines give the full code not change this line and that line.
One responsibility per class.
No magic numbers.
Every effect configurable from constants.
Every new feature compiles before moving to the next.
Commit after every completed component.
If we need to redesign, do it before writing code.
Whenever we replace a source file, we replace the ENTIRE file.

Never:

"insert this here"
"paste this after line 43"

unless it's only one or two lines, then give the line numbers to reduce search time.

## Whole-file replacements are much safer.

## Never add a new method while we're debugging.

When replacing a subsystem, do not modify it in place. 
First restore the last known-good state, then replace it 
with a proven subsystem, removing obsolete code as each 
replacement is verified. Never leave two competing 
implementations in the codebase.

## Before building an emitter or animation system, 
always prove that a single instance of the visual 
object can be created, displayed, and rendered.

## Every animated subsystem must be connected 
to the application's update/render loop 
before any visual tuning begins.

## Whenever we modify a file, 
I will provide the entire file 
unless we are replacing a single line.

## If another Expert could reasonably use a file then → no prefix (shared infrastructure).
## If it belongs only to the a module → a_ prefix. b module → b_ prefix and so on.

## An Expert owns everything required to understand its domain.
An Investigation asks questions. It does not perform work.

## A Specialist performs one investigation well.

Nothing more.

## A Building Block performs one reusable task.

Never move shared architecture until the replacement is complete.

## If two Experts need the same class, promote it to the platform.

Don't modify a file until we know why it's the right file.