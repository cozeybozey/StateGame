using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class Cherub : Unit
{

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    if (targets.Count == 0) return;

    Unit target = unitsGrid[targets[0].X, targets[0].Y];
    if (target == null) return;

    target.ChangeDamage(Damage);
    target.ChangeSpeed(Damage);
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new();
    int frontDir = Side ? -1 : 1;

    // Find frontmost unit in same column
    foreach (Vector2I cell in GetOccupiedCells())
    {
      int checkY = cell.Y + frontDir;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(cell.X, checkY)))
      {
        Unit unit = unitsGrid[cell.X, checkY];
        if (unit != null)
        {
          targets.Add(unit.OccupiedMainCell);
          return targets;
        }
        checkY += frontDir;
      }
    }

    return targets;
  }
}
