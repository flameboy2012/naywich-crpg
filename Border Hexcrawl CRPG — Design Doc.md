# Border Hexcrawl CRPG — Design Doc

Oct 24, 2026 · @Robbie

## Overview

A hobby CRPG in Godot 4, ported from a 10x10 D&D 5e hexcrawl, with the look and feel of Baldur's Gate 1: 2D isometric, a party controlled RTS-style, and real-time-with-pause combat.

The party arrives at a village on the border of a kingdom, resolves a few local problems, and is drawn into a quest to find two powerful artifacts that rival factions are also hunting. The hexmap is a design tool only: players never see hexes. They walk a graph of connected zones, including interiors and dungeons.

**Audience for this doc:** the developer and Claude Code. Sections 3–8 are the build spec; section 9 is the first thing to implement.

## Pillars and scope

The game is small, finishable, and built so features can be added later without rewrites.

**Pillars**

- **Exploration feels like a hexcrawl.** A compact world of distinct places, each worth visiting, with travel taking meaningful in-game time.
- **BG1-style party control.** Select, order, pause, repeat. Same controls in and out of combat.
- **5e at its core, simplified.** d20 vs AC, dice damage, ability scores. Start minimal and grow.
- **Factions react.** Choices about the artifacts change how factions treat the party.

**In scope for v1**

- A fixed party of 2–4 characters
- Click / box / portrait selection, click-to-move, pause
- Basic melee and ranged attacks only
- The village hub plus a handful of zones
- Save and load

**Out of scope until later**

- Spells, class features, conditions, reactions
- Character creation, levelling, inventory beyond equipped weapon/armour
- Dialogue trees beyond simple linear conversations
- Survival mechanics (rations, weather, exhaustion)
- Fog of war, formations, voice, music

## World structure

The world is a graph of zones: each zone is its own Godot scene, and exits connect zones to each other. The hexmap survives only as data.

**Zones**

- An **outdoor zone** represents one hex (occasionally two adjacent hexes merged). Roughly 1–3 screens of isometric terrain.
- An **interior zone** is a building, cave or dungeon level, entered from a door or entrance in a parent zone.
- Not every hex needs a hand-built zone. Uninteresting hexes can be crossed on a travel screen (below) or merged into neighbours.

**Exits**

- An exit is an area the party walks into (map edge, door, stairs). It names a target zone and an entry point in that zone.
- When the selected party reaches an exit, the whole party transitions together. Party members not near the exit are pulled along (BG1 behaviour); optionally require all members to gather first.
- Each exit carries a **travel time**. Outdoor edge exits default to 3 hours (the tabletop hex crossing time); doors and stairs are 0.

**Time**

- A global game clock advances in real time while unpaused (suggest 1 real second = 1 game minute, tunable) and jumps forward on zone travel.
- The clock drives day/night lighting later, and timed faction events.

**Random encounters**

- On an outdoor transition, roll on the source hex's encounter table. A hit either spawns an encounter in the destination zone or loads a small generic ambush map for that terrain.
- Off by default in v1; hook in place.

**Hex data**

Every outdoor zone records its hex coordinate, terrain type, encounter table and faction presence. This keeps the original hexcrawl as the source of truth for design, even though players never see it.

**Optional: travel map**

A painted, hex-free region map that unlocks visited locations for fast travel, BG-style. Not required for v1.

## Party control and movement

Controls are identical in and out of combat: select units, then right- or left-click to order them.

**Selection**

- Left-click a party member to select only them.
- Shift-click to add or remove from the selection.
- Left-drag to box-select party members inside the rectangle (screen space, projected for isometric).
- Portrait column on the screen's side: click to select, shift-click to add, double-click to centre the camera.
- Keys 1–6 select by portrait slot; Ctrl+A selects the whole party.
- Selected units show a ring under their feet; portraits highlight.

**Orders**

- Click ground: move selected units there. Multiple units spread into simple offset positions around the target so they don't stack.
- Click enemy: selected units attack it (move into range, then attack).
- Click NPC or object: walk to it, then interact (talk, open, use, pick up).
- A move marker briefly shows the destination.

**Pathfinding**

