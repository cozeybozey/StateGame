using Godot;
using System;
using System.Collections.Generic;

public partial class Nuke : Unit
{
  List<Vector2I> _targets = new List<Vector2I>();

  protected override void Start()
  {
    int ystart = (side) ? 0 : Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5);
    int yend = (side) ? Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5) : GlobalConstants.GridSize.Y;
    for (int y = ystart; y < yend; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        _targets.Add(new Vector2I(x, y));
      }
    }
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && targetUnit.side != side)
      {
        targetUnit.ChangeHealth(-damage);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    return _targets;
  }
}
