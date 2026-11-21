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
  private int _turn = 0;
  private int _telegraphTicksLeft = 0;  // > 0 while winding up an action
  private Node2D? _telegraph = null;
  private Color _actionColor = Colors.White;

  protected List<Unit> _units;
  protected List<Unit> _removedUnits;
  protected Unit[,] _unitsGrid;
  protected Terrain[,] _terrainGrid;
  protected Prop[,] _propsGrid;
  protected List<Vector2I> _selectedTargets;
  protected List<Vector2I> _activeUnitCells;

  protected OverlayLayer _activeUnitsLayer;
  protected OverlayLayer _targetedCellsLayer;

  private TextureProgressBar _actionBar;
  public int ActionProgress = 0;

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

    // Action bar (the "Cooldown" node in the scene): fills by Speed every tick, the unit acts when it is full.
    // Every unit, also ones spawned mid-battle, starts with an empty bar.
    _actionBar = GetNode<TextureProgressBar>("Cooldown");
    _actionBar.MaxValue = GlobalConstants.ActionBarSize;
    ResetActionBar();
  }

  public bool IsTelegraphing => _telegraphTicksLeft > 0;

  // Move only if the destination is still free. Targets are picked when the wind-up starts, so another
  // unit may have moved into the cell in the meantime (two units can never share a cell).
  protected bool TryMoveToCell(Vector2I cell)
  {
    if (!GlobalFunctions.CanMoveToCell(this, cell, _unitsGrid, _terrainGrid, _propsGrid))
      return false;
    MoveToCell(cell, true);
    return true;
  }

  public void ResetActionBar()
  {
    _telegraphTicksLeft = 0;
    ActionProgress = 0;
    if (_actionBar != null)
      _actionBar.Value = 0;
  }

  // Number of ticks from now until this unit's k-th next action (k = 1 is the next one), assuming its speed stays the same.
  // Mirrors SimTick: the bar gains Speed per tick and the unit acts on the tick it reaches a multiple of ActionBarSize.
  public int TicksUntilAction(int k)
  {
    if (Speed <= 0)
      return int.MaxValue;
    long needed = (long)k * GlobalConstants.ActionBarSize - ActionProgress;
    if (needed <= 0)
      return 1;
    return (int)((needed + Speed - 1) / Speed);
  }

  // Fill (or drain) the action bar, e.g. "this ally acts sooner". Amount is in action bar points.
  public void ChangeActionProgress(int amount, string text)
  {
    if (_dead)
      return;
    ActionProgress = Mathf.Clamp(ActionProgress + amount, 0, GlobalConstants.ActionBarSize);
    if (_actionBar != null)
      _actionBar.Value = ActionProgress;
    SpawnFloatingText(text, amount >= 0 ? Colors.Yellow : Colors.Red);
  }

  // Small number in the unit's top-left corner showing its place in the turn order (placement phase only)
  private Label? _turnOrderLabel = null;

  public void ShowTurnOrder(int? order)
  {
    if (order == null)
    {
      if (_turnOrderLabel != null)
        _turnOrderLabel.Visible = false;
      return;
    }

    if (_turnOrderLabel == null)
    {
      _turnOrderLabel = new Label
      {
        ZIndex = 20,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        CustomMinimumSize = new Vector2(14, 14),
      };
      _turnOrderLabel.AddThemeFontSizeOverride("font_size", 11);
      _turnOrderLabel.AddThemeColorOverride("font_color", Colors.White);
      AddChild(_turnOrderLabel);
    }

    // Solid badge in the side's color (blue = player, red = enemy) so the number reads on any tile
    StyleBoxFlat badge = new StyleBoxFlat
    {
      BgColor = Side ? new Color(0.15f, 0.45f, 0.85f) : new Color(0.8f, 0.2f, 0.15f),
      BorderColor = new Color(0, 0, 0, 0.9f),
      ContentMarginLeft = 3,
      ContentMarginRight = 3,
      ContentMarginTop = 0,
      ContentMarginBottom = 0,
    };
    badge.SetBorderWidthAll(1);
    badge.SetCornerRadiusAll(7);
    _turnOrderLabel.AddThemeStyleboxOverride("normal", badge);
    _turnOrderLabel.Text = order.Value.ToString();
    _turnOrderLabel.Size = Vector2.Zero; // Shrink to fit the new text
    _turnOrderLabel.Size = _turnOrderLabel.GetCombinedMinimumSize();

    // Center the badge on the top-left corner of the unit's bounding box (GlobalPosition is the box center)
    Vector2I dims = GlobalFunctions.CellsToDimensions(OccupiedCells);
    Vector2 corner = new Vector2(-dims.X * GlobalConstants.TileSize * 0.5f + 3, -dims.Y * GlobalConstants.TileSize * 0.5f + 3);
    _turnOrderLabel.Position = corner - _turnOrderLabel.Size / 2;
    _turnOrderLabel.Visible = true;
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

  // Called by World once per simulation tick
  public void SimTick()
  {
    if (_dead)
      return;

    // The bar keeps filling while winding up, so a unit's action rate is exactly its speed.
    // Capped at two full bars so a unit can never bank many actions.
    ActionProgress = Mathf.Min(ActionProgress + Mathf.Max(0, Speed), 2 * GlobalConstants.ActionBarSize);

    if (_telegraphTicksLeft > 0)
    {
      _telegraphTicksLeft--;
      if (_telegraphTicksLeft == 0)
        Strike();
    }
    else if (CanAct())
      BeginAction();

    if (_actionBar != null && IsInstanceValid(_actionBar))
      _actionBar.Value = ActionProgress;
  }

  // Pick targets and show a telegraph; the actual action happens TelegraphTicks later
  private void BeginAction()
  {
    _selectedTargets = GetTargets(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
    PulseActionBar();

    if (_selectedTargets.Count == 0)
    {
      // Nothing to telegraph, act immediately
      Act(_selectedTargets, _unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
      AfterAct();
      return;
    }

    _actionColor = CombatFx.GetActionColor(this, _selectedTargets, _unitsGrid);
    _telegraph = CombatFx.Instance?.ShowTelegraph(this, _selectedTargets, _actionColor,
      (float)GlobalConstants.TelegraphTicks / GlobalConstants.TicksPerSecond + 0.2f);
    _telegraphTicksLeft = GlobalConstants.TelegraphTicks;
  }

  private void Strike()
  {
    CombatFx? fx = CombatFx.Instance;
    fx?.ClearTelegraph(_telegraph);
    _telegraph = null;

    // Collect target units before acting, so reactions also play on units that die from this action
    List<Unit> targetUnits = new();
    foreach (Vector2I cell in _selectedTargets)
    {
      if (!GlobalFunctions.IsCellInsideGrid(cell))
        continue;
      Unit target = _unitsGrid[cell.X, cell.Y];
      if (target != null && target != this && !targetUnits.Contains(target))
        targetUnits.Add(target);
    }

    fx?.ShowStrike(this, _selectedTargets, _actionColor);
    PlayLunge(_selectedTargets);

    Act(_selectedTargets, _unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);

    foreach (Unit target in targetUnits)
    {
      if (!IsInstanceValid(target))
        continue;
      if (target.Side == Side)
        target.PlaySupportReaction();
      else
        target.PlayHitReaction(GlobalPosition);
    }

    AfterAct();
  }

  private void AfterAct()
  {
    // Reset units side to original side after acting
    if (SwitchedSides)
      SwitchSides();
  }

  private void PlayLunge(List<Vector2I> targets)
  {
    List<Vector2I> ownCells = GetOccupiedCells();
    Vector2? targetPos = null;
    foreach (Vector2I cell in targets)
    {
      if (!ownCells.Contains(cell))
      {
        targetPos = CombatFx.CellCenter(cell);
        break;
      }
    }
    if (targetPos == null)
    {
      PlaySupportReaction();  // Self-targeted: small pulse instead of a lunge
      return;
    }

    Vector2 dir = (targetPos.Value - GlobalPosition).Normalized();
    float t = FxTimeScale();
    Tween tween = RestartSpriteTween();
    tween.TweenProperty(_sprite, "position", dir * 7.0f, 0.06f * t).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    tween.TweenProperty(_sprite, "position", Vector2.Zero, 0.14f * t).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
  }

  private void PulseActionBar()
  {
    if (_actionBar == null || !IsInstanceValid(_actionBar))
      return;
    float t = FxTimeScale();
    _actionBar.PivotOffset = _actionBar.Size / 2;
    Tween tween = _actionBar.CreateTween();
    tween.TweenProperty(_actionBar, "modulate", new Color(2.5f, 2.5f, 2.5f), 0.04f * t);
    tween.Parallel().TweenProperty(_actionBar, "scale", new Vector2(1.15f, 2.2f), 0.04f * t);
    tween.TweenProperty(_actionBar, "modulate", Colors.White, 0.2f * t);
    tween.Parallel().TweenProperty(_actionBar, "scale", Vector2.One, 0.2f * t);
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
			Health, Damage, Armor, Speed, Description, Rarity, Types, Stage);
	}

  public UnitInfo GetStartInfo()
  {
		return StartUnitInfo;
  }

  public override bool CanAct()
	{
    // Act when the action bar is full; the overflow carries over so the action rate matches Speed exactly
    if (ActionProgress < GlobalConstants.ActionBarSize)
      return false;
    ActionProgress -= GlobalConstants.ActionBarSize;

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
    _actionBar.Visible = false;
    _telegraphTicksLeft = 0;
    CombatFx.Instance?.ClearTelegraph(_telegraph);
    _telegraph = null;
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