- Each zone has a NavigationRegion2D baked from its tilemap. Each character has a NavigationAgent2D.
- Use agent avoidance so party members steer around each other.
- Isometric art, but movement happens in world 2D coordinates; the tilemap's isometric layout handles projection.

**Camera**

- Pan with screen edges, WASD/arrows, or middle-mouse drag. Clamp to zone bounds.
- Optional follow-selected toggle.

## Combat

Combat is real-time with pause, as in Baldur's Gate 1: there is no separate combat mode, just enemies becoming hostile in the same zone.

**Pause**

- Space toggles pause. Orders can be issued while paused and execute on resume.
- Auto-pause options (on by default): enemy sighted, party member at low HP, party member down.

**Timing: rounds become cooldowns**

- One 5e round = 6 game seconds. Each attack has a cooldown equal to one round (scaled by a global combat speed setting).
- A unit attacks when its target is in range and its cooldown is ready; otherwise it moves to close distance.
- Movement speed derives from 5e speed (30 ft per round becomes a tunable pixels-per-second value).

**Attack resolution (v1)**

- Roll d20 + attack bonus vs target AC. Natural 20 always hits and doubles damage dice; natural 1 always misses.
- Damage = weapon dice + ability modifier. HP to 0 means down (party) or dead (enemy).
- Melee range \~1 tile; ranged weapons have a range in tiles and need line of sight later (not v1).
- Floating combat text plus a scrolling combat log showing each roll.

**Default behaviour when idle**

- A unit with no current order and an enemy within its awareness radius attacks the nearest enemy.
- Player orders always override. After finishing a player order, the unit returns to idle behaviour.
- Enemies use the same logic in v1: attack the nearest party member.
- Structure AI as a small state machine (Idle, Moving, Attacking, Down) so smarter behaviours slot in later.

**Party down**

- Downed members stay on the field and recover after combat at 1 HP (simplified; death saves later). All members down = game over, reload.

## Rules and data model

All game data lives in Godot Resources (.tres), so new monsters, weapons and zones are content, not code.

| Resource | Key fields (v1) | Later |
| --- | --- | --- |
| `CharacterStats` | name, portrait, STR/DEX/CON/INT/WIS/CHA, max HP, AC, speed, proficiency bonus, equipped weapon | class, level, XP, skills, saves |
| `WeaponData` | name, damage dice (e.g. 1d8), damage type, ability used (STR/DEX/finesse), range in tiles, is\_ranged | properties, magic bonus |
| `ArmorData` | name, base AC, max DEX bonus | stealth disadvantage |
| `MonsterData` | extends CharacterStats; awareness radius, faction, XP value, loot table | abilities, resistances |
| `ZoneData` | id, scene path, hex coord, terrain, encounter table, faction presence | weather, ambient audio |
| `EncounterTable` | weighted list of monster groups, chance per transition | time-of-day variants |
| `FactionData` | id, name, starting reputation, hostility threshold | relations between factions |

**Rules code**

- A single `Rules` autoload holds pure functions: `roll(dice_string)`, `ability_mod(score)`, `attack_roll(attacker, target)`, `damage_roll(attacker, crit)`.
- All randomness goes through one seeded RNG so fights can be reproduced when debugging.
- Derived values (attack bonus, AC) are computed, never stored, so equipment changes just work.

**Adding 5e features later**

Planned order: saving throws, then conditions (prone, poisoned, etc.), then a few class features, then a small spell list. Each should be a self-contained Resource-driven effect rather than special cases in combat code.

## Narrative and factions

The story runs in three acts, all anchored on the border village as the hub.

1. **Arrival.** The party reaches the village. Local problems introduce the area, the factions and the controls.
2. **The hunt.** Clues point toward two artifacts. The party explores outward through the zone graph while factions compete for the same prizes.
3. **Resolution.** Who ends up holding the artifacts decides the ending.

**Quest system**

- Quests are data: id, stages, and flags that advance them. A global `GameState` holds all story flags (`village_well_cleared`, `artifact_a_found`, etc.).
- NPC dialogue, zone contents and exits can check flags, so the world changes as quests progress.

**Factions**

- Each faction has a reputation score with the party. Actions (quests done, NPCs killed, artifacts handed over) adjust it.
- Below a threshold, that faction's members spawn hostile; above another, they offer help, trades or information.
- Faction presence per hex (in `ZoneData`) controls where their agents appear.

