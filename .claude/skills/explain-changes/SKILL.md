---
name: explain-changes
description: Walk through every uncommitted change (modified tracked files AND new untracked files) one file at a time. For each file, explain what changed, why, how it relates to docs/implementation-plan.md, and how it fits the FR/NFR requirements in docs/requirements.md. After each file offer next / skip / skip-all, and end with a one-row-per-file summary table. Use when the user asks to "explain the changes", "walk me through the diff", "explain what's uncommitted", or review pending work before committing.
---

# Explain uncommitted changes

Give the user a guided, one-file-at-a-time tour of everything that is **not yet
committed** — both modified tracked files and brand-new untracked files. This is a
teaching/review walkthrough, not a code review: the goal is understanding, grounded in the
project's own roadmap and spec.

## Step 1 — Gather the uncommitted files

Find every file with pending changes (staged, unstaged, and untracked):

```bash
git status --porcelain      # the full list; '??' = new/untracked, ' M'/'M ' = modified, etc.
```

For each file, get the actual content to explain:
- **Modified tracked file** → `git diff HEAD -- <file>` (covers staged + unstaged).
- **New untracked file** → read the whole file (it's all "new").

Ignore nothing by default, but you may fold purely mechanical, machine-generated files
(e.g. `package-lock.json`, `*.min.*`, generated migrations' designer files) into a single
"generated — no manual review needed" note rather than a full explanation. If in doubt,
include it.

Read `docs/implementation-plan.md` and `docs/requirements.md` **once up front** so every
per-file explanation can cite the right slice/commit and the right FR-/NFR- ids. Skim
`git log --oneline -5` for the recent context.

## Step 2 — Order the files into a coherent story

Don't just use `git status` order. Sequence the files so the walkthrough reads well —
typically: foundational/new modules first, then the files that depend on them, then tests,
then docs/config last. Keep the chosen order fixed for the rest of the session so "next"
and "skip" are unambiguous.

State the plan up front in one line: how many files, and the order you'll go in.

## Step 3 — Explain one file at a time

Explain exactly **one** file per turn. For that file, cover these four points, with
headings so they're easy to scan:

1. **What changed** — concretely: the functions/types/sections added or edited (not a
   line-by-line dump — the meaningful units).
2. **Why it was changed** — the intent/problem it solves.
3. **How it relates to the implementation plan** — name the slice and the specific
   commit/bullet in `docs/implementation-plan.md` this advances (or the Current State cell
   it updates).
4. **How it fits the requirements** — cite the relevant FR-/NFR- id(s) from
   `docs/requirements.md` and say in a sentence how this file serves them. If a file has no
   direct requirement (e.g. tooling/config/docs), say so plainly.

Keep it focused and readable — a few tight paragraphs or short bulleted points per section,
not an essay.

## Step 4 — Offer the three options (only if a next file exists)

After explaining a file, if there is still at least one un-explained file left, end the
turn by asking the user to choose one:

- **next** — explain the next file in the order.
- **skip** — skip the next file (leave it unexplained) and continue with the one after it.
- **skip all** — stop explaining the remaining files and jump straight to the summary
  table (Step 5).

Present them as a short, literal list of those three words so the user knows what to type.
On the **last** file there is no next file, so don't offer the options — go straight to
Step 5.

Interpreting replies:
- `next` → explain the next file, then offer the options again.
- `skip` → do not explain the next file; explain the one after it (then offer options
  again). If skipping leaves nothing to explain, go to Step 5.
- `skip all` → go to Step 5 immediately.

## Step 5 — Final summary table

After the last file (or on "skip all"), print a summary with **one row per uncommitted
file** — including files that were skipped — so the user has a complete recap:

```markdown
| File | What changed | Ties to |
|------|--------------|---------|
| `path/to/file` | one-line summary | Slice / FR-xx, NFR-yy |
```

Keep each row to a single terse line. Mark skipped files (e.g. append "*(skipped)*" in the
What-changed cell) so it's clear which got a full explanation.

## Guardrails

- **Read-only.** This skill explains; it does not edit, stage, or commit anything. If the
  user wants changes, that's a separate request.
- **One file per turn** during the walkthrough — never dump all files at once (except the
  final summary table).
- **Ground every claim in the repo.** Cite real slice/commit lines and real FR-/NFR- ids;
  don't invent a requirement to make a file look load-bearing. If something is pure
  tooling/scaffolding, say so.
- If there are **no** uncommitted changes, say so and stop — there's nothing to explain.
