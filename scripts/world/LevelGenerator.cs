using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

// The starting content of one board: terrain, props, and enemy units on their main cells
public record LevelBoard(TerrainInfo[,] Terrain, PropInfo[,] Props, UnitInfo[,] Units);

// Creates the boards of levels: random ones that scale with difficulty, and the fixed ones in levels.json
public class LevelGenerator
{
  // One entry per section of the world
  private readonly List<string> _nonBlockingTerrains = ["grass", "white_floor_tiles", "stone_floor", "dark_grass", "gray_floor_tiles"];
  private readonly List<string> _blockingTerrains1 = ["water", "holy_water", "oil", "poisoned_water", "blood"];
  private readonly List<string> _blockingTerrains2 = ["mountain", "green_mountain", "hole_in_floor", "mud", "lower_gray_floor_tiles"];

  private readonly Random _rng;
  private readonly Dictionary _levelsData;

  public LevelGenerator(Random rng)
  {
    _rng = rng;
    _levelsData = (Dictionary)Json.ParseString(FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText());
  }

  public LevelBoard LoadLevel(string levelId, int levelSection)
  {
    TerrainInfo[,] terrainGrid = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    PropInfo[,] propsGrid = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    Dictionary levelData = (Dictionary)_levelsData[levelId];

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        terrainGrid[x, y] = GlobalConstants.TerrainsData[_nonBlockingTerrains[levelSection]];

    Godot.Collections.Array levelUnits = (Godot.Collections.Array)levelData["units"];
    foreach (Dictionary unitData in levelUnits)
    {
      int x = (int)unitData["x"];
      int y = (int)unitData["y"];
      string name = (string)unitData["name"];

      UnitInfo unitInfo = GlobalConstants.UnitsData[name];

      mainCellsUnitGrid[x, y] = unitInfo;
    }

    return new LevelBoard(terrainGrid, propsGrid, mainCellsUnitGrid);
  }

  public LevelBoard CreateRandomLevel(int difficulty, int levelSection)
  {
    // Only place a limited amount of blocking terrains and props. So there is enough room for units.
    int expectedUserUnitSlots = difficulty;
    int requiredFreeTilesUser = expectedUserUnitSlots + 10;
    int expectedEnemyUnitSlots = difficulty + (difficulty / 2);
    if (difficulty > 3)
      expectedEnemyUnitSlots += difficulty;  // Ramp up difficulty even more after a while.
    int requiredFreeTilesEnemy = expectedEnemyUnitSlots + 10;
    int halfTiles = GlobalConstants.GridSize.X * Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    int maxBlockingUser = halfTiles - requiredFreeTilesUser;
    int maxBlockingEnemy = halfTiles - requiredFreeTilesEnemy;

    TerrainInfo[,] terrain = CreateTerrain(maxBlockingUser, maxBlockingEnemy, levelSection);
    PropInfo[,] props = CreateProps(maxBlockingUser, maxBlockingEnemy, terrain, levelSection);
    UnitInfo[,] units = CreateUnits(difficulty, terrain, props, levelSection, expectedEnemyUnitSlots);
    return new LevelBoard(terrain, props, units);
  }

  private TerrainInfo[,] CreateTerrain(int maxBlockingUser, int maxBlockingEnemy, int levelSection)
  {
    TerrainInfo[,] terrain = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        terrain[x, y] = GlobalConstants.TerrainsData[_nonBlockingTerrains[levelSection]];

    FastNoiseLite mountainNoise = new();
    mountainNoise.Seed = _rng.Next();
    mountainNoise.Frequency = 0.035f;
    mountainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

    FastNoiseLite waterNoise = new();
    waterNoise.Seed = _rng.Next();
    waterNoise.Frequency = 0.035f;
    waterNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

    int topHalfBlocking = 0;
    int bottomHalfBlocking = 0;
    int midY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        bool isTopHalf = y < midY;
        if ((isTopHalf && topHalfBlocking >= maxBlockingEnemy) ||
          (!isTopHalf && bottomHalfBlocking >= maxBlockingUser))
          continue;

        float mountainVal = mountainNoise.GetNoise2D(x, y);
        float waterVal = waterNoise.GetNoise2D(x, y);

        if (mountainVal > 0.40f)
        {
          terrain[x, y] = GlobalConstants.TerrainsData[_blockingTerrains1[levelSection]];
          if (isTopHalf) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
        else if (waterVal > 0.40f)
        {
          terrain[x, y] = GlobalConstants.TerrainsData[_blockingTerrains2[levelSection]];
          if (isTopHalf) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
      }
    }

    return terrain;
  }

