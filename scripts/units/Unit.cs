using Godot;
using System;
using System.Collections.Generic;
using static Godot.TextServer;

public partial class Unit : GridEntity
{
  public bool SwitchedSides = false;
  public bool Stunned = false;
  public bool ExtraTurn = false;
  public UnitInfo StartUnitInfo = null!;

  // Turn processing variables
  public double ActingCooldown = 0.1f;
  public double ActingStartCooldown = 0.1f;
  public bool Paused = false;
  private int _turn = 0;
  private bool _acting = false;
  private bool _targeting = false;

  protected List<Unit> _units;
  protected List<Unit> _removedUnits;
  protected Unit[,] _unitsGrid;
  protected Terrain[,] _terrainGrid;
  protected Prop[,] _propsGrid;
  protected List<Vector2I> _selectedTargets;
  protected List<Vector2I> _activeUnitCells;

  protected OverlayLayer _activeUnitsLayer;
  protected OverlayLayer _targetedCellsLayer;

  private TextureProgressBar _cooldownBar;

  public void Initialize(UnitInfo unitInfo, bool side, Vector2I startCell, bool placed = false)
	{
    Damagable = true;
    Movable = true;
    Blocking = true;

		Id = unitInfo.Id;
		DisplayName = unitInfo.Name;
		ScenePath = unitInfo.ScenePath;
		MaxHealth = unitInfo.Health;
		Health = unitInfo.Health;
		Damage = unitInfo.Damage;
		Armor = unitInfo.Armor;
		Speed = unitInfo.Speed;
    StartingCooldown = unitInfo.StartCooldown;
		Cooldown = unitInfo.Cooldown;
		OccupiedCells = unitInfo.OccupiedCells;
		Texture = unitInfo.Texture;
		Cost = unitInfo.Cost;
		Description = unitInfo.Description;
		Rarity = unitInfo.Rarity;
    Types = unitInfo.Types;
    Stage = unitInfo.Stage;
		Side = side;
		StartCell = startCell;
		StartUnitInfo = unitInfo;
    _placed = placed;

    // TODO FIX
    _cooldownBar = GetNode<TextureProgressBar>("Cooldown");
    StartingCooldown *= 5;
    Cooldown *= 5;
    _cooldownBar.MaxValue = StartingCooldown;
  }

  public void SetPlayData(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> removedUnits, OverlayLayer activeUnitsLayer, OverlayLayer targetedCellsLayer)
  {
    _unitsGrid = unitsGrid;
    _terrainGrid = terrainGrid;
    _propsGrid = propsGrid;
    _units = units;
    _removedUnits = removedUnits;
    _activeUnitsLayer = activeUnitsLayer;
    _targetedCellsLayer = targetedCellsLayer;
  }

