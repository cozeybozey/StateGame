using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Lifebinder : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> injuredAllies = units
        .Where(u => u.Side == Side && u.Health < u.MaxHealth)
        .OrderBy(u => u.GetHealthPercentage())
        .ThenByDescending(u => u.MaxHealth)
        .ThenByDescending(u => Side ? u.OccupiedMainCell.Y : -u.OccupiedMainCell.Y)
        .ThenBy(u => u.OccupiedMainCell.X)
        .Take(3)
        .Select(u => u.OccupiedMainCell)
        .ToList();

    return injuredAllies;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    int heal = Damage; // starting heal amount
    foreach (Vector2I target in targets)
    {
      if (heal <= 0)
        break; // no more effective healing

      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && targetUnit.Health < targetUnit.MaxHealth)
      {
        targetUnit.ChangeHealth(heal, this);
        // Each subsequent unit receives 50% less (halve the heal)
        heal = Mathf.FloorToInt(heal * 0.5f);
      }
    }
  }
}
