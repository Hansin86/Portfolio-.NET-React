---
name: update-plan
description: Update docs/implementation-plan.md when a commit or slice lands, or when planning the next slice. Ticks commit/slice status markers, refreshes the Current State table and its date, and keeps the terse slice → commit-list format consistent. Use when work has just landed ("mark X done", "update the plan", "we finished the auth screens") or when scoping an upcoming slice.
---

# Update the implementation plan

Keep `docs/implementation-plan.md` in sync with reality. It is the *how / in what order*
roadmap — `requirements.md` is the *what*, `database-schema.md` is the data model. Do not
duplicate their content here.

## Format contract (do not drift from this)

The file has three parts: **Current State** table → **Slices** → **Cross-cutting** +
**Key rules**. Every slice follows the same shape:

```markdown
### 🚧 Slice name (FR-xx, NFR-yy)

One or two sentences: what this slice delivers and why it exists.

Commits:
- [x] Short commit description (the concrete thing that landed)
- [ ] Next commit, not yet done

Key decisions: deviations worth remembering — guards, caps, contract choices. One paragraph.
```

Rules:
- **Slice markers** (in the `###` heading): ✅ done · 🚧 in progress · ⬜ not started.
- **Commit markers**: `- [x]` done · `- [ ]` not done. A slice is 🚧 the moment its first
  commit is `[x]` and ⬜→✅ only when *every* commit is `[x]`.
- **Terse.** Commit lines are a phrase, not a paragraph. Rationale goes in "Key decisions"
  (one paragraph) or is left to git/code — do not narrate step-by-step.
- Keep a `Prerequisites:` checklist block above `Commits:` only when a slice has real
  cross-cutting prereqs (see the Transactions CRUD slice as the template).
- Future/unstarted slices carry a *tentative* commit list — fine to reshape as the slice
  is actually scoped.

## When a commit or slice lands

1. Read `docs/implementation-plan.md` and skim recent history: `git log --oneline -15`.
2. Find the slice; tick the matching `- [ ]` → `- [x]`. If the landed work doesn't map to
   an existing line, add/split a commit line rather than cramming it into another.
3. Promote the **slice marker** if its state changed (⬜→🚧 on first commit, 🚧→✅ when all
   commits done). Do the same for any `Prerequisites:` block.
4. Update **Current State**: edit the affected layer cell(s) to match what now exists, and
   bump the `## Current State (YYYY-MM-DD)` date to today. Keep cells scannable — swap in
   the new fact, don't just append.
5. If a "Key decision" was made (a guard, a cap, an API-contract choice, a deviation from
   the plan), add or tighten one line in that slice's Key decisions paragraph.
6. Report what you changed in a sentence. Do **not** commit unless asked.

## When scoping the next slice

Flesh out the ⬜ slice's tentative commit list into concrete `- [ ]` lines (one per
intended small commit), refine the one-liner, and note any prerequisites. Leave the marker
⬜ until work starts. Mirror the vertical-slice order from the Key rules section: Domain →
Application → Infrastructure → API → tests.

## Guardrails

- Match the existing terseness and marker style exactly — this file is meant to stay
  scannable. If it's growing prose, trim.
- Don't invent status. Only tick a box when the work is actually in the repo (verify via
  `git log` / the code, not just because the user said "I'll do it").
- Keep dates absolute (today's date), never "recently" / "now".
- Don't restate requirement text or schema — reference FR-/NFR- ids instead.