**Dialogue (v1)**

- Simple linear or lightly branching conversations defined as Resources: lines, a few choices, choices can set flags or change reputation.
- Skill checks in dialogue come later.

**The village hub**

- Shop, inn (rest to heal and advance time), quest givers, and visible signs of faction influence that change with reputation.

## Opening quest: The Missing Lumberjacks

The vertical slice (M5) is the original tabletop hook: rescue the village's missing lumberjacks from the orc Skullaplitta and his goblins.

**Beats**

1. **Arrival.** The village looks bleak: farmers idle around town instead of working the fields, villagers look worried.
2. **Rumours.** Locals reveal the lumberjacks vanished from the logging camp a few days ago. A wanted poster for Skullaplitta, an orc with a mean reputation, hangs in town.
3. **The ask.** The innkeeper's husband is one of the missing. She asks the party to investigate.
4. **Travel.** The logging camp is 2 hexes from town. Each outdoor transition can trigger a random encounter.
5. **The camp.** Abandoned, with pools of dried blood. Investigating reveals a trail into the forest.
6. **The trail.** It leads 2 more hexes to a cave where Skullaplitta and his goblins are camped.
7. **The cave.** Defeat the goblins and Skullaplitta, and free the lumberjacks held prisoner inside.
8. **Escort home.** The freed lumberjacks follow the party back to town, through the same hexes and encounter rolls. On arrival: reunion at the inn, reward, and the village switches from bleak to lively.

**Zones**

&#91;embedded content: quest zone chain · 6 zones\]

Each outdoor arrow is one hex crossing at 3 hours; the camp-to-forest exit stays hidden until the trail is found. The party escorts the lumberjacks home through the same exits in reverse.

**Quest flags**

| Flag | Set when | Effect |
| --- | --- | --- |
| `heard_lumberjack_rumour` | Talking to any worried villager | Innkeeper's dialogue unlocks the request |
| `lumberjack_quest_started` | Accepting the innkeeper's request | Journal entry added |
| `camp_trail_found` | Inspecting the clues at the camp | Trail exit to the forest hex appears |
| `skullaplitta_dead` | Skullaplitta killed | Wanted poster bounty can be claimed |
| `lumberjacks_freed` | Releasing the prisoners | Lumberjacks start following the party as escorts |
| `lumberjack_quest_done` | Bringing the lumberjacks back to the village | Village switches to its lively state; reward |

**Investigation at the camp**

- The blood pools and the tracks into the forest are clickable clue objects. Each adds a line to the combat log describing what the party sees.
- Inspecting the tracks sets `camp_trail_found` and reveals the trail exit. Later, this can become a Wisdom (Survival) check where failure costs time rather than blocking progress.

**Escorting the lumberjacks**

- Once freed, the lumberjacks become escort NPCs: they follow the party and travel with it through exits, but can't be selected or given orders.
- They don't fight. When enemies are near they keep their distance from the fight, and enemies can target them.
- Every outdoor transition on the way home still rolls for an encounter, so the return trip carries real risk.
- Arriving in the village with them sets `lumberjack_quest_done`.
- If all the lumberjacks die, the quest fails and no reward is given. What happens when only some survive is deferred, along with the wider question of how to manage quest outcome states.

**Encounters**

- Road and forest hexes roll on their terrain's encounter table on transition.
- Cave mouth: goblin lookouts, using SRD goblin stats.
- Cave interior: Skullaplitta and the main goblin group. Build Skullaplitta from the SRD orc with boosted HP so he works as a boss.
- The prisoners are held in a pen or cage that opens once the fight is over.

**Open questions for this quest**

- How many goblins, and Skullaplitta's final stats?
- Who pays the bounty, and how much?
- Is fighting the only way in for v1, or can the party sneak or talk?
- Do the in-between road and forest hexes get full zones, or only a transition with an encounter roll?
- Does the cave link onward to the artifact plot or a faction?

## Technical architecture

Godot 4.x, GDScript, 2D isometric TileMapLayer nodes, with a persistent `Main` scene that swaps zone scenes in and out.

**Scene tree**

