using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class BloodPriest : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit? highestHealth = units
        .Where(u => u.Side == Side)
        .OrderByDescending(u => (float)u.Health / u.MaxHealth)
        .ThenByDescending(u => u.MaxHealth)
        .FirstOrDefault();

    if (highestHealth == null) return [];

    Unit? lowestHealth = units
        .Where(u => u.Side == Side && u != highestHealth && u.Health < u.MaxHealth)
        .OrderBy(u => (float)u.Health / u.MaxHealth)
        .ThenByDescending(u => u.MaxHealth)
        .FirstOrDefault();

    if (lowestHealth == null) return [];

    return [highestHealth.OccupiedMainCell, lowestHealth.OccupiedMainCell];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    if (targets.Count < 2) return;

    Unit sacrifice = unitsGrid[targets[0].X, targets[0].Y];
    Unit receiver = unitsGrid[targets[1].X, targets[1].Y];

    if (sacrifice == null || receiver == null) return;

    int damageDealt = sacrifice.ChangeHealth(-Damage, this);
    receiver.ChangeHealth(Damage * 3, this);
  }
}
