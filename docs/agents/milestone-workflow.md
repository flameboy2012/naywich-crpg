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

## Rules learned in M2

- **Check each worktree's base.** Several implementer worktrees started on an old `main` commit, not the integration branch. Every implementer prompt must say to confirm the base and reset onto the integration branch tip, naming the expected commit.
- **Grill state that lasts over time, not just rules for one moment.** The spec said "once aware, no leash" but never said what an aware enemy does when its target goes down, so two tickets disagreed and the orchestrator had to rule mid-build. When grilling, ask what happens next to every state the spec creates (aware, down, paused, defeated), including across a scene restart.
- **Put every place a setting applies in the spec's acceptance criteria.** ADR 0003 said combat speed scales cooldowns and movement; the first ticket did only cooldowns. "Tunable" also needs a named place to tune it (inspector, project setting), or it ends up as a code constant.
- **Treat UI placement as provisional.** The combat log's corner was picked in the spec without asking, and the play-test moved it. Mark screen positions as play-test questions, like visibility rules.
- **Split a check file before it grows past one ticket.** CombatCheck took cases from four tickets, switching enemy AI off for older cases and changing its seed twice. Give each ticket's engine behaviour its own check file, sharing setup through `HeadlessCheck`.
- **Make every commit load.** A commit that references a script added in a later commit breaks bisecting. Ask for this in implementer prompts.
- **Widen the ticket graph where you can.** After two parallel tickets, M2 was a chain of four. Look for parts that don't need the previous ticket (Core rules for a later ticket, UI with stub data) and make them separate unblocked tickets.
- **Verify the integration branch yourself before the play-test.** Run the build, `dotnet test` and every check on the merged tip, not just trust each implementer's report.
- **Commit the editor's rewrites before merging.** Opening the project for the play-test rewrote `Main.tscn` (uids, default values) and blocked the checkout to `main`. Check `git status` after the play-test and commit those rewrites as a chore.
