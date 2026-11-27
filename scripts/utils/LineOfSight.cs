using Godot;
using System.Collections.Generic;
using System.Linq;

// Which units can be seen from where. Mountains hide what is behind them, and so do tall props until they are destroyed.
public static class LineOfSight
{
  // The cell an attack goes to. An obstacle is a prop that hides every enemy, and has to be destroyed first.
  public record Target(Vector2I Cell, bool Obstacle);

  // The closest enemy that can be seen. Of enemies just as close, the frontmost one, and then the leftmost one.
  public static Target? FindClosest(List<Vector2I> cells, bool side, IEnumerable<Unit> units, Terrain[,] terrain, Prop[,] props)
  {
    List<Unit> enemies = units
      .Where(u => u.Side != side)
      .OrderBy(u => Pairs(cells, u).First().Distance)
      .ThenBy(u => side ? -u.GetOccupiedCells().Max(c => c.Y) : u.GetOccupiedCells().Min(c => c.Y))
      .ThenBy(u => u.OccupiedMainCell.X)
      .ToList();
    return FindFirstVisible(cells, enemies, terrain, props);
  }

  // The enemy farthest away that can be seen
  public static Target? FindFarthest(List<Vector2I> cells, bool side, IEnumerable<Unit> units, Terrain[,] terrain, Prop[,] props)
  {
    List<Unit> enemies = units
      .Where(u => u.Side != side)
      .OrderByDescending(u => Pairs(cells, u).Last().Distance)
      .ToList();
    return FindFirstVisible(cells, enemies, terrain, props);
  }

  // The first of these enemies that can be seen
  public static Target? FindFirstVisible(List<Vector2I> cells, List<Unit> enemies, Terrain[,] terrain, Prop[,] props)
  {
    foreach (Unit enemy in enemies)
    {
      foreach ((Vector2I from, Vector2I to, int _) in Pairs(cells, enemy))
      {
        if (CanSee(from, to, terrain, props))
          return new Target(to, false);
      }
    }

    if (enemies.Count == 0)
      return null;

    // No enemy can be seen: break through to the first one, the closest. If only mountains are in the way, shoot over them.
    (Vector2I start, Vector2I end, int _) = Pairs(cells, enemies[0]).First();
    foreach (Vector2I cell in CellsBetween(start, end))
    {
      if (BlocksSight(props[cell.X, cell.Y]))
        return new Target(cell, true);
    }
    return new Target(end, false);
  }

  // The first unit straight in front of one of these cells, friend or foe. A prop that blocks sight in the way is
  // the target when hitsProps, and otherwise stops it, like terrain that blocks sight.
  public static Target? FindInColumn(List<Vector2I> cells, bool side, Unit[,] units, Terrain[,] terrain, Prop[,] props, bool hitsProps)
  {
    Target? closest = null;
    int closestDistance = int.MaxValue;
    foreach (Vector2I cell in cells)
    {
      List<Vector2I> column = GetColumn(cell, side, terrain, props);
      for (int i = 0; i < column.Count && i < closestDistance; i++)
      {
        Vector2I target = column[i];
        Unit unit = units[target.X, target.Y];
        if (unit != null && !cells.Contains(target))
        {
          closest = new Target(target, false);
          closestDistance = i;
        }
        else if (BlocksSight(target, terrain, props))
        {
          if (hitsProps && BlocksSight(props[target.X, target.Y]))
          {
            closest = new Target(target, true);
            closestDistance = i;
          }
        }
        else
          continue;
        break;
      }
    }
    return closest;
  }

  // Every enemy that can be seen, with the cell of it that is seen
  public static List<Target> FindVisibleEnemies(List<Vector2I> cells, bool side, IEnumerable<Unit> units, Terrain[,] terrain, Prop[,] props)
  {
    List<Target> visible = new();
    foreach (Unit enemy in units.Where(u => u.Side != side))
    {
      foreach ((Vector2I from, Vector2I to, int _) in Pairs(cells, enemy))
      {
        if (CanSee(from, to, terrain, props))
        {
          visible.Add(new Target(to, false));
          break;
        }
      }
    }
    return visible;
  }

