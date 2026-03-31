using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Sergeant : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();

    Unit? targetAlly = null;
    int bestCooldown = int.MinValue;

    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Unit unit = unitsGrid[x, y];
        if (unit != null && unit.Side == Side && unit != this)
        {
          if (unit.Cooldown > bestCooldown)
          {
            bestCooldown = unit.Cooldown;
            targetAlly = unit;
          }
        }
      }
    }

    if (targetAlly != null)
      result.Add(targetAlly.OccupiedMainCell);

    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets == null || targets.Count == 0)
      return;

    Vector2I target = targets[0];
    Unit ally = unitsGrid[target.X, target.Y];
    if (ally == null || ally.Side != Side)
      return;

    // Reduce the ally's cooldown by 1 (i.e. bring them closer to being able to act)
    ally.ChangeCooldown(-1);
  }
}
