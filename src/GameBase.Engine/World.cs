namespace GameBase.Engine;

public sealed class Tile
{
    public required TerrainType Terrain { get; init; }
    public required int Height { get; init; }
    public double MoveCost { get; init; } = 1;
    public int AttackModifier { get; init; } = 0;
}

public sealed class BattleMap
{
    private readonly Tile[,] _tiles;

    public BattleMap(int width, int height, Func<int, int, Tile> generator)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                _tiles[x, y] = generator(x, y);
            }
        }
    }

    public int Width { get; }
    public int Height { get; }

    public bool Contains(Position3 position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    public Tile GetTile(Position3 position) => _tiles[position.X, position.Y];
}

public sealed class Character
{
    private readonly List<StatusEffect> _effects = [];

    public required int Id { get; init; }
    public required string Name { get; init; }
    public required Team Team { get; init; }
    public required CombatStats BaseStats { get; init; }
    public required Position3 Position { get; set; }
    public int Health { get; set; }
    public bool IsAlive => Health > 0;
    public IReadOnlyList<StatusEffect> Effects => _effects;

    public int EffectiveAttack => BaseStats.AttackPower + _effects.Sum(x => x.AttackDelta);
    public int EffectiveDefense => BaseStats.Defense + _effects.Sum(x => x.DefenseDelta);
    public int EffectiveSpeed => Math.Max(1, BaseStats.Speed + _effects.Sum(x => x.SpeedDelta));

    public void AddEffect(StatusEffect effect) => _effects.Add(effect);

    public IEnumerable<string> TickEffects()
    {
        for (var i = _effects.Count - 1; i >= 0; i--)
        {
            var effect = _effects[i];
            if (effect.DotDamage > 0)
            {
                Health = Math.Max(0, Health - effect.DotDamage);
                yield return $"{Name} takes {effect.DotDamage} damage from {effect.Name}.";
            }

            var next = effect with { DurationTicks = effect.DurationTicks - 1 };
            if (next.DurationTicks <= 0)
            {
                _effects.RemoveAt(i);
                yield return $"{Name} loses effect {effect.Name}.";
            }
            else
            {
                _effects[i] = next;
            }
        }
    }
}

public sealed class MissionState
{
    public MissionState(BattleMap map, IReadOnlyList<Character> characters)
    {
        Map = map;
        Characters = characters.ToDictionary(x => x.Id);
    }

    public BattleMap Map { get; }
    public Dictionary<int, Character> Characters { get; }
    public List<EventLogEntry> Log { get; } = [];

    public IEnumerable<Character> AliveCharacters => Characters.Values.Where(x => x.IsAlive);
    public Character? GetCharacter(int id) => Characters.GetValueOrDefault(id);
}
