using Godot;
using System;
using System.Collections.Generic;

public partial class Nuke : Unit
{
  List<Vector2I> _targets = new List<Vector2I>();

  protected override void Start()
  {
    int ystart = (Side) ? 0 : Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5);
    int yend = (Side) ? Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5) : GlobalConstants.GridSize.Y;
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
      if (targetUnit != null && targetUnit.Side != Side)
      {
        targetUnit.ChangeHealth(-Damage, this);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    return _targets;
  }
}
