Phase 1 — Replace EmberSystem
Component 1

CampFireEffects.cs

Responsibilities:

Own animation timer
Own particle list
Spawn particles
Update particles
Remove dead particles

Result

Exactly reproduces today's embers.

Nothing fancy.

Just a cleaner engine.

✅ Commit

Phase 2 — Particle Infrastructure
Component 2

Particle.cs

Contains

Position
Velocity
Age
Lifetime
Opacity
Size
Visual

Nothing campfire specific.

Future proof.

Commit.

Component 3

ParticleType.cs

Ember

Smoke

Firefly

Leaf

Snow

Only Ember used initially.

Commit.

Phase 3 — Better Embers
Component 4

Replace current ember spawning.

Add

✔ better colors

✔ variable size

✔ elongated cinders

✔ random drift

✔ smoother fade

✔ rare escapee embers

Commit.

Phase 4 — Smoke
Component 5

SmokeParticle.cs

Adds

soft wisps

growth

fade

curl

Commit.

Component 6

SmokeSpawner

Responsible ONLY for

when

where

how often

Commit.

Phase 5 — Lighting
Component 7

Fire glow pulse.

Tiny brightness changes.

Rare ember slightly brightens fire.

Commit.

Phase 6 — Wind
Component 8

Global wind.

Every particle reacts.

Not obvious.

Just enough.

Commit.

Phase 7 — Atmosphere

Components added one at a time.

Fireflies

Leaves

Snow

Ash

Floating dust

Magic particles

Moonbeams

Each becomes

class FireflyParticle

or

class SnowParticle

No engine changes.

The Folder

Eventually we'll have

Camp
│
├── CampFire.xaml
├── CampFire.xaml.cs
│
├── CampFireEffects.cs
│
├── Particle.cs
├── ParticleType.cs
│
├── EmberParticle.cs
├── SmokeParticle.cs
├── FireflyParticle.cs
├── SnowParticle.cs
│
├── SmokeSpawner.cs
├── Wind.cs
│
└── Effects

Notice something...

There is no 800-line file anymore.

There are ten files.

Each around 100 lines.

Each understandable.

Each testable.