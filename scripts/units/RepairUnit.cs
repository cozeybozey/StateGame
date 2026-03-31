using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RepairUnit : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit? lowestHealthMachine = units
        .Where(u => u.Side == Side && u.Types.Contains("machinery"))
        .OrderBy(u => (float)u.Health / u.MaxHealth)
        .ThenByDescending(u => u.MaxHealth)
        .FirstOrDefault();

    if (lowestHealthMachine == null || lowestHealthMachine.Health >= lowestHealthMachine.MaxHealth)
      return [];
    else
      return [lowestHealthMachine.OccupiedMainCell];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(Damage, this);
      }
    }
  }
}
