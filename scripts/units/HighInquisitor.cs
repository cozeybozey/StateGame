using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class HighInquisitor : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new List<Vector2I>();
    List<int> mirroredYValues = new List<int>();

    foreach (Vector2I cell in GetOccupiedCells())
    {
      int mirroredCellY = GlobalConstants.GridSize.Y - cell.Y - 1;
      if (!mirroredYValues.Contains(mirroredCellY))
        mirroredYValues.Add(mirroredCellY);
    }

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      foreach (int mirroredY in mirroredYValues)
      {
        Vector2I targetCell = new Vector2I(x, mirroredY);
        targets.Add(targetCell);
      }
    }

    return targets;
  }
}
