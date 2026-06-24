# ADR 0001: Use .NET And FastEndpoints For The Backend

Date: 2026-06-24

## Status

Accepted

## Context

SoundLens needs a backend that can handle audio ingestion, deterministic DSP workflows, evidence packaging, privacy-sensitive file handling, and server-side OpenAI API integration.

The project owner prefers C# and wants to keep backend code simple, testable, and aligned with vertical slices.

## Decision

Use C# on .NET for the backend and FastEndpoints for HTTP API endpoints.

## Rationale

- C# is a good fit for structured backend code, tests, and long-lived domain models.
- .NET gives strong performance and mature tooling.
- FastEndpoints supports clear command/handler/endpoint-style vertical slices without forcing excessive framework ceremony.
- Server-side C# is a suitable boundary for OpenAI API calls, DSP orchestration, and privacy controls.

## Consequences

- Backend examples, tests, and project structure should assume .NET.
- API work should follow vertical-slice boundaries where practical.
- FastEndpoints conventions should be documented in `docs/backend/README.md` as they emerge.
- The project should avoid adding large backend abstractions before the first end-to-end slices prove they are needed.

