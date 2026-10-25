# Milestone workflow

The order that worked for M1, plus the fixes for what went wrong.

## Order

1. **Grill** the milestone against the design doc. Settle every decision that leaks into later milestones (scale, scene structure, terms) before any code. Write terms to `GLOSSARY.md` and hard-to-reverse choices to `docs/adr/` as they are settled.
2. **Spec**, as one GitHub issue (see M1's spec, #6, for the shape). The spec names the test approach on both sides: xUnit for `Crpg.Core`, and headless checks in `game/tools/` for engine behaviour.
3. **Tickets**: tracer-bullet slices of the spec, created after it, as sub-issues of the milestone issue with native blocking edges.
4. **Build** on one integration branch per milestone. Each ticket ends with build, `dotnet test`, a headless run and every check passing (see `godot-gotchas.md`).
5. **Review** the integration branch against the spec and the glossary.
6. **Play-test** by the developer. Agents cannot judge feel, rendering or whether input feels right. List those items for the developer explicitly.
7. **Merge** to `main` only after the play-test passes.

## Rules learned in M1

- **Check each step before the next.** Close issues only after confirming the merge landed on `main`. In M1, a chained command closed issues even though the branch checkout had failed. Look at each command's result, or let `Closes #N` in a merged PR do the closing.
- **Surface ambiguous numbers to the developer.** When a spec value has two readings (M1's "1 tile per second" on an isometric grid), get the developer's answer and record it in the relevant ADR before building.
- **Size the agent count to the graph.** A straight chain of tickets runs no faster with one agent per ticket. Use parallel implementers only when the frontier holds several unblocked tickets.
- **Keep the developer's scope small.** The developer prefers thin first milestones and splits extras into a follow-up (M1 to M1.5). Propose cuts instead of expansions.

## Rules learned in M1.5

- **Give each parallel ticket its own lane.** Tickets that run at the same time should touch different code paths. Name the other agent's area in each prompt, and prefer a new headless check file over growing a shared one.
- **Tell running agents when the integration branch moves.** An agent can finish on a stale base. After each merge, message any agent still running with the new tip and the areas likely to conflict. Merge its branch only once it has merged that tip.
- **Name the glossary's Avoid list in implementer prompts.** Review found "member" and "group" in M1.5 code, even though the glossary avoids both.
- **Define shared numbers once, in Core.** Tile size and the party cap were copied into a second place until review. If Core and game code both need a value, put it in Core and have the game read it.
- **Treat visibility and feedback rules as play-test questions.** The spec hid path lines on deselection. The play-test reversed that, because seeing every party member's order matters more. Mark such rules as provisional in the spec.
- **Make the developer's play-test list specific.** List exact things to try (a click inside an obstacle, deselecting someone mid-move), not general areas.
