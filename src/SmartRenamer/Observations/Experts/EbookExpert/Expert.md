# Ebook Expert

## Purpose

The Ebook Expert is Scout's plugin for understanding ebook collections.

Its responsibility is to investigate ebook libraries and produce observations
that help Scout organize, repair, and improve the collection.

The Ebook Expert does not rename files directly.
It does not communicate with the user directly.
It does not modify ebook metadata directly.

Instead, it investigates the collection and reports what it learns.

---

## Architecture

Analyzer
↓
Ebook Expert
↓
Investigations
↓
Specialists
↓
Blocks

---

## Planned Investigations

- Metadata Investigation
- Cover Investigation
- Series Investigation
- Duplicate Investigation
- Organization Investigation
- Repair Investigation
- Online Lookup Investigation

---

## Plugin Goal

The Ebook Expert should eventually become a completely self-contained plugin
that can be added to or removed from Scout with minimal changes to the
application.

This document describes the architecture of the plugin and serves as the
starting point for future development.