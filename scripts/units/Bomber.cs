using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Bomber : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Random random = new();

    List<Vector2I> enemies = new();
    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        if (unitsGrid[x, y] != null && unitsGrid[x, y].Side != Side)
          enemies.Add(new Vector2I(x, y));

    if (enemies.Count == 0) return [];

    Vector2I target = enemies[random.Next(enemies.Count)];

    List<Vector2I> cells = new();
    for (int dy = -1; dy <= 1; dy++)
      for (int dx = -1; dx <= 1; dx++)
      {
        Vector2I cell = new(target.X + dx, target.Y + dy);
        if (cell.X >= 0 && cell.X < GlobalConstants.GridSize.X &&
            cell.Y >= 0 && cell.Y < GlobalConstants.GridSize.Y)
          cells.Add(cell);
      }

    return cells;
  }

}
