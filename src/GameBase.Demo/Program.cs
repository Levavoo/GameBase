using GameBase.Engine;

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

var units = new List<Character>
{
    new() { Id = 1, Name = "Vanguard-01", Team = Team.Player, BaseStats = new CombatStats(30, 8, 3, 1, 5), Health = 30, Position = new Position3(1, 2, map.GetTile(new Position3(1,2,0)).Height) },
    new() { Id = 2, Name = "Arbalist-02", Team = Team.Player, BaseStats = new CombatStats(20, 7, 2, 3, 6), Health = 20, Position = new Position3(1, 5, map.GetTile(new Position3(1,5,0)).Height) },
    new() { Id = 101, Name = "Ghoul-A", Team = Team.Enemy, BaseStats = new CombatStats(18, 6, 2, 1, 5), Health = 18, Position = new Position3(10, 2, map.GetTile(new Position3(10,2,0)).Height) },
    new() { Id = 102, Name = "Ghoul-B", Team = Team.Enemy, BaseStats = new CombatStats(18, 6, 2, 1, 4), Health = 18, Position = new Position3(10, 5, map.GetTile(new Position3(10,5,0)).Height) },
    new() { Id = 103, Name = "Shaman", Team = Team.Enemy, BaseStats = new CombatStats(22, 5, 1, 2, 5), Health = 22, Position = new Position3(9, 3, map.GetTile(new Position3(9,3,0)).Height) }
};

var brains = units.ToDictionary(c => c.Id, _ => (ICharacterAi)new BasicCombatAi());
var state = new MissionState(map, units);
var engine = new SimulationEngine(brains, new SimulationConfig { MaxTicks = 80, RandomSeed = 42 });
var result = engine.Run(state);

Console.WriteLine("=== Mission Log ===");
foreach (var entry in result.Log)
{
    Console.WriteLine($"[{entry.Tick:000}] {entry.Message}");
}

Console.WriteLine();
Console.WriteLine("=== Survivors ===");
foreach (var survivor in result.AliveCharacters.OrderBy(c => c.Team).ThenBy(c => c.Id))
{
    Console.WriteLine($"{survivor.Team,-6} | {survivor.Name,-12} | HP {survivor.Health}/{survivor.BaseStats.MaxHealth} | Pos ({survivor.Position.X},{survivor.Position.Y},{survivor.Position.Height})");
}
