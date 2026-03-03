namespace GameBase.Engine;

public interface ICharacterAi
{
    ActionIntent Decide(Character self, MissionState mission);
}

public sealed class BasicCombatAi : ICharacterAi
{
    public ActionIntent Decide(Character self, MissionState mission)
    {
        if (!self.IsAlive)
        {
            return new ActionIntent(ActionType.Wait, self.Id);
        }

        var enemies = mission.AliveCharacters.Where(x => x.Team != self.Team).ToArray();
        if (enemies.Length == 0)
        {
            return new ActionIntent(ActionType.Wait, self.Id);
        }

        var target = enemies.MinBy(e => Position3.ManhattanDistance(self.Position, e.Position))!;
        var distance = Position3.ManhattanDistance(self.Position, target.Position);

        if (distance <= self.BaseStats.AttackRange)
        {
            return new ActionIntent(ActionType.Attack, self.Id, target.Id);
        }

        var nextStep = self.Position.StepTowards(target.Position);
        return mission.Map.Contains(nextStep)
            ? new ActionIntent(ActionType.Move, self.Id, Destination: nextStep)
            : new ActionIntent(ActionType.Wait, self.Id);
    }
}
