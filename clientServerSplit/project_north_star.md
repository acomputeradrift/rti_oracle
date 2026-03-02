# Project North Star

## Purpose

This document defines the single shared objective for the current RTI Oracle refactor effort so all future agents work toward the same outcome.

## North Star

The current effort is a preparatory architectural refactor of the existing desktop app.

The goal is to:

- reduce lines of code
- split responsibilities cleanly
- preserve all current functionality
- preserve speed and responsiveness
- preserve current capabilities
- improve reuse and readability
- shape the codebase for a future client/server split

## What We Are Doing Now

We are refactoring the current application so it is easier to:

- maintain
- test
- extend
- divide into a lightweight desktop client and a server-backed API system later

This means:

- thinning oversized files
- extracting deterministic logic into reusable services
- separating UI concerns from domain logic
- isolating updateable intelligence that may later move server-side

## What We Are Not Doing Now

At this stage, we are not:

- building the server
- introducing the full client/server runtime split
- changing product scope
- reducing behavior or features for the sake of smaller code

This is preparation, not the final migration.

## Architectural Direction

The intended long-term shape is:

- a lightweight desktop client for UI, local control, and operator workflow
- a server/API for centrally managed intelligence, updates, and remotely delivered improvements

The current refactor should move the code toward that destination without forcing the split prematurely.

## Immediate Design Rule

When making refactor decisions now:

- keep UI orchestration in the desktop app
- move deterministic, UI-free logic into portable shared-domain services
- isolate logic that is likely to become remotely managed later
- avoid abstractions that add more code than they remove

## Success Standard

A good change in this phase does all of the following:

- lowers LOC or reduces responsibility density
- makes the code easier to read and test
- does not reduce performance
- does not remove capability
- creates cleaner boundaries for a future client/server version

If a change reduces LOC but makes future separation harder, it is the wrong change.

## Multi-Agent Rule

All implementation planning and packet assignment should be judged against this north star.

Parallel agents should extract clean, portable, testable slices now so the later server-backed version can be built on stable boundaries instead of another large rewrite.
