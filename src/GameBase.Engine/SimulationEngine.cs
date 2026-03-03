namespace GameBase.Engine;

public sealed class SimulationConfig
{
    public int MaxTicks { get; init; } = 300;
    public int RandomSeed { get; init; } = 1337;
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
            var aliveTeams = mission.AliveCharacters.Select(x => x.Team).Distinct().Count();
            if (aliveTeams <= 1)
            {
                mission.Log.Add(new EventLogEntry(tick, "Mission ended: one team remains."));
                return mission;
            }

            ProcessEffects(mission, tick);
            var intents = CollectIntents(mission);
            ResolveIntents(mission, tick, intents);
        }

        mission.Log.Add(new EventLogEntry(_config.MaxTicks, "Mission ended: tick limit reached."));
        return mission;
    }

    private void ProcessEffects(MissionState mission, int tick)
    {
        foreach (var character in mission.AliveCharacters.ToArray())
        {
            foreach (var message in character.TickEffects())
            {
                mission.Log.Add(new EventLogEntry(tick, message));
            }
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
            .OrderByDescending(x => x.actor!.EffectiveSpeed)
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
        mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} moves to ({actor.Position.X},{actor.Position.Y},{actor.Position.Height})."));
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
        if (distance > actor.BaseStats.Range)
        {
            mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} attack is out of range."));
            return;
        }

        var attackerTile = mission.Map.GetTile(actor.Position);
        var defenderTile = mission.Map.GetTile(target.Position);
        var highGroundBonus = Math.Max(0, actor.Position.Height - target.Position.Height);

        var raw = actor.EffectiveAttack + attackerTile.AttackModifier + highGroundBonus;
        var mitigated = Math.Max(1, raw - (target.EffectiveDefense + defenderTile.AttackModifier));
        target.Health = Math.Max(0, target.Health - mitigated);

        mission.Log.Add(new EventLogEntry(tick, $"{actor.Name} hits {target.Name} for {mitigated} damage ({target.Health}/{target.BaseStats.MaxHealth})."));

        var targetSurface = defenderTile.Terrain;
        if (targetSurface == TerrainType.Swamp && target.IsAlive)
        {
            target.AddEffect(new StatusEffect("Swamp Rot", 3, SpeedDelta: -1, DotDamage: 1));
            mission.Log.Add(new EventLogEntry(tick, $"{target.Name} is afflicted by Swamp Rot."));
        }
    }
}
