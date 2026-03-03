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

public sealed record CombatStats(int MaxHealth, int AttackPower, int Defense, int Range, int Speed);

public sealed record StatusEffect(
    string Name,
    int DurationTicks,
    int AttackDelta = 0,
    int DefenseDelta = 0,
    int SpeedDelta = 0,
    int DotDamage = 0);

public sealed record ActionIntent(ActionType Type, int ActorId, int? TargetId = null, Position3? Destination = null);

public sealed record EventLogEntry(int Tick, string Message);