  private PropInfo[,] CreateProps(int maxBlockingUser, int maxBlockingEnemy, TerrainInfo[,] terrainGrid, int levelSection)
  {
    PropInfo[,] props = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    List<string> movableProps = ["bush", "vase", "broken_turret", "dark_bush", "demonic_vase"];
    List<string> damagableProps = ["rock", "white_statue", "rock", "gravestone", "statue"];
    List<string> specialProps1 = ["fire", "fire", "fire", "fire", "fire"];
    List<string> specialProps2 = ["wild_fire", "wild_fire", "wild_fire", "wild_fire", "wild_fire"];

    // Count already blocking terrain per half
    int midY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    int topHalfBlocking = 0;
    int bottomHalfBlocking = 0;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        if (terrainGrid[x, y] != null && terrainGrid[x, y].Blocking)
        {
          if (y < midY) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
      }
    }

    bool CanPlaceBlocking(int x, int y)
    {
      bool isTopHalf = y < midY;
      if ((isTopHalf && topHalfBlocking >= maxBlockingEnemy) ||
        (!isTopHalf && bottomHalfBlocking >= maxBlockingUser))
        return false;
      else
        return true;
    }

    void PlaceBlocking(int x, int y)
    {
      if (y < midY) topHalfBlocking++;
      else bottomHalfBlocking++;
    }

