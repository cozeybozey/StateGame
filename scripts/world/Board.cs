using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Everything standing on the board: the unit, terrain and prop grids, kept up to date from the entities' signals
public class Board
{
  private const string WarningMarkScenePath = "res://scenes/explanation_mark.tscn";

  public readonly List<Unit> Units = new();

  // Dead units. They can be revived, so they stay known for the rest of the battle.
  public readonly List<Unit> RemovedUnits = new();

  // Enemies the current level started with, used for rewards
  public readonly List<UnitInfo> LevelUnits = new();

  public Unit[,] UnitsGrid { get; private set; } = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
  public Terrain[,] TerrainGrid { get; private set; } = new Terrain[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
  public Prop[,] PropsGrid { get; private set; } = new Prop[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

  public int PlayerUnitsCount { get; private set; } = 0;
  public int EnemyUnitsCount { get; private set; } = 0;

  // Set by Battle. During a battle the order of Units only changes at the end of a turn.
  public bool InBattle = false;

  public event Action<Unit>? UnitAdded;
  public event Action<Unit>? UnitRemoved;
  public event Action<Unit>? UnitChanged;       // Moved or resized
  public event Action<Vector2I>? CellChanged;   // Something appeared on this cell, or terrain or a prop left it
  public event Action? UnitDied;

  public IEnumerable<Prop> Props => PropsGrid.Cast<Prop>().Where(p => p != null && GodotObject.IsInstanceValid(p));

  private readonly Node _unitsNode;
  private readonly Node _terrainNode;
  private readonly Node _propsNode;
  private readonly GridOverlay _gridOverlay;
  private Node2D?[,] _warningMarks = new Node2D[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
  private bool _turnOrderDirty = false;

  public Board(Node world, GlobalSignals signals)
  {
    _unitsNode = world.GetNode("Units");
    _terrainNode = world.GetNode("Terrain");
    _propsNode = world.GetNode("Props");
    _gridOverlay = world.GetNode<GridOverlay>("GridOverlay");
    _gridOverlay.SetUnits(UnitsGrid);

    signals.GridEntityDied += OnDied;
    signals.GridEntityMoved += OnMoved;
    signals.GridEntitySpawned += OnSpawned;
    signals.SpeedChanged += OnSpeedChanged;
    signals.SizeChanged += OnSizeChanged;
    signals.UnitRemoved += OnUnitRemoved;
  }

  // Cells that already hold a prop or unit keep it
  public void SpawnLevel(TerrainInfo[,] terrain, PropInfo[,] props, UnitInfo[,] enemies)
  {
    int halfY = GlobalConstants.GridSize.Y / 2;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        TerrainInfo terrainInfo = terrain[x, y];
        if (terrainInfo != null)
        {
          Terrain instance = GD.Load<PackedScene>(terrainInfo.ScenePath).Instantiate<Terrain>();
          instance.Initialize(terrainInfo, cell, placed: true);
          _terrainNode.AddChild(instance);
          if (y < halfY)
            instance.ModulateSprite(new Color(0.8f, 0.8f, 0.8f));  // Darken the sprite to make it clear it's the enemy side
        }

        PropInfo propInfo = props[x, y];
        if (propInfo != null && PropsGrid[x, y] == null)
        {
          Prop instance = GD.Load<PackedScene>(propInfo.ScenePath).Instantiate<Prop>();
          instance.Initialize(propInfo, cell, placed: true);
          _propsNode.AddChild(instance);
        }
      }
    }

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < halfY; y++)
      {
        UnitInfo unitInfo = enemies[x, y];
        if (unitInfo != null && UnitsGrid[x, y] == null)
        {
          SpawnUnit(unitInfo, false, new Vector2I(x, y), placed: true);
          LevelUnits.Add(unitInfo);
        }
      }
    }

    foreach (Vector2I cell in GetBlockedUnitCells())
      AddWarningMark(cell);

