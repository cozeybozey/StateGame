using Godot;
using System;
using System.Collections.Generic;

public partial class Nurse : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
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

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I targetVector = new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y - 1);

    if (!Side)
      targetVector = new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y + 1);

    if (targetVector.Y >= 0 && targetVector.Y < GlobalConstants.GridSize.Y && 
      unitsGrid[targetVector.X, targetVector.Y] != null && 
      unitsGrid[targetVector.X, targetVector.Y].Side == Side &&
      unitsGrid[targetVector.X, targetVector.Y].Health < unitsGrid[targetVector.X, targetVector.Y].MaxHealth)
        return [targetVector];
    else
      return [];
  }
}
