# GameBase

Initial scalable skeleton for an AI-driven, Diablo-like party game where the player focuses on stash, loadout, and party composition.

## Current scope (foundation)

- **Headless / log-first simulation** (easy to test, replay, and port to any engine).
- **Fixed-step simulation loop** running at **10 ticks/second** by default, deterministic seed support, and no FPS coupling.
- **Many autonomous units** via per-character AI controllers.
- **Concurrent intent resolution** by attack speed and initiative order.
- **Semi-3D positioning** through tile height (`x, y, height`) and high-ground bonuses.
- **Surface modifiers** (example: swamp applies poison DoT + movement debuff).
- **Core stat model v1** with:
  - typed damage profiles (`physical` split into `strike/slash/pierce/crush`, plus elemental/custom),
  - typed defenses using the same type IDs,
  - balance, health/mana pools, health/mana regen,
  - attack speed and movement speed.
- **Unit size classes** by occupied surface area:
  - `Small <= 6`, `Normal <= 16`, `Large <= 25`, `Huge <= 36`, `Colossal > 36`.

This foundation is intentionally gameplay-light but architecture-heavy, so future systems (attributes, itemization, skills, advanced pathing, and mission scripting) can be added without rewriting the core loop.

## Project structure

- `src/GameBase.Engine`: reusable simulation engine (domain + loop + AI interface).
- `src/GameBase.Demo`: console demo that runs one mission and prints the combat log.

## Run

```bash
dotnet run --project src/GameBase.Demo
# demo uses RunRealtimeAsync at 10 ticks/sec by default
```

## Next planned modules

1. Attribute layer (`STR/AGI/INT`) that derives core stats.
2. Skill system + cooldown/resource economy.
3. Better movement/pathing for large unit counts + variable footprint collision.
4. Party composition & equipment layer (player meta-control).
5. Mission generator with objectives and biome rules.