    _gridOverlay.SetTerrain(TerrainGrid);
    _gridOverlay.SetProps(PropsGrid);
  }

  public Unit SpawnUnit(UnitInfo unitInfo, bool side, Vector2I cell, bool placed = false, Action<Unit>? beforeAdd = null)
  {
    Unit instance = GD.Load<PackedScene>(unitInfo.ScenePath).Instantiate<Unit>();
    instance.Initialize(unitInfo, side, cell, placed);
    beforeAdd?.Invoke(instance);
    _unitsNode.AddChild(instance);
    return instance;
  }

  // Cells where a unit stands on blocking terrain or a blocking prop
  public IEnumerable<Vector2I> GetBlockedUnitCells()
  {
    foreach (Unit unit in Units)
    {
      foreach (Vector2I cell in unit.GetOccupiedCells())
      {
        if ((TerrainGrid[cell.X, cell.Y] != null && TerrainGrid[cell.X, cell.Y].Blocking) ||
          (PropsGrid[cell.X, cell.Y] != null && PropsGrid[cell.X, cell.Y].Blocking))
          yield return cell;
      }
    }
  }

  public UnitInfo[,] GetPlayerUnits()
  {
    UnitInfo[,] grid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    foreach (Unit unit in Units)
    {
      if (unit.Side)
        grid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y] = unit.GetInfo();
    }
    return grid;
  }

  public void Clear()
  {
    while (Units.Count > 0)
      Units[Units.Count - 1].Remove();

    // Dead units can still be active, so those have to be freed as well
    foreach (Unit unit in RemovedUnits)
    {
      if (GodotObject.IsInstanceValid(unit))
        unit.QueueFree();
    }
    RemovedUnits.Clear();
    UnitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _gridOverlay.SetUnits(UnitsGrid);

    foreach (Terrain terrain in TerrainGrid)
      if (GodotObject.IsInstanceValid(terrain))
        terrain.QueueFree();
    TerrainGrid = new Terrain[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _gridOverlay.SetTerrain(TerrainGrid);

    foreach (Prop prop in PropsGrid)
      if (GodotObject.IsInstanceValid(prop))
        prop.QueueFree();
    PropsGrid = new Prop[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _gridOverlay.SetProps(PropsGrid);

    foreach (Node2D? mark in _warningMarks)
      if (mark != null && GodotObject.IsInstanceValid(mark))
        mark.QueueFree();
    _warningMarks = new Node2D[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    LevelUnits.Clear();
    EnemyUnitsCount = 0;
  }

  // Turn order for units that act on the same tick: leftmost first, then frontmost
  public void SortUnits()
  {
    List<Unit> sorted = Units
      .OrderBy(u => u.GlobalPosition.X)
      .ThenBy(u =>
          u.Side
              ? u.OccupiedMainCell.Y  // Player: lower Y first
              : GlobalConstants.GridSize.Y - 1 - u.OccupiedMainCell.Y)  // Enemy: higher Y first
      .ToList();

    Units.Clear();
    Units.AddRange(sorted);
  }

  // While placing units (deck screen or before a level), show the turn order:
  // a number per unit showing the order of first actions.
  // Uses the same prediction as the timeline, which mirrors the simulation.
  public void UpdateTurnOrderPreview()
  {
    if (InBattle || !_turnOrderDirty)
      return;

    _turnOrderDirty = false;
    Units.RemoveAll(u => !GodotObject.IsInstanceValid(u));
    SortUnits();
    foreach (Unit unit in Units)
      unit.ResetActionBar();

    // Number = place of the unit's first action (all bars start empty, so this is who opens the battle)
    int order = 1;
    HashSet<Unit> numbered = new();
    foreach (TurnTimeline.PredictedAction action in TurnTimeline.Predict(Units, Units.Count * 2))
    {
      if (action.Unit != null && numbered.Add(action.Unit))
        action.Unit.ShowTurnOrder(order++);
    }
    foreach (Unit unit in Units)
    {
      if (!numbered.Contains(unit))
        unit.ShowTurnOrder(null);  // Never acts (speed 0)
    }
  }

  public void HideTurnOrder()
  {
    foreach (Unit unit in Units)
      unit.ShowTurnOrder(null);
    _turnOrderDirty = false;
  }

  private void AddWarningMark(Vector2I cell)
  {
    Node2D mark = GD.Load<PackedScene>(WarningMarkScenePath).Instantiate<Node2D>();
    mark.GlobalPosition = GlobalFunctions.CellToGlobalPosition(cell, 1, 1, new Vector2I(0, 0));
    _unitsNode.AddChild(mark);
    _warningMarks[cell.X, cell.Y] = mark;
  }

  private void RemoveWarningMark(Vector2I cell)
  {
    Node2D? mark = _warningMarks[cell.X, cell.Y];
    if (mark != null && GodotObject.IsInstanceValid(mark))
      mark.QueueFree();
    _warningMarks[cell.X, cell.Y] = null;
  }

  private void OnUnitsChanged()
  {
    if (InBattle)
      return;
    SortUnits();
    _turnOrderDirty = true;
  }

  private void RemoveUnit(Unit unit)
  {
    UnitRemoved?.Invoke(unit);
    Units.Remove(unit);
    if (!InBattle)
      _turnOrderDirty = true;

    Put(UnitsGrid, unit.GetOccupiedCells(), null);

    // ^ = xor, which switches the condition if switched sides is true
    if (unit.Side ^ unit.SwitchedSides)
      PlayerUnitsCount--;
    else
      EnemyUnitsCount--;
  }

  private static void Put<T>(T[,] grid, IEnumerable<Vector2I> cells, T? entity) where T : GridEntity
  {
    foreach (Vector2I cell in cells)
      grid[cell.X, cell.Y] = entity!;
  }

  // Swapping can make it so another entity already stands on the old cells, which then keeps them
  private static void Move<T>(T[,] grid, T entity, Vector2I oldMainCell) where T : GridEntity
  {
    foreach (Vector2I offset in entity.OccupiedCells)
    {
      Vector2I cell = oldMainCell + offset;
      if (grid[cell.X, cell.Y] == entity)
        grid[cell.X, cell.Y] = null!;
    }
    Put(grid, entity.GetOccupiedCells(), entity);
  }

  private void OnSpawned(GridEntity entity, bool playing)
  {
    if (entity is Unit unit)
    {
      Put(UnitsGrid, unit.GetOccupiedCells(), unit);
      Units.Add(unit);
      if (unit.Side)
        PlayerUnitsCount++;
      else
        EnemyUnitsCount++;
      UnitAdded?.Invoke(unit);
      OnUnitsChanged();
    }
    else if (entity is Terrain terrain)
      Put(TerrainGrid, terrain.GetOccupiedCells(), terrain);
    else if (entity is Prop prop)
      Put(PropsGrid, prop.GetOccupiedCells(), prop);

    CellChanged?.Invoke(entity.OccupiedMainCell);
  }

  private void OnMoved(GridEntity entity, Vector2I oldCell, bool playing)
  {
    if (entity is Unit unit)
    {
      UnitChanged?.Invoke(unit);
      Move(UnitsGrid, unit, oldCell);
      if (!InBattle)
      {
        foreach (Vector2I offset in unit.OccupiedCells)
          RemoveWarningMark(oldCell + offset);
      }
      OnUnitsChanged();
    }
    else if (entity is Terrain terrain)
    {
      Move(TerrainGrid, terrain, oldCell);
      CellChanged?.Invoke(oldCell);
    }
    else if (entity is Prop prop)
    {
      Move(PropsGrid, prop, oldCell);
      CellChanged?.Invoke(oldCell);
    }
  }

  private void OnSizeChanged(GridEntity entity, Godot.Collections.Array<Vector2I> oldOccupiedCells)
  {
    if (entity is Unit unit)
    {
      UnitChanged?.Invoke(unit);
      Put(UnitsGrid, oldOccupiedCells, null);
      Put(UnitsGrid, unit.GetOccupiedCells(), unit);
      OnUnitsChanged();
    }
    else if (entity is Terrain terrain)
    {
      Put(TerrainGrid, oldOccupiedCells, null);
      Put(TerrainGrid, terrain.GetOccupiedCells(), terrain);
    }
    else if (entity is Prop prop)
    {
      Put(PropsGrid, oldOccupiedCells, null);
      Put(PropsGrid, prop.GetOccupiedCells(), prop);
    }
  }

  private void OnSpeedChanged(GridEntity entity)
  {
    if (entity is Unit)
      OnUnitsChanged();
  }

  private void OnDied(GridEntity entity)
  {
    entity.DeathRattle(UnitsGrid, TerrainGrid, PropsGrid, Units, RemovedUnits);
    if (entity is Unit unit)
    {
      RemoveUnit(unit);
      RemovedUnits.Add(unit);
      UnitDied?.Invoke();
    }
    else if (entity is Terrain terrain)
      Put(TerrainGrid, terrain.GetOccupiedCells(), null);
    else if (entity is Prop prop)
      Put(PropsGrid, prop.GetOccupiedCells(), null);
  }

  // Taken off the board without dying
  private void OnUnitRemoved(Unit unit)
  {
    RemoveUnit(unit);
    foreach (Vector2I cell in unit.GetOccupiedCells())
      RemoveWarningMark(cell);
  }
}
