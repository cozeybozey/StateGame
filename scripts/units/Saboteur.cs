using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Saboteur : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int dx = -2; dx <= 2; dx++)
    {
      for (int dy = -2; dy <= 2; dy++)
      {
        int distance = Mathf.Abs(dx) + Mathf.Abs(dy);

        if (distance == 0 || distance > 2)
          continue;

        Vector2I pos = new(OccupiedMainCell.X + dx, OccupiedMainCell.Y + dy);

        if (!GlobalFunctions.IsCellInsideGrid(pos))
          continue;

        result.Add(pos);
      }
    }

    if (Side)
    {
      if (!GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y - 1), unitsGrid, terrainGrid, propsGrid))
      {
        return result;
      }
    }
    else
    {
      if (!GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y + 1), unitsGrid, terrainGrid, propsGrid))
      {
        return result;
      }
    }
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0)
    {
      if (Side)
      {
        MoveToCell(new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y - 1), playing:true);
      }
      else
      {
        MoveToCell(new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y + 1), playing:true);
      }
    }
    else
    {
      foreach (Vector2I target in targets)
      {
        Unit targetUnit = unitsGrid[target.X, target.Y];
        if (targetUnit != null)
        {
          targetUnit.ChangeHealth(-Damage, this);
        }
      }
      Die(); // Sacrifice self
    }
  }
}
