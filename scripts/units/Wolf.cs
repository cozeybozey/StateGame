using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Wolf : Unit
{
  private Vector2I? _targetLocation = null;

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0)
      return;

    // Possibly move to cell
    if (_targetLocation != null && _targetLocation != OccupiedMainCell)
    {
      MoveToCell(_targetLocation.Value, true);
    }

    // Attack target
    Unit targetUnit = unitsGrid[targets[0].X, targets[0].Y];
    if (targetUnit != null)
    {
      int packBonus = 0;
      // Count friendly wolves
      foreach (Unit unit in units)
      { 
        if (unit != null && unit != this && unit.Side == Side && unit.Id == Id)
          packBonus++;
      }

      targetUnit.ChangeHealth(-Damage - packBonus, this);
    }
    _targetLocation = null;
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Tuple<Vector2I, Vector2I>> targets = new List<Tuple<Vector2I, Vector2I>>();
    List<Vector2I> newSurroundingCells = [new Vector2I(-1, 0), new Vector2I(0, -1), new Vector2I(1, 0), new Vector2I(0, 1)];

    // Check if already adjacent to enemy unit, if so don't move and simply attack
    foreach (Vector2I cell in GetSurroundingCells())
    {
      Unit unit = unitsGrid[cell.X, cell.Y];
      if (unit != null && unit.Side != Side)
      {
        return [cell];
      }
    }

    if (Side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Vector2I newCell = new Vector2I(x, y);
          if (!GlobalFunctions.CanMoveToCell(this, newCell, unitsGrid, terrainGrid, propsGrid))
            continue;

          foreach (Vector2I cell in newSurroundingCells)
          {
            if (!GlobalFunctions.IsCellInsideGrid(newCell + cell))
              continue;

            Unit unit = unitsGrid[x + cell.X, y + cell.Y];
            if (unit != null && unit.Side != Side)
            {
              _targetLocation = newCell;
              return [newCell + cell];
            }
          }
        }
      }
    }
    else
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Vector2I newCell = new Vector2I(x, y);
          if (!GlobalFunctions.CanMoveToCell(this, newCell, unitsGrid, terrainGrid, propsGrid))
            continue;

          foreach (Vector2I cell in newSurroundingCells)
          {
            if (!GlobalFunctions.IsCellInsideGrid(newCell + cell))
              continue;

            Unit unit = unitsGrid[x + cell.X, y + cell.Y];
            if (unit != null && unit.Side != Side)
            {
              _targetLocation = newCell;
              return [newCell + cell];
            }
          }
        }
      }
    }

    return [];
  }
}
