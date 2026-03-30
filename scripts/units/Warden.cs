using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class Warden : Unit
{

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    if (targets.Count == 0) return;

    Unit target = unitsGrid[targets[0].X, targets[0].Y];
    if (target == null) return;

    int holyBonus = 0;
    // Count friendly holy units in the same rows as this unit
    List<Unit> countedUnits = new List<Unit>();
    foreach (Vector2I cell in GetOccupiedCells())
    {
      // check all cells in this row
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Unit unit = unitsGrid[x, cell.Y];
        if (unit != null && unit != this && unit.Side == Side && unit.Types.Contains("holy") && !countedUnits.Contains(unit))
        {
          holyBonus++;
          countedUnits.Add(unit);
        }
      }
    }

    target.ChangeHealth(-Damage - holyBonus, this);
  }
}
