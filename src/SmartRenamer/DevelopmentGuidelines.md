# Development Guidelines

## 1. Always Build

Every feature should compile before moving to the next step.

## 2. One Vertical Slice

A feature is not complete until:

- View
- ViewModel
- Model
- Service (if needed)
- Guide integration

are all connected.

## 3. Conversation First

Whenever possible, Friend should interact with the Guide instead of directly manipulating the interface.

## 4. Guide Cards

Guide Cards are temporary pieces of UI that appear as part of the conversation.

A Guide Card should:

- explain why something is needed
- perform one task
- disappear when finished

## 5. Options

The left panel contains Options.

Options represent capabilities.

The Guide recommends Options.

Friend chooses Options.

## 6. Project Understanding

The Guide always tries to understand the project before recommending actions.

## 7. Friend Stays in Control

The Guide recommends.

Friend decides.

Never perform destructive actions without Friend approving them.

## 8. Simple Beats Clever

If there are two solutions:

Choose the one that will still make sense a year from now.


Scout never chooses a naming convention.

Scout helps Friend choose one.

Thinking = internal reasoning

Conversations = user-facing dialogue

Those should never be mixed.

Every commit must make Friend noticeably more successful than before.