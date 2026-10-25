# Naywich CRPG

A hobby CRPG built in Godot 4, ported from a 10x10 D&D 5e hexcrawl. It aims for the look and feel of Baldur's Gate 1: 2D isometric, a party controlled RTS-style, and real-time-with-pause combat.

The party arrives at a village on the border of a kingdom, resolves a few local problems, and is drawn into a hunt for two powerful artifacts that rival factions also want. Players never see the hexmap. They walk a graph of connected zones, including interiors and dungeons.

## Status

Pre-production. No code yet. The first milestone is the movement prototype (M1).

## Design

The full design is in [Border Hexcrawl CRPG — Design Doc.md](<Border Hexcrawl CRPG — Design Doc.md>). It covers pillars and scope, world structure, party control, combat, the data model, narrative, the opening quest, technical architecture and milestones.

## Tech

- Godot 4.x, GDScript
- 2D isometric `TileMapLayer` nodes, with a persistent `Main` scene that swaps zone scenes
- Game data stored as Godot Resources (`.tres`)

## Milestones

1. **M1:** Movement prototype
2. **M2:** Combat prototype
3. **M3:** Zone graph
4. **M4:** Save/load and game state
5. **M5:** Vertical slice (The Missing Lumberjacks)

## Licensing note

Any 5e rules text or monsters used verbatim come from the SRD and include its attribution. The project avoids D&D trademarks.
