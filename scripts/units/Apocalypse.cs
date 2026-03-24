using Godot;
using System;
using System.Collections.Generic;

public partial class Apocalypse : Unit
{

  protected override void Start()
  {

  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new List<Vector2I>();
    List<Vector2I> occupiedCells = GetOccupiedCells();

    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Vector2I cell = new Vector2I(x, y);
        if (!occupiedCells.Contains(cell))
          targets.Add(cell);
      }
    }

    return targets;
  }
}