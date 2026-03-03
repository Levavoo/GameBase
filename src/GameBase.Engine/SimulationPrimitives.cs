namespace GameBase.Engine;

public enum Team
{
    Player,
    Enemy,
    Neutral
}

public enum TerrainType
{
    Plain,
    Swamp,
    Water,
    Rocky
}

public enum ActionType
{
    Wait,
    Move,
    Attack,
    UseSkill
}

public enum DamageDomain
{
    Physical,
    Fire,
    Cold,
    Lightning,
    Poison,
    Arcane,
    Custom
}

public readonly record struct DamageTypeId(DamageDomain Domain, string Variant)
{
    public override string ToString() => $"{Domain}:{Variant}";

    public static DamageTypeId Physical(string variant) => new(DamageDomain.Physical, variant);
    public static DamageTypeId Elemental(DamageDomain domain, string variant = "base") => new(domain, variant);
    public static DamageTypeId Custom(string variant) => new(DamageDomain.Custom, variant);
}

public static class DamageTypes
{
    public static readonly DamageTypeId Strike = DamageTypeId.Physical("strike");
    public static readonly DamageTypeId Slash = DamageTypeId.Physical("slash");
    public static readonly DamageTypeId Pierce = DamageTypeId.Physical("pierce");
    public static readonly DamageTypeId Crush = DamageTypeId.Physical("crush");

    public static readonly DamageTypeId Fire = DamageTypeId.Elemental(DamageDomain.Fire);
    public static readonly DamageTypeId Cold = DamageTypeId.Elemental(DamageDomain.Cold);
    public static readonly DamageTypeId Lightning = DamageTypeId.Elemental(DamageDomain.Lightning);
    public static readonly DamageTypeId Poison = DamageTypeId.Elemental(DamageDomain.Poison);
    public static readonly DamageTypeId Arcane = DamageTypeId.Elemental(DamageDomain.Arcane);
}

public enum UnitSizeClass
{
    Small,
    Normal,
    Large,
    Huge,
    Colossal
}

public static class UnitSizeRules
{
    public static UnitSizeClass Classify(int occupiedSurfaceCells) => occupiedSurfaceCells switch
    {
        <= 6 => UnitSizeClass.Small,
        <= 16 => UnitSizeClass.Normal,
        <= 25 => UnitSizeClass.Large,
        <= 36 => UnitSizeClass.Huge,
        _ => UnitSizeClass.Colossal
    };

    public static int MaxCells(UnitSizeClass size) => size switch
    {
        UnitSizeClass.Small => 6,
        UnitSizeClass.Normal => 16,
        UnitSizeClass.Large => 25,
        UnitSizeClass.Huge => 36,
        UnitSizeClass.Colossal => int.MaxValue,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };
}

public readonly record struct Position3(int X, int Y, int Height)
{
    public static int ManhattanDistance(Position3 a, Position3 b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Height - b.Height);

    public Position3 StepTowards(Position3 target)
    {
        var dx = Math.Sign(target.X - X);
        var dy = Math.Sign(target.Y - Y);
        var dh = Math.Sign(target.Height - Height);
        return new Position3(X + dx, Y + dy, Height + dh);
    }
}

public sealed class DamageProfile
{
    private readonly Dictionary<DamageTypeId, int> _values = [];

    public DamageProfile Add(DamageTypeId type, int value)
    {
        if (_values.TryGetValue(type, out var current))
        {
            _values[type] = current + value;
        }
        else
        {
            _values[type] = value;
        }

        return this;
    }

    public int this[DamageTypeId type] => _values.GetValueOrDefault(type);
    public IEnumerable<KeyValuePair<DamageTypeId, int>> Entries => _values;
}

public sealed record CoreStats(
    int MaxHealth,
    int MaxMana,
    int Balance,
    int HealthRegen,
    int ManaRegen,
    int AttackSpeed,
    int MovementSpeed,
    int AttackRange,
    DamageProfile Damage,
    DamageProfile Defense);

public sealed record StatusEffect(
    string Name,
    int DurationTicks,
    int AttackSpeedDelta = 0,
    int MovementSpeedDelta = 0,
    int BalanceDelta = 0,
    int DotDamage = 0,
    DamageTypeId? DotType = null);

public sealed record ActionIntent(ActionType Type, int ActorId, int? TargetId = null, Position3? Destination = null);

public sealed record EventLogEntry(int Tick, string Message);
