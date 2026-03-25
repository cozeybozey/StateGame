using Godot;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Principal;

public partial class GlobalFunctions : Node
{
  public static bool IsCellInsideGrid(Vector2I cell)
  {
    return cell.X >= 0 && cell.X < GlobalConstants.GridSize.X && cell.Y >= 0 && cell.Y < GlobalConstants.GridSize.Y;
  }

  public static Vector2I AbsCellToRelCell(Vector2I cell, bool player = true)
  {
    Vector2I subtractedPos = player ? GlobalConstants.GridStartPosPlayer : GlobalConstants.GridStartPosEnemy;
    return cell - subtractedPos;
  }

  public static Vector2I RelCellToAbsCell(Vector2I cell, bool player = true)
  {
    Vector2I addedPos = player ? GlobalConstants.GridStartPosPlayer : GlobalConstants.GridStartPosEnemy;
    return cell + addedPos;
  }

  public static Vector2I CellToGlobalPosition(Vector2I cell, int width, int height, Vector2I relPos)
  {
    Vector2I globalPos = cell * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt((0.5 * width - relPos.X) * GlobalConstants.TileSize), Mathf.FloorToInt((0.5 * height - relPos.Y) * GlobalConstants.TileSize));
    return globalPos;
  }

  public static Vector2I CellsToDimensions(List<Vector2I> cells)
  {
    int minX = cells[0].X;
    int maxX = cells[0].X;
    int minY = cells[0].X;
    int maxY = cells[0].X;
    foreach (Vector2I cell in cells)
    {
      if (cell.X < minX)
        minX = cell.X;
      if (cell.X > maxX)
        maxX = cell.X;
      if (cell.Y < minY)
        minY = cell.Y;
      if (cell.Y > maxY)
        maxY = cell.Y;
    }

    return new Vector2I(maxX - minX + 1, maxY - minY + 1);
  }

  public static Vector2I GetRelPosInCells(List<Vector2I> cells, Vector2I targetCell)
  {
    int minX = cells[0].X;
    int minY = cells[0].X;
    foreach (Vector2I cell in cells)
    {
      if (cell.X < minX)
        minX = cell.X;
      if (cell.Y < minY)
        minY = cell.Y;
    }

    return new Vector2I(targetCell.X - minX, targetCell.Y - minY);
  }

  // Type U unitsGrid because it can be of type Unit[,] or UnitInfo[,] all we do is check for null
  // Type T terrainGrid because it can be of type Terrain[,] or TerrainInfo[,] all we do is check for null and blocking
  // Type P propsGrid because it can be of type Prop[,] or PropInfo[,] all we do is check for null and blocking
  public static List<Vector2I> GetPossibleUnitLocations<U, T, P>(U[,] unitsGrid, T[,] terrainGrid, P[,] propsGrid, List<Vector2I> occupiedCells, bool side)
  {
    int startY, endY;
    if (side)
    {
      startY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
      endY = GlobalConstants.GridSize.Y;
    }
    else
    {
      startY = 0;
      endY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    }

    bool IsTerrainBlocking(T t) => t is Terrain terrain && terrain.Blocking ||
                                   t is TerrainInfo terrainInfo && terrainInfo.Blocking;

    bool IsPropBlocking(P p) => p is Prop prop && prop.Blocking ||
                                p is PropInfo propInfo && propInfo.Blocking;

    bool IsBlocked(int x, int y)
    {
      if (x < 0 || x >= GlobalConstants.GridSize.X) return true;
      if (y < startY || y >= endY) return true;
      if (unitsGrid[x, y] != null) return true;
      if (terrainGrid[x, y] != null && IsTerrainBlocking(terrainGrid[x, y])) return true;
      if (propsGrid[x, y] != null && IsPropBlocking(propsGrid[x, y])) return true;
      return false;
    }

    List<Vector2I> possiblePositions = new();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = startY; y < endY; y++)
      {
        bool canPlace = occupiedCells.All(rel => !IsBlocked(x + rel.X, y + rel.Y));
        if (canPlace)
          possiblePositions.Add(new Vector2I(x, y));
      }
    }

    return possiblePositions;
  }

  public static Vector2I? GetRandomUnitSpawnLocation<U, T, P>(U[,] unitsGrid, T[,] terrainGrid, P[,] propsGrid, List<Vector2I> occupiedCells, bool side)
  {
    List<Vector2I> possibleLocations = GetPossibleUnitLocations(unitsGrid, terrainGrid, propsGrid, occupiedCells, side);

    if (possibleLocations.Count == 0)
      return null!;


    RandomNumberGenerator rng = new RandomNumberGenerator();
    rng.Randomize();
    return possibleLocations[rng.RandiRange(0, possibleLocations.Count - 1)];
  }

  public static bool CanMoveToCell(GridEntity gridEntity, Vector2I targetLocation, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    foreach (Vector2I cell in gridEntity.OccupiedCells)
    {
      Vector2I checkLocation = targetLocation + cell;
      
      // Check if location is inside the grid
      if (!IsCellInsideGrid(checkLocation))
        return false;
      
      // Check if there are units at the target location
      if (unitsGrid[checkLocation.X, checkLocation.Y] != null && unitsGrid[checkLocation.X, checkLocation.Y] != gridEntity)
        return false;

      // Check if there is blocking terrain at the target location
      if (terrainGrid[checkLocation.X, checkLocation.Y] != null && terrainGrid[checkLocation.X, checkLocation.Y] != gridEntity && terrainGrid[checkLocation.X, checkLocation.Y].Blocking)
        return false;

      // Check if there is blocking props at the target location
      if (propsGrid[checkLocation.X, checkLocation.Y] != null && propsGrid[checkLocation.X, checkLocation.Y] != gridEntity && propsGrid[checkLocation.X, checkLocation.Y].Blocking)
        return false;
    }

    return true;
  }

  public static bool AreTypesCompatible(PropInfo prop, TerrainInfo terrain)
  {
    foreach (string propType in prop.Types)
    {
      if (!GlobalConstants.PropTerrainCompatibility.TryGetValue(propType, out var allowedTerrainTypes))
        continue;

      bool hasCompatibleTerrain = terrain.Types.Any(t => allowedTerrainTypes.Contains(t));
      if (!hasCompatibleTerrain)
        return false;
    }

    return true;
  }

  public static bool CanSpawnProp(Prop prop, Vector2I targetLocation, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    foreach (Vector2I cell in prop.OccupiedCells)
    {
      Vector2I checkLocation = targetLocation + cell;

      // Check if location is inside the grid
      if (!IsCellInsideGrid(checkLocation))
        return false;

      // Cannot place prop if other prop is already present
      if (propsGrid[checkLocation.X, checkLocation.Y] != null && propsGrid[checkLocation.X, checkLocation.Y] != prop)
        return false;

      // Check if prop and terrain types are compatible
      if (terrainGrid[checkLocation.X, checkLocation.Y] != null && !AreTypesCompatible(prop.GetInfo(), terrainGrid[checkLocation.X, checkLocation.Y].GetInfo()))
        return false;
    }

    return true;
  }

  public static bool CanSpawnProp(PropInfo prop, Vector2I targetLocation, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    foreach (Vector2I cell in prop.OccupiedCells)
    {
      Vector2I checkLocation = targetLocation + cell;

      // Check if location is inside the grid
      if (!IsCellInsideGrid(checkLocation))
        return false;

      // Cannot place prop if other prop is already present
      if (propsGrid[checkLocation.X, checkLocation.Y] != null)
        return false;

      // Check if prop and terrain types are compatible
      if (terrainGrid[checkLocation.X, checkLocation.Y] != null && !AreTypesCompatible(prop, terrainGrid[checkLocation.X, checkLocation.Y]))
        return false;
    }

    return true;
  }
}