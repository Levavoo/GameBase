using System.Diagnostics;

namespace GameBase.Engine;

public sealed class SimulationConfig
{
    public int MaxTicks { get; init; } = 300;
    public int RandomSeed { get; init; } = 1337;
    public int TickRateHz { get; init; } = 10;

    public TimeSpan TickInterval =>
        TickRateHz <= 0
            ? throw new InvalidOperationException("TickRateHz must be > 0.")
            : TimeSpan.FromSeconds(1d / TickRateHz);
}

public sealed class SimulationEngine
{
    private readonly Dictionary<int, ICharacterAi> _brains;
    private readonly SimulationConfig _config;
    private readonly Random _random;

    public SimulationEngine(Dictionary<int, ICharacterAi> brains, SimulationConfig config)
    {
        _brains = brains;
        _config = config;
        _random = new Random(config.RandomSeed);
    }

    public MissionState Run(MissionState mission)
    {
        for (var tick = 1; tick <= _config.MaxTicks; tick++)
        {
            if (!Step(mission, tick))
            {
                return mission;
            }
        }

        mission.Log.Add(new EventLogEntry(_config.MaxTicks, "Mission ended: tick limit reached."));
        return mission;
    }

    public async Task<MissionState> RunRealtimeAsync(MissionState mission, CancellationToken cancellationToken = default)
    {
        var tickInterval = _config.TickInterval;

        for (var tick = 1; tick <= _config.MaxTicks; tick++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedAt = Stopwatch.GetTimestamp();

            if (!Step(mission, tick))
            {
                return mission;
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var remaining = tickInterval - elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }
        }

        mission.Log.Add(new EventLogEntry(_config.MaxTicks, "Mission ended: tick limit reached."));
        return mission;
    }

    private bool Step(MissionState mission, int tick)
    {
        var aliveTeams = mission.AliveCharacters.Select(x => x.Team).Distinct().Count();
        if (aliveTeams <= 1)
        {
            mission.Log.Add(new EventLogEntry(tick, "Mission ended: one team remains."));
            return false;
        }

        ProcessEffectsAndRegen(mission, tick);
        var intents = CollectIntents(mission);
        ResolveIntents(mission, tick, intents);

        return true;
    }

    private void ProcessEffectsAndRegen(MissionState mission, int tick)
    {
        foreach (var character in mission.AliveCharacters.ToArray())
        {
            foreach (var message in character.TickEffects())
            {
                mission.Log.Add(new EventLogEntry(tick, message));
            }

            character.Regenerate();
        }
    }

    private List<ActionIntent> CollectIntents(MissionState mission)
    {
        var intents = new List<ActionIntent>(mission.AliveCharacters.Count());
        foreach (var character in mission.AliveCharacters)
        {
            if (!_brains.TryGetValue(character.Id, out var brain))
            {
                intents.Add(new ActionIntent(ActionType.Wait, character.Id));
                continue;
            }

            intents.Add(brain.Decide(character, mission));
        }

        return intents;
    }

    private void ResolveIntents(MissionState mission, int tick, List<ActionIntent> intents)
    {
        var ordered = intents
            .Select(intent => (intent, actor: mission.GetCharacter(intent.ActorId)))
            .Where(x => x.actor is { IsAlive: true })
            .OrderByDescending(x => x.actor!.EffectiveAttackSpeed)
            .ThenBy(_ => _random.Next())
            .ToArray();

        foreach (var (intent, actor) in ordered)
        {
            ExecuteIntent(mission, tick, actor!, intent);
        }
    }

    private void ExecuteIntent(MissionState mission, int tick, Character actor, ActionIntent intent)
    {
        switch (intent.Type)
        {
            case ActionType.Move:
                ExecuteMove(mission, tick, actor, intent.Destination);
                break;
            case ActionType.Attack:
                ExecuteAttack(mission, tick, actor, intent.TargetId);
                break;
            default:
                mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} waits."));
                break;
        }
    }

    private static void ExecuteMove(MissionState mission, int tick, Character actor, Position3? destination)
    {
        if (destination is null || !mission.Map.Contains(destination.Value))
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} failed to move."));
            return;
        }

        var occupied = mission.AliveCharacters.Any(x => x.Id != actor.Id && x.Position.X == destination.Value.X && x.Position.Y == destination.Value.Y);
        if (occupied)
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} could not move, tile occupied."));
            return;
        }

        actor.Position = new Position3(destination.Value.X, destination.Value.Y, mission.Map.GetTile(destination.Value).Height);
        mission.Log.Add(new EventLogEntry(tick, $"{actor.Name}({actor.SizeClass}) moves to ({actor.Position.X},{actor.Position.Y},{actor.Position.Height})."));
    }

    private static void ExecuteAttack(MissionState mission, int tick, Character actor, int? targetId)
    {
        if (targetId is null)
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} fails to attack (no target)."));
            return;
        }

        var target = mission.GetCharacter(targetId.Value);
        if (target is null || !target.IsAlive)
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} fails to attack (invalid target)."));
            return;
        }

        var distance = Position3.ManhattanDistance(actor.Position, target.Position);
        if (distance > actor.BaseStats.AttackRange)
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} attack is out of range."));
            return;
        }

        var attackerTile = mission.Map.GetTile(actor.Position);
        var defenderTile = mission.Map.GetTile(target.Position);
        var highGroundBonus = Math.Max(0, actor.Position.Height - target.Position.Height);

        var totalDamage = ComputeMitigatedDamage(actor.BaseStats.Damage, target.BaseStats.Defense, attackerTile.AttackModifier + highGroundBonus, defenderTile.AttackModifier);
        target.Health = Math.Max(0, target.Health - totalDamage);

        mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} hits {target.Name} for {totalDamage} mixed damage ({target.Health}/{target.BaseStats.MaxHealth})."));

        if (defenderTile.Terrain == TerrainType.Swamp && target.IsAlive)
        {
            target.AddEffect(new StatusEffect("Swamp Rot", 3, MovementSpeedDelta: -1, DotDamage: 1, DotType: DamageTypes.Poison));
            mission.Log.Add(new EventLogEntry(tick, $"{target.Name} is afflicted by Swamp Rot."));
        }
    }

    private static int ComputeMitigatedDamage(DamageProfile incoming, DamageProfile defense, int attackFlatMod, int defenseFlatMod)
    {
        var total = 0;
        foreach (var (type, value) in incoming.Entries)
        {
            var raw = Math.Max(0, value + attackFlatMod);
            var reduced = Math.Max(0, raw - (defense[type] + defenseFlatMod));
            total += reduced;
        }

        return Math.Max(1, total);
    }
}
