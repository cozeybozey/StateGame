using Godot;
using System;
using System.Collections.Generic;

public partial class Cleric : Unit
{
  List<Vector2I> _targets = new List<Vector2I>();

  protected override void Start()
  {
    int ystart = (Side) ? Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5) : 0;
    int yend = (Side) ? GlobalConstants.GridSize.Y : Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5);
    for (int y = ystart; y < yend; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        _targets.Add(new Vector2I(x, y));
      }
    }
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && targetUnit.Health < targetUnit.MaxHealth)
      {
        targetUnit.ChangeHealth(Damage, this);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    return [.._targets];
  }
}
