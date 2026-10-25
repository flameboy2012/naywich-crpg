# Naywich CRPG

A hobby CRPG built in Godot 4, ported from a 10x10 D&D 5e hexcrawl. It aims for the look and feel of Baldur's Gate 1: 2D isometric, a party controlled RTS-style, and real-time-with-pause combat.

The party arrives at a village on the border of a kingdom, resolves a few local problems, and is drawn into a hunt for two powerful artifacts that rival factions also want. Players never see the hexmap. They walk a graph of connected zones, including interiors and dungeons.

## Status

Pre-production. No code yet. The first milestone is the movement prototype (M1).

## Design

The full design is in [Border Hexcrawl CRPG — Design Doc.md](<Border Hexcrawl CRPG — Design Doc.md>). It covers pillars and scope, world structure, party control, combat, the data model, narrative, the opening quest, technical architecture and milestones.

Domain terms are defined in [GLOSSARY.md](GLOSSARY.md). Architecture decisions are recorded in [docs/adr/](docs/adr/).

## Tech

- Godot 4.7 (.NET build), C#. This is a provisional choice, reviewed at the end of M1.5 ([ADR 0002](docs/adr/0002-csharp-over-gdscript.md))
- Pure rules logic in a plain .NET library (`Crpg.Core`), tested with xUnit
- 64x32 isometric tiles, with one tile equal to 5 ft ([ADR 0001](docs/adr/0001-isometric-tile-scale.md))
- 2D isometric `TileMapLayer` nodes, with a persistent `Main` scene that swaps zone scenes
- Game data stored as Godot Resources (`.tres`)

## Milestones

- **M1:** Movement prototype (one party member, static camera)
- **M1.5:** Party control (selection, portraits, camera)
- **M2:** Combat prototype
- **M3:** Zone graph
- **M4:** Save/load and game state
- **M5:** Vertical slice (The Missing Lumberjacks)

## Licensing note

Any 5e rules text or monsters used verbatim come from the SRD and include its attribution. The project avoids D&D trademarks.