  public override void _Process(double delta)
  {
    // Process getting damaged
    base._Process(delta);

    if (Paused || _dead)
      return;

    if (_acting)
    {
      ActingCooldown -= delta;
      if (ActingCooldown <= 0)
      {
        Act(_selectedTargets, _unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
        // Reset units side to original side after acting
        if (SwitchedSides)
          SwitchSides();
        _acting = false;
        ActingCooldown = ActingStartCooldown;
        //_targetedCellsLayer.RemoveCells(_selectedTargets);
        //_activeUnitsLayer.RemoveCells(_activeUnitCells);
        _cooldownBar.Value = StartingCooldown - Cooldown;
      }
    }
    else if (_targeting)
    {
      ActingCooldown -= delta;
      if (ActingCooldown <= 0)
      {
        _selectedTargets = GetTargets(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
        //_targetedCellsLayer.AddCells(_selectedTargets);
        _acting = true;
        if (_selectedTargets.Count > 0)
          ActingCooldown = ActingStartCooldown;
        else
          ActingCooldown = 0; // If there are no targets, skip directly to acting phase
        _targeting = false;
      }
    }
  }

  public void PlayTurn()
  {
    if (CanAct())
    {
      // Show overlay on selected cells
      _activeUnitCells = GetOccupiedCells();
      //_activeUnitsLayer.AddCells(_activeUnitCells);
      _targeting = true;
    }
  }

  // Find closest unit that obeys certain clauses. These clauses are defined by the optional predicate.
  // Think of only finding friendly units, or only finding other units of the same type.
  protected Tuple<Unit, Vector2I>? GetClosestUnit(List<Unit> units, Func<Unit, bool>? predicate = null)
  {
    Vector2I? closestCell = null;
    float closestDist = float.MaxValue;
    Unit? closestUnit = null;

    if (units == null || units.Count == 0)
      return null;

    foreach (Unit unit in units)
    {
      if (unit == null || unit == this)
        continue;

      // Check predicate and continue if it does not hold
      if (predicate != null && !predicate(unit))
        continue;

      // Find the minimum distance between any pair of occupied cells
      foreach (Vector2I myCell in GetOccupiedCells())
      {
        foreach (Vector2I theirCell in unit.GetOccupiedCells())
        {
          float dist = myCell.DistanceTo(theirCell);
          if (dist < closestDist)
          {
            closestDist = dist;
            closestCell = theirCell;
            closestUnit = unit;
          }
        }
      }
    }

    if (closestCell.HasValue && closestUnit != null)
      return new Tuple<Unit, Vector2I>(closestUnit, closestCell.Value);
    else
      return null;
  }

  // Find farthest unit that obeys certain clauses. These clauses are defined by the optional predicate.
  // Think of only finding friendly units, or only finding other units of the same type.
  protected Tuple<Unit, Vector2I>? GetFarthestUnit(List<Unit> units, Func<Unit, bool>? predicate = null)
  {
    Vector2I? farthestCell = null;
    float farthestDist = float.MinValue;
    Unit? farthestUnit = null;

    if (units == null || units.Count == 0)
      return null;

    foreach (Unit unit in units)
    {
      if (unit == null || unit == this)
        continue;

      // Check predicate and continue if it does not hold
      if (predicate != null && !predicate(unit))
        continue;

      // Find the minimum distance between any pair of occupied cells
      foreach (Vector2I myCell in GetOccupiedCells())
      {
        foreach (Vector2I theirCell in unit.GetOccupiedCells())
        {
          float dist = myCell.DistanceTo(theirCell);
          if (dist > farthestDist)
          {
            farthestDist = dist;
            farthestCell = theirCell;
            farthestUnit = unit;
          }
        }
      }
    }

    if (farthestCell.HasValue && farthestUnit != null)
      return new Tuple<Unit, Vector2I>(farthestUnit, farthestCell.Value);
    else
      return null;
  }

  public UnitInfo GetInfo()
	{
		return new UnitInfo(Id, DisplayName, Texture, ScenePath, OccupiedCells, Cost, MaxHealth, 
			Health, Damage, Armor, Speed, StartingCooldown, Cooldown, Description, Rarity, Types, Stage);
	}

  public UnitInfo GetStartInfo()
  {
		return StartUnitInfo;
  }

  public override bool CanAct()
	{
    Cooldown -= 1;
    _cooldownBar.Value = StartingCooldown - Cooldown;
    if (Cooldown > 0)
      return false;
    Cooldown = StartingCooldown;
    
    if (Stunned)
    {
      SpawnFloatingText("Stunned", Colors.Red);
      Stunned = false;
      return false;
    }
    
    return true;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
	{
		foreach (Vector2I target in targets)
		{
			Unit targetUnit = unitsGrid[target.X, target.Y];
			if (targetUnit != null)
			{
				targetUnit.ChangeHealth(-Damage, this);
      }

      Prop targetProp = propsGrid[target.X, target.Y];
      if (targetProp != null)
      {
        targetProp.ChangeHealth(-Damage, this);
      }
    }
  }

	public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
	{
    if (Side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
				for (int x = 0; x < GlobalConstants.GridSize.X; x++)
				{
					if (unitsGrid[x, y] != null && unitsGrid[x, y].Side != Side)
						return [new Vector2I(x, y)];
				}
      }
    }
		else
		{
			for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
			{
				for (int x = 0; x < GlobalConstants.GridSize.X; x++)
				{ 
					if (unitsGrid[x, y] != null && unitsGrid[x, y].Side != Side)
						return [new Vector2I(x, y)];
				}
      }
    }

		return [];
  }

  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    _turn++;    
  }

  public override void Die()
  {
    base.Die();
    _cooldownBar.Visible = false;
  }

  public void SwitchSides()
  {
    SwitchedSides = !SwitchedSides;
    Side = !Side;
    SpawnFloatingText("Switched sides", Colors.Red);
    _globalSignals.EmitSignal(GlobalSignals.SignalName.SideChanged, this);
  }

  public void Stun()
  {
    Stunned = true;
    SpawnFloatingText("Stunned", Colors.Red);
  }

  public override void Remove()
  {
    if (!_dead && IsInstanceValid(this))
      _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitRemoved, this);
    QueueFree();
  }

  public float GetHealthPercentage()
  {
    return (float)Health / MaxHealth;
  }

  public static int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
