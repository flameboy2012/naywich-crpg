# Combat runs in real seconds, separate from the game clock

The game clock runs at 1 real second = 1 game minute, for day/night and timed events. Combat does not use that clock. A 5e round is 6 real seconds at normal combat speed: each character attacks at most once every 6 seconds and moves 1 tile per second, as ADR 0001 already assumed. Read literally, "one round = 6 game seconds" on the game clock would make a round last 0.1 real seconds, so we chose real seconds to keep combat timing simple and readable.

## Consequences

- Attack cooldowns and movement speeds are in real seconds, scaled by the global combat speed setting. That setting is the project setting `crpg/combat/combat_speed` (default 1.0): in the editor, Project > Project Settings > Crpg > Combat (turn on Advanced Settings if it is hidden). The `Rules` autoload reads it at start-up.
- The game clock keeps ticking during a fight at its own rate; a long fight advances it by minutes, not rounds.
