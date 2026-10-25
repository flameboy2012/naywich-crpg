# Isometric tiles are 64x32 pixels and one tile is 5 ft

We fixed the isometric tile size at 64x32 pixels before the art source was chosen, and made one tile equal one 5 ft square from 5e. Navigation baking, movement speeds, weapon ranges and all future art depend on this, so leaving it open until art was chosen would have meant reworking every zone later. With 1 tile = 5 ft, 5e numbers convert directly: 30 ft speed is 6 tiles per 6-second round (1 tile per second at normal combat speed), melee reach is 1 tile, and a 5e weapon range in feet divided by 5 gives its range in tiles.

## What "one tile" means as a distance

One tile of distance is one grid step: moving from tile (x, y) to (x + 1, y). On a 64x32 isometric layout that step is the vector (32, 16), about 35.78 px in world coordinates. It is not the tile's screen width (64 px) or height (32 px). Movement speed, ranges and awareness radii all use this grid-step length, at the same pixel speed in every direction. As a result, a character crossing the screen horizontally takes about 1.8 s per 64 px tile width, which looks slower than "one tile per second" if you count tiles across the screen. This was confirmed during M1 play-testing.

## Consequences

- Any art pack or commissioned art must target 64x32 tiles, or be rescaled to fit.
- The tile size and grid-step length are defined once in the game code; the speed setting is exported in tiles per second.
