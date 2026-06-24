# ADR 0002: Use shadcn/ui And Radix Primitives For The Frontend

Date: 2026-06-24

## Status

Accepted

## Context

SoundLens needs a modern, clean, professional interface for acoustic investigation and product-sound benchmarking. The UI should avoid the cluttered feel of older acoustic tools and generic audio editors while still supporting dense engineering workflows.

Mantine is a viable speed-oriented option, but the project needs more design ownership and a distinctive product surface.

## Decision

Use shadcn/ui with Radix primitives as the preferred frontend component foundation.

## Rationale

- shadcn/ui provides source-owned components that can evolve with the product.
- Radix primitives provide accessible interaction foundations.
- The stack supports a clean, modern, custom design language without fighting a large prebuilt visual system.
- It pairs well with React, TypeScript, Vite, and semantic design tokens.

## Consequences

- Components should be added intentionally, not installed wholesale.
- Styling should use semantic tokens rather than scattered raw colors.
- Accessibility requirements from Radix/shadcn patterns should be preserved.
- Mantine remains a fallback only if early prototyping proves speed is more important than design ownership.

