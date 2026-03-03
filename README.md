# GameBase

Initial scalable skeleton for an AI-driven, Diablo-like party game where the player focuses on stash, loadout, and party composition.

## Current scope (foundation)

- **Headless / log-first simulation** (easy to test, replay, and port to any engine).
- **Tick-based combat loop** with deterministic seed support.
- **Many autonomous units** via per-character AI controllers.
- **Concurrent intent resolution** by speed and initiative order.
- **Status effects** (buff/debuff with duration and damage-over-time hooks).
- **Semi-3D positioning** through tile height (`x, y, height`) and high-ground bonuses.
- **Surface modifiers** (example: swamp applies a debuff on hit).

This foundation is intentionally gameplay-light but architecture-heavy, so future systems (stats growth, itemization, skills, movement cost, pathing, and mission scripting) can be added without rewriting the core loop.

## Project structure

- `src/GameBase.Engine`: reusable simulation engine (domain + loop + AI interface).
- `src/GameBase.Demo`: console demo that runs one mission and prints the combat log.

## Run

```bash
dotnet run --project src/GameBase.Demo
```

## Next planned modules

1. Rich stat model + derived formulas.
2. Skill system + cooldown/resource economy.
3. Better movement/pathing for large unit counts.
4. Party composition & equipment layer (player meta-control).
5. Mission generator with objectives and biome rules.
