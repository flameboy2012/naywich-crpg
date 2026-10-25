# Border Hexcrawl CRPG

A party-based CRPG ported from a D&D 5e hexcrawl. The party explores a graph of zones around a border village, fights in real time with pause, and gets drawn into a hunt for two artifacts.

## Characters

**Character**:
Anyone in a zone that has stats: a party member, monster, NPC or escort.
_Avoid_: Unit, actor, entity

**Party member**:
A character the player controls.
_Avoid_: Unit, member, hero

**Monster**:
A type of creature defined by data, such as a goblin or an orc.
_Avoid_: Mob, creep

**Enemy**:
Any character currently hostile to the party. A monster or a faction NPC can be an enemy.
_Avoid_: Hostile, foe

## Control

**Selection**:
The set of party members currently receiving orders.
_Avoid_: Group, squad

**Order**:
A player command given to the selection, such as move, attack or interact.
_Avoid_: Command, action

**Box select**:
Selecting the party members inside a rectangle dragged across the screen.
_Avoid_: Marquee, drag select

**Spread slot**:
The end point given to one party member when a move order sends several party members to the same place, so they don't stack.
_Avoid_: Formation position, offset

**Move marker**:
A brief ring showing where a party member will actually stop after a move order.
_Avoid_: Cursor, waypoint

**Path line**:
The drawn route from a moving party member to their destination, shown whether or not they are selected.
_Avoid_: Trail, route preview

## Space

**Tile**:
One 5 ft square of the game world, matching one square on a 5e battle map.
_Avoid_: Square, cell
