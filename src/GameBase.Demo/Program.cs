using GameBase.Engine;

static CoreStats BuildStats(int hp, int mana, int balance, int atkSpeed, int moveSpeed, int range, params (DamageTypeId type, int damage, int defense)[] profile)
{
    var damage = new DamageProfile();
    var defense = new DamageProfile();

    foreach (var (type, attackValue, defenseValue) in profile)
    {
        damage.Add(type, attackValue);
        defense.Add(type, defenseValue);
    }

    return new CoreStats(
        MaxHealth: hp,
        MaxMana: mana,
        Balance: balance,
        HealthRegen: 1,
        ManaRegen: 1,
        AttackSpeed: atkSpeed,
        MovementSpeed: moveSpeed,
        AttackRange: range,
        Damage: damage,
        Defense: defense);
}

var map = new BattleMap(12, 8, (x, y) =>
{
    var terrain = TerrainType.Plain;
    var moveCost = 1d;
    var attackMod = 0;
    var h = (x + y) % 3;

    if (y is 3 or 4 && x is > 2 and < 9)
    {
        terrain = TerrainType.Swamp;
        moveCost = 1.3;
        attackMod = -1;
    }
    else if (x is 10 or 11)
    {
        terrain = TerrainType.Rocky;
        attackMod = 1;
        h += 1;
    }

    return new Tile
    {
        Terrain = terrain,
        MoveCost = moveCost,
        AttackModifier = attackMod,
        Height = h
    };
});

var vanguardStats = BuildStats(30, 10, 8, 5, 4, 1,
    (DamageTypes.Strike, 4, 2),
    (DamageTypes.Slash, 4, 2),
    (DamageTypes.Fire, 1, 1));

var arbalistStats = BuildStats(20, 16, 5, 6, 5, 3,
    (DamageTypes.Pierce, 7, 1),
    (DamageTypes.Cold, 1, 2));

var ghoulStats = BuildStats(18, 6, 4, 5, 4, 1,
    (DamageTypes.Crush, 5, 1),
    (DamageTypes.Poison, 1, 1));

var shamanStats = BuildStats(22, 20, 6, 5, 4, 2,
    (DamageTypes.Fire, 3, 0),
    (DamageTypes.Arcane, 3, 1),
    (DamageTypes.Strike, 1, 1));

var units = new List<Character>
{
    new() { Id = 1, Name = "Vanguard-01", Team = Team.Player, BaseStats = vanguardStats, Health = vanguardStats.MaxHealth, Mana = vanguardStats.MaxMana, Balance = vanguardStats.Balance, OccupiedSurfaceCells = 12, Position = new Position3(1, 2, map.GetTile(new Position3(1,2,0)).Height) },
    new() { Id = 2, Name = "Arbalist-02", Team = Team.Player, BaseStats = arbalistStats, Health = arbalistStats.MaxHealth, Mana = arbalistStats.MaxMana, Balance = arbalistStats.Balance, OccupiedSurfaceCells = 6, Position = new Position3(1, 5, map.GetTile(new Position3(1,5,0)).Height) },
    new() { Id = 101, Name = "Ghoul-A", Team = Team.Enemy, BaseStats = ghoulStats, Health = ghoulStats.MaxHealth, Mana = ghoulStats.MaxMana, Balance = ghoulStats.Balance, OccupiedSurfaceCells = 9, Position = new Position3(10, 2, map.GetTile(new Position3(10,2,0)).Height) },
    new() { Id = 102, Name = "Ghoul-B", Team = Team.Enemy, BaseStats = ghoulStats, Health = ghoulStats.MaxHealth, Mana = ghoulStats.MaxMana, Balance = ghoulStats.Balance, OccupiedSurfaceCells = 9, Position = new Position3(10, 5, map.GetTile(new Position3(10,5,0)).Height) },
    new() { Id = 103, Name = "Shaman", Team = Team.Enemy, BaseStats = shamanStats, Health = shamanStats.MaxHealth, Mana = shamanStats.MaxMana, Balance = shamanStats.Balance, OccupiedSurfaceCells = 18, Position = new Position3(9, 3, map.GetTile(new Position3(9,3,0)).Height) }
};

var brains = units.ToDictionary(c => c.Id, _ => (ICharacterAi)new BasicCombatAi());
var state = new MissionState(map, units);
var config = new SimulationConfig { MaxTicks = 80, RandomSeed = 42, TickRateHz = 10 };
var engine = new SimulationEngine(brains, config);
var result = await engine.RunRealtimeAsync(state);

Console.WriteLine($"=== Mission Log ({config.TickRateHz} ticks/sec) ===");
foreach (var entry in result.Log)
{
    Console.WriteLine($"[{entry.Tick:000}] {entry.Message}");
}

Console.WriteLine();
Console.WriteLine("=== Survivors ===");
foreach (var survivor in result.AliveCharacters.OrderBy(c => c.Team).ThenBy(c => c.Id))
{
    Console.WriteLine($"{survivor.Team,-6} | {survivor.Name,-12} | Size {survivor.SizeClass,-8} | HP {survivor.Health}/{survivor.BaseStats.MaxHealth} | MP {survivor.Mana}/{survivor.BaseStats.MaxMana} | Pos ({survivor.Position.X},{survivor.Position.Y},{survivor.Position.Height})");
}