    // Movable prop
    int rockCount = _rng.Next(0, 4);
    PropInfo propInfo = GlobalConstants.PropsData[movableProps[levelSection]];
    for (int i = 0; i < rockCount; i++)
    {
      List<Vector2I> candidates = new();
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
          if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, props) &&
              CanPlaceBlocking(x, y))
            candidates.Add(new Vector2I(x, y));

      if (candidates.Count == 0) break;
      Vector2I cell = candidates[_rng.Next(candidates.Count)];
      props[cell.X, cell.Y] = propInfo;
      PlaceBlocking(cell.X, cell.Y);
    }

    // Damagable prop
    int bushCount = _rng.Next(0, 4);
    propInfo = GlobalConstants.PropsData[damagableProps[levelSection]];
    for (int i = 0; i < bushCount; i++)
    {
      List<Vector2I> candidates = new();
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
          // There are no units yet, so pass empty grid for units
          if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, props) &&
              CanPlaceBlocking(x, y))
            candidates.Add(new Vector2I(x, y));

      if (candidates.Count == 0) break;
      Vector2I cell = candidates[_rng.Next(candidates.Count)];
      props[cell.X, cell.Y] = propInfo;
      PlaceBlocking(cell.X, cell.Y);
    }

    // Special prop
    bool spawnFire = _rng.NextDouble() < 0.25;
    propInfo = GlobalConstants.PropsData[specialProps1[levelSection]];
    if (spawnFire)
    {
      List<Vector2I> cluster = SpawnClusterOfPropType(propInfo, terrainGrid, props, _rng.Next(2, 6));
      foreach (Vector2I cell in cluster)
        props[cell.X, cell.Y] = propInfo;
    }

    // Special prop 2
    bool spawnWildFire = _rng.NextDouble() < 0.15;
    propInfo = GlobalConstants.PropsData[specialProps2[levelSection]];
    if (spawnWildFire)
    {
      List<Vector2I> cluster = SpawnClusterOfPropType(propInfo, terrainGrid, props, _rng.Next(2, 6));
      foreach (Vector2I cell in cluster)
        props[cell.X, cell.Y] = propInfo;
    }

    return props;
  }

  private List<Vector2I> SpawnClusterOfPropType(PropInfo propInfo, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid, int clusterSize, Func<int, int, bool>? extraCheck = null)
  {
    List<Vector2I> candidates = new();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        // There are no units yet, so pass empty grid for units
        if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, propsGrid) &&
            (extraCheck == null || extraCheck(x, y)))
          candidates.Add(new Vector2I(x, y));

    if (candidates.Count == 0) return [];

    Vector2I origin = candidates[_rng.Next(candidates.Count)];
    List<Vector2I> cluster = new() { origin };
    List<Vector2I> frontier = new() { origin };

    while (cluster.Count < clusterSize && frontier.Count > 0)
    {
      Vector2I current = frontier[_rng.Next(frontier.Count)];
      frontier.Remove(current);

      Vector2I[] neighbors = {
            new(current.X + 1, current.Y),
            new(current.X - 1, current.Y),
            new(current.X, current.Y + 1),
            new(current.X, current.Y - 1),
        };

      foreach (Vector2I neighbor in neighbors)
      {
        if (cluster.Count >= clusterSize) break;
        if (neighbor.X < 0 || neighbor.Y < 0 ||
            neighbor.X >= GlobalConstants.GridSize.X ||
            neighbor.Y >= GlobalConstants.GridSize.Y) continue;
        if (cluster.Contains(neighbor)) continue;
        // There are no units yet, so pass empty grid for units
        if (!GlobalFunctions.CanSpawnProp(propInfo, neighbor, new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, propsGrid)) continue;
        if (extraCheck != null && !extraCheck(neighbor.X, neighbor.Y)) continue;

        cluster.Add(neighbor);
        frontier.Add(neighbor);
      }
    }

    return cluster;
  }

  private UnitInfo[,] CreateUnits(int difficulty, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid, int levelSection, int expectedEnemyUnitSlots)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    List<string> dominantUnitTypes = ["primal", "holy", "machinery", "rotten", "demonic"];
    string dominantType = dominantUnitTypes[levelSection];

    int budget = expectedEnemyUnitSlots;
    int maxStage = GetMaxStageForDifficulty(difficulty);

    // Build unit pools
    List<UnitInfo> dominantPool = GlobalConstants.UnitsData.Values
        .Where(u => u.Types.Contains(dominantType) && u.Stage <= maxStage)
        .ToList();

    List<UnitInfo> typelessPool = GlobalConstants.UnitsData.Values
        .Where(u => u.Types.Count == 0 && u.Stage <= maxStage)
        .ToList();

    List<UnitInfo> otherPool = GlobalConstants.UnitsData.Values
        .Where(u => !u.Types.Contains(dominantType) && u.Types.Count > 0 && u.Stage <= maxStage)
        .ToList();

    RandomNumberGenerator rng = new();
    rng.Randomize();

    while (budget > 0)
    {
      // It is possible for a randomly chosen unit to not have any possible positions on the grid due to terrain or props/
      // If that happens it can only be because of multi celled units, because there are always enough non-blocking tiles on each level
      // Therefore we simply retry selecting random units until we find one that does have a possible location.
      UnitInfo unitInfo = GlobalConstants.UnitsData["archer"];
      List<Vector2I> possiblePositions = new List<Vector2I>();
      int cost = unitInfo.OccupiedCells.Count;
      int nrOfTries = 0;
      while (possiblePositions.Count == 0 && nrOfTries < 10)
      {
        nrOfTries++;

        // Pick pool based on probability
        double roll = rng.Randf();
        List<UnitInfo> pool;
        if (roll < 0.75f)
          pool = dominantPool;
        else if (roll < 0.90f)
          pool = typelessPool;
        else
          pool = otherPool;

        if (pool.Count == 0)
          pool = typelessPool.Count > 0 ? typelessPool : GlobalConstants.UnitsData.Values.ToList();

        // Pick random unit from pool within budget
        List<UnitInfo> affordableUnits = pool
            .Where(u => u.OccupiedCells.Count <= budget)
            .ToList();
        if (affordableUnits.Count == 0)
          continue;
        unitInfo = affordableUnits[rng.RandiRange(0, affordableUnits.Count - 1)];
        cost = unitInfo.OccupiedCells.Count;

        // Get random position for the unit, ensuring it fits within the grid and doesn't overlap with existing units
        possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, false);
      }

      // We failed finding a different unit
      if (nrOfTries >= 10)
      {
        unitInfo = GlobalConstants.UnitsData["archer"];
        possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, false);
        cost = unitInfo.OccupiedCells.Count;
        GD.Print("Unit placement failed. Placing archer instead.");
      }

      // Determine optimal positions for this unit and pick the semi-best one
      Vector2I cellPos = possiblePositions
          .OrderBy(_ => rng.Randi())
          .OrderByDescending(p => unitInfo.ScorePlacement(p, unitInfo, unitGrid, terrainGrid, propsGrid) + rng.RandiRange(0, 5))
          .First();

      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[cellPos.X + cell.X, cellPos.Y + cell.Y] = unitInfo;
      }
      mainCellsUnitGrid[cellPos.X, cellPos.Y] = unitInfo;

      budget -= cost; // TODO AKN unitInfo.Cost;
    }

    return mainCellsUnitGrid;
  }

  private static int GetMaxStageForDifficulty(int difficulty)
  {
    if (difficulty < 2) return 0;
    if (difficulty < 3) return 1;
    if (difficulty < 5) return 2;
    if (difficulty < 7) return 3;
    if (difficulty < 9) return 4;
    return 5;
  }
}