  // A random enemy that can be seen. When none can be seen, the way to the closest enemy is broken open first.
  public static Target? FindRandomVisible(List<Vector2I> cells, bool side, IEnumerable<Unit> units, Terrain[,] terrain, Prop[,] props, RandomNumberGenerator rng)
  {
    List<Target> visible = FindVisibleEnemies(cells, side, units, terrain, props);
    if (visible.Count > 0)
      return visible[rng.RandiRange(0, visible.Count - 1)];
    return FindClosest(cells, side, units, terrain, props);
  }

  // The cells on the other half of the board that can be seen
  public static List<Vector2I> GetVisibleCells(List<Vector2I> cells, bool side, Terrain[,] terrain, Prop[,] props)
  {
    return Board.Cells
      .Where(cell => cell.Y < GlobalConstants.GridSize.Y / 2 == side && CanSee(cells, cell, terrain, props))
      .ToList();
  }

  // The cells straight in front of a cell, up to and with the first one that blocks sight
  public static List<Vector2I> GetColumn(Vector2I cell, bool side, Terrain[,] terrain, Prop[,] props)
  {
    List<Vector2I> column = new();
    int frontDir = side ? -1 : 1;
    for (Vector2I next = cell + new Vector2I(0, frontDir); GlobalFunctions.IsCellInsideGrid(next); next.Y += frontDir)
    {
      column.Add(next);
      if (BlocksSight(next, terrain, props))
        break;
    }
    return column;
  }

  // Whether any cell of the unit can be seen from these cells
  public static bool CanSeeUnit(List<Vector2I> cells, Unit unit, Terrain[,] terrain, Prop[,] props)
  {
    return unit.GetOccupiedCells().Any(cell => CanSee(cells, cell, terrain, props));
  }

  public static bool CanSee(List<Vector2I> cells, Vector2I target, Terrain[,] terrain, Prop[,] props)
  {
    return cells.Any(cell => CanSee(cell, target, terrain, props));
  }

  public static bool CanSee(Vector2I from, Vector2I to, Terrain[,] terrain, Prop[,] props)
  {
    return !CellsBetween(from, to).Any(cell => BlocksSight(cell, terrain, props));
  }

  public static bool BlocksSight(Vector2I cell, Terrain[,] terrain, Prop[,] props)
  {
    Terrain cellTerrain = terrain[cell.X, cell.Y];
    return (GodotObject.IsInstanceValid(cellTerrain) && cellTerrain.StartTerrainInfo.BlocksSight) || BlocksSight(props[cell.X, cell.Y]);
  }

  private static bool BlocksSight(Prop? prop)
  {
    return prop != null && GodotObject.IsInstanceValid(prop) && prop.StartPropInfo.BlocksSight;
  }

  // Every combination of one of these cells and one of the unit's cells, closest first
  private static IEnumerable<(Vector2I From, Vector2I To, int Distance)> Pairs(List<Vector2I> cells, Unit unit)
  {
    return cells
      .SelectMany(from => unit.GetOccupiedCells().Select(to => (from, to, from.DistanceSquaredTo(to))))
      .OrderBy(pair => pair.Item3);
  }

  // The cells that a straight line between two cell centers crosses, without both ends.
  // A line that exactly crosses a corner passes between the cells beside it.
  private static IEnumerable<Vector2I> CellsBetween(Vector2I from, Vector2I to)
  {
    Vector2I step = (to - from).Sign();
    Vector2I total = (to - from).Abs();
    Vector2I taken = Vector2I.Zero;
    Vector2I cell = from;

    while (true)
    {
      int difference = (1 + 2 * taken.X) * total.Y - (1 + 2 * taken.Y) * total.X;
      Vector2I move = new(difference <= 0 ? 1 : 0, difference >= 0 ? 1 : 0);
      taken += move;
      cell += move * step;
      if (taken.X >= total.X && taken.Y >= total.Y)
        yield break;
      yield return cell;
    }
  }
}
