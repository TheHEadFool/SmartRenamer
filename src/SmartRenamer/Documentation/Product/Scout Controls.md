# Scout Controls

## Purpose

Scout separates explanation from direct operation. The conversation explains what Scout is doing and why. The **Scout Controls** panel on the left is the direct-control surface for users who prefer buttons. The Live Report remains the collection-wide dashboard and contains per-item attention actions.

## Control stages

The left panel exposes only choices that are currently available. The same Guide action path is used by buttons and typed conversation.

### Discovery

When Scout needs to know how to search, choices such as **Search nested folders** and **Current folder only** appear in Scout Controls.

### Repair / review

When an Expert exposes a user decision or action, it appears in Scout Controls. Action evidence remains visible there. The conversation does not duplicate these controls.

### Organization

When repair decisions are exhausted, organization is presented as two separate decisions:

1. **Destination** — where Scout creates the organized copy. The original collection remains protected.
2. **Organization style** — how folders and files are arranged.

The destination must be configured before organization style is committed. Neither decision performs filesystem work until the Expert has received the complete organization configuration.

For example, **Author → Series → Title** means:

```text
Author\
    Series\
        01 - Title.epub
        02 - Title.epub
    Book Without A Series.epub
```

A book without an established series is not assigned a fabricated series. It remains directly under the author's folder. Titles are ordered according to the selected file-order policy.

## Conversation behavior

The conversation should tell the user where to act, for example:

> Your choices are in the Scout Controls panel on the left. Choose the option that best matches what you want Scout to do.

Conversation action buttons are intentionally not duplicated in the transcript. Typed input remains supported and routes through the same generic Guide/Expert path.
