using Godot;
using System;
using System.Collections.Generic;

public partial class Chaos : Unit
{
  protected override void Start()
  {
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    // Collect unique units currently on the grid (by reference)
    List<Unit> units = new List<Unit>();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Unit unit = unitsGrid[x, y];
        if (unit == null)
          continue;
        if (!units.Contains(unit))
          units.Add(unit);
      }
    }

    // Move each unit to its mirrored position across the horizontal center
    foreach (Unit unit in units)
    {
      Vector2I oldMain = unit.occupiedMainCell;

      Vector2I dimensions = GlobalFunctions.CellsToDimensions(unit.occupiedCells);
      Vector2I relPos = GlobalFunctions.GetRelPosInCells(unit.occupiedCells, unit.occupiedCells[0]);
      int yPosToBeMirrored = unit.occupiedMainCell.Y + (dimensions.Y - 1 - relPos.Y);
      
      Vector2I mirrored = new Vector2I(oldMain.X, GlobalConstants.GridSize.Y - 1 - yPosToBeMirrored);
      unit.MoveToCell(mirrored, playing: true);
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new List<Vector2I>();

    // Show which main cells will be affected (one entry per unit)
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Unit u = unitsGrid[x, y];
        if (u == null)
          continue;

        foreach (Vector2I cell in u.GetOccupiedCells())
          result.Add(cell);
      }
    }

    return result;
  }
}
