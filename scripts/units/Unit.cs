using Godot;
using System;
using System.Collections.Generic;

public partial class Unit : GridEntity
{
  public bool SwitchedSides = false;
  public bool Stunned = false;
  public UnitInfo StartUnitInfo = null!;

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
    if (Stunned)
    {
      SpawnFloatingText("Stunned", Colors.Red);
      return false;
    }

    Cooldown -= 1;
    if (Cooldown > 0)
      return false;

    Cooldown = StartingCooldown;
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
    // Reset units side to original side upon turn end
    if (SwitchedSides)
      SwitchSides();

    // Unstun the unit upon turn end
    Stunned = false;
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
    _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitRemoved, this);
    QueueFree();
  }

  public float GetHealthPercentage()
  {
    return (float)Health / MaxHealth;
  }
}
