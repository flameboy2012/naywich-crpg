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

**Down**:
A party member at 0 HP. They stay on the field, cannot be selected or ordered, are ignored by enemies, and get up at 1 HP at combat end.
_Avoid_: KO, unconscious, dead

**Dead**:
An enemy at 0 HP. Its body stays where it fell but takes no further part.
_Avoid_: Killed, destroyed

## Combat

**Attack**:
One way a character can attack, such as a weapon or a cantrip. Each has an ability, damage dice, damage type and range.
_Avoid_: Ability, move, skill

**Attack cooldown**:
The time a character must wait between attacks: one 5e round, 6 real seconds at normal combat speed.
_Avoid_: Attack timer, swing timer, round

**Awareness radius**:
How far, in tiles, a character notices an enemy.
_Avoid_: Aggro range, sight range

**Combat end**:
The moment no living enemy in the zone is aware of the party. There is no separate combat mode.
_Avoid_: Battle over, encounter end

**Combat log**:
The scrolling text record of every attack roll, damage roll and result.
_Avoid_: Console, chat log

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

**Portrait slot**:
A party member's fixed position, 1 to 4, in the portrait column. Number keys select by portrait slot.
_Avoid_: Index, hotkey slot

## Camera

**Pan**:
Moving the camera across the zone without moving any party member.
_Avoid_: Scroll

**Zone bounds**:
The rectangle the camera may show: the zone plus a small margin.
_Avoid_: Map edge, level bounds

## Space

**Tile**:
One 5 ft square of the game world, matching one square on a 5e battle map.
_Avoid_: Square, cell