```text
Main (persistent)
├── ZoneContainer        ← current zone scene loaded here
├── Party                ← party characters, reparented into the zone on load
├── CameraRig
├── SelectionController  ← mouse input, box select, issuing orders
└── UI (CanvasLayer)
    ├── Portraits
    ├── CombatLog
    └── PauseIndicator
```

**Autoloads (singletons)**

- `GameState`: story flags, faction reputation, game clock, current zone id.
- `Rules`: dice and 5e resolution functions, seeded RNG.
- `ZoneManager`: loads/unloads zones, places party at entry points, applies travel time, rolls encounters.
- `PauseManager`: owns pause state; game nodes respect it via `process_mode`.
- `SaveManager`: serialise and restore everything above.

**Character scene**

`Character` (CharacterBody2D) with child nodes: Sprite/AnimatedSprite2D (8-direction isometric), NavigationAgent2D, SelectionRing, HealthBar, and a script holding a `CharacterStats` resource plus the AI state machine. Party members and monsters share this scene; a flag or controller component decides player vs AI.

**Zone scene**

Each zone: TileMapLayers (ground, objects), NavigationRegion2D, `EntryPoint` markers (named), `Exit` Area2Ds (target zone id, target entry point, travel hours), spawn points, and a `ZoneData` resource.

**Save system (build early)**

- Save = `GameState` + party stats/HP/positions + per-zone state (dead enemies, opened chests, removed items) keyed by zone id.
- Zones re-read their saved state on load, so revisiting a cleared zone keeps it cleared.
- Store as JSON or a `.tres` in `user://saves/`.

**Suggested folders**

```text
res://
  autoload/   characters/   zones/   data/   ui/   art/
  data/weapons/  data/monsters/  data/zones/  data/factions/  data/quests/
```

## Milestones

Build in small, playable steps. Each milestone ends with something you can run and test. Placeholder art (coloured diamonds and capsules) is fine until milestone 5.

**M1 — Movement prototype (start here)**

- [ ] One isometric test zone with a TileMapLayer and baked navigation
- [ ] Two party characters; click to select, shift-click, box select
- [ ] Click-to-move with pathfinding and avoidance
- [ ] Portrait column with click to select
- [ ] Camera pan and clamp

**M2 — Combat prototype**

- [ ] `Rules` autoload with dice rolling and attack resolution
- [ ] One enemy type using `MonsterData`
- [ ] Click enemy to attack; cooldown-based attacks
- [ ] Idle AI: attack nearest enemy; enemies attack nearest party member
- [ ] HP bars, death/down, floating text, combat log
- [ ] Space to pause; orders while paused

**M3 — Zone graph**

- [ ] Second zone plus one interior; exits and entry points
- [ ] `ZoneManager` transitions, game clock, travel time
- [ ] Zone state persists when leaving and returning

**M4 — Save/load and game state**

- [ ] `GameState` flags and faction reputation
- [ ] Save and load from a menu

**M5 — Vertical slice**

- [ ] The village and two or three surrounding hexes, real art style
- [ ] The Missing Lumberjacks quest with simple dialogue, start to finish
- [ ] One faction reacting to the quest outcome

## Open questions

These don't block M1–M2 but should be settled before M5.

- **Party size and makeup:** fixed party of 2–4, or companions recruited in the village?
- **Artifact order:** can either artifact be found first, or is one gated behind the other?
- **Endings:** how many, and does each faction get one?
- **Zone scale:** how many screens per outdoor zone; which of the 100 hexes become full zones vs merged or skipped?
- **Party transitions:** must all members gather at an exit, or does reaching it with one member move everyone?
- **Death:** keep "down, recover after combat", or add 5e death saves and resurrection?
- **Art source:** commissioned, asset packs, or self-made? This sets the isometric tile size (e.g. 64x32) early.
- **Rulesets and licensing:** if 5e text or monsters are used verbatim, stick to the SRD and include its attribution; avoid D&D trademarks.

**Deferred features (rough priority)**

1. Fog of war and line of sight
2. Spells and class features
3. Inventory, loot and shops
4. Levelling and XP
5. Day/night cycle and resting rules
6. Travel map for fast travel
7. Formations
8. Survival mechanics
