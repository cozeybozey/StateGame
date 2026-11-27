using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Sergeant : Unit
{
  // How much of an ally's action bar the Sergeant fills per action (1/4 of a full bar)
  private const int ActionBarBoost = GlobalConstants.ActionBarSize / 4;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();

    // Target the ally with the most ticks until its next action (TicksUntilAction, which accounts for speed).
    // Units that never act (speed 0) and allies it cannot see are skipped.
    Unit? targetAlly = null;
    int longestWait = int.MinValue;

    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Unit unit = unitsGrid[x, y];
        if (unit != null && unit.Side == Side && unit != this && unit.Speed > 0 && LineOfSight.CanSeeUnit(GetOccupiedCells(), unit, terrainGrid, propsGrid))
        {
          int wait = unit.TicksUntilAction(1);
          if (wait > longestWait)
          {
            longestWait = wait;
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

    // Fill part of the ally's action bar so it acts sooner
    ally.ChangeActionProgress(ActionBarBoost, "Hastened");
  }
}
