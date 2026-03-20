using Godot;
using System;
using System.Collections.Generic;

public partial class Unit : Node2D
{
	// Stats
  public virtual int maxHealth { get; set; } = 10;
  public virtual int health { get; set; } = 10;
  public virtual int damage { get; set; } = 1;
  public virtual int armor { get; set; } = 0;
  public virtual int startingCooldown { get; set; } = 1;
  public virtual int cooldown { get; set; } = 1;
  public virtual int speed { get; set; } = 1;
  
	// Info
	public string id { get; set; }
	public string name { get; set; }
	public virtual List<Vector2I> occupiedCells { get; set; } = [new Vector2I(0, 0)];
	public string description { get; set; }
	public string rarity { get; set; }
  public bool side = true; // true for player, false for enemy
  public bool SwitchedSides = false;
  public Vector2I occupiedMainCell;
	public Vector2I startCell;
	public Texture2D texture;
	public string scenePath;
	public int cost { get; set; }
	public UnitInfo startUnitInfo;

  protected Sprite2D _sprite;
	private TextureProgressBar _healthBar;
  protected GlobalSignals _globalSignals;
	private bool _affected = false;
	private double _damageIndicatorDuration = 0.3f;
  private double _damageIndicatorTime = 0.3f;
	private string _floatingTextPath = "res://scenes/FloatingText.tscn";
	private Queue<Tuple<string, Color>> _floatingTextQueue = new Queue<Tuple<string, Color>>();
	private bool _dead = false;
  private bool _placed = false;
  protected RandomNumberGenerator _rng = new RandomNumberGenerator();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
    _rng.Randomize();
    _sprite = GetNode<Sprite2D>("Sprite");
		_healthBar = GetNode<TextureProgressBar>("Health");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    _healthBar.MaxValue = maxHealth;
    _healthBar.Value = health;
    occupiedMainCell = startCell;
    Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(occupiedCells);
		Vector2I relPos = GlobalFunctions.GetRelPosInCells(occupiedCells, occupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
		_sprite.Texture = texture;
    //if (!side)
    //  _sprite.FlipV = true; // Flip the sprite for enemy units

    _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitSpawned, this, !_placed);

    Start();
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_affected)
		{
			_damageIndicatorTime -= delta;
			if (_damageIndicatorTime <= 0)
			{
				_sprite.Modulate = new Color(1, 1, 1, 1);
				_damageIndicatorTime = _damageIndicatorDuration;
				if (_floatingTextQueue.Count > 0)
				{
					Tuple<string, Color> floatingTextInfo = _floatingTextQueue.Dequeue();
					SpawnFloatingText(floatingTextInfo.Item1, floatingTextInfo.Item2);
				}
				else
				{
					_affected = false;
					if (_dead)
						QueueFree();
        }
      }
    }
  }

	public void Initialize(UnitInfo unitInfo, bool _side, Vector2I _startCell, bool placed = false)
	{
		id = unitInfo.Id;
		name = unitInfo.Name;
		scenePath = unitInfo.ScenePath;
		maxHealth = unitInfo.Health;
		health = unitInfo.Health;
		damage = unitInfo.Damage;
		armor = unitInfo.Armor;
		speed = unitInfo.Speed;
    startingCooldown = unitInfo.StartCooldown;
		cooldown = unitInfo.Cooldown;
		occupiedCells = unitInfo.OccupiedCells;
		texture = unitInfo.Texture;
		cost = unitInfo.Cost;
		description = unitInfo.Description;
		rarity = unitInfo.Rarity;
		side = _side;
		startCell = _startCell;
		startUnitInfo = unitInfo;
    _placed = placed;
  }

	protected virtual void Start()
	{

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
		return new UnitInfo(id, name, texture, scenePath, occupiedCells, cost, maxHealth, 
			health, damage, armor, speed, startingCooldown, cooldown, description, rarity);
	}

  public UnitInfo GetStartInfo()
  {
		return startUnitInfo;
  }

  public virtual bool CanAct()
	{
    cooldown -= 1;
    if (cooldown > 0)
      return false;

    cooldown = startingCooldown;
		return true;
  }

  public virtual void Act(List<Vector2I> targets, Unit[,] unitsGrid)
	{
		foreach (Vector2I target in targets)
		{
			Unit targetUnit = unitsGrid[target.X, target.Y];
			if (targetUnit != null)
			{
				targetUnit.ChangeHealth(-damage, this);
      }
    }
  }

	public virtual List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
	{
    if (side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
				for (int x = 0; x < GlobalConstants.GridSize.X; x++)
				{
					if (unitsGrid[x, y] != null && unitsGrid[x, y].side != side)
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
					if (unitsGrid[x, y] != null && unitsGrid[x, y].side != side)
						return [new Vector2I(x, y)];
				}
      }
    }

		return [];
  }

  public virtual void TurnEnd(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Reset units side to original side upon turn end
    if (SwitchedSides)
    {
      SwitchSides();
    }
  }

  public void SwitchSides()
  {
    SwitchedSides = !SwitchedSides;
    side = !side;
    SpawnFloatingText("Switched sides", Colors.Red);
    _globalSignals.EmitSignal(GlobalSignals.SignalName.SideChanged, this);
  }

  public void Die()
	{
    _dead = true;
    DeathRattle();
    _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitDied, this);
    _healthBar.Hide();
    _sprite.Hide();
  }

  public void Remove()
  {
    _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitRemoved, this);
    QueueFree();
  }

  public virtual void ChangeHealth(int amount, Unit? unit)
  {
		if (_dead)
			return;

		string displayedText = "";
		Color displayedColor = Colors.White;

		if (amount <= 0)
		{
			int damage = -amount;
      int effectiveDamage = Mathf.Max(0, damage - armor);
      int damageTaken = Mathf.Min(effectiveDamage, health);
			health -= effectiveDamage;
			displayedText = effectiveDamage.ToString();
      _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageTaken, this, damageTaken);
      if (unit != null)
        _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageDealt, unit, damageTaken);
      if (health <= 0)
      {
        Die();
      }
    }
		else
		{
      int effectiveHealing = Mathf.Min(maxHealth - health, amount);
      health += effectiveHealing;
      displayedText = effectiveHealing.ToString();
			displayedColor = Colors.Green;
      _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingReceived, this, effectiveHealing);
      if (unit != null)
        _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingDone, unit, effectiveHealing);
    }
    _healthBar.Value = health;

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(displayedText, displayedColor));
    else
    {
      _affected = true;
      _sprite.Modulate = new Color(1, 1, 1, 0.5f);
      SpawnFloatingText(displayedText, displayedColor);
    }
  }

  public virtual void ChangeDamage(int amount)
  {
		if (_dead)
			return;

    damage += amount;

		string text = $"+{amount} Damage";
		Color color = Colors.Yellow;
		if (amount < 0)
		{
			text = $"{amount} Damage";
			color = Colors.Red;
    }

    if (_affected)
			_floatingTextQueue.Enqueue(new Tuple<string, Color>(text, color));
		else
		{
			_affected = true;
			_sprite.Modulate = new Color(1, 1, 1, 0.5f);
			SpawnFloatingText(text, color);
		}
	}

  public virtual void ChangeMaxHealth(int amount)
  {
    if (_dead)
      return;

    maxHealth += amount;
		health += amount;
		_healthBar.MaxValue = maxHealth;
    _healthBar.Value = health;

    string text = $"+{amount} Health";
    Color color = Colors.Yellow;
    if (amount < 0)
    {
      text = $"{amount} Health";
      color = Colors.Red;
    }

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(text, color));
    else
    {
      _affected = true;
      _sprite.Modulate = new Color(1, 1, 1, 0.5f);
      SpawnFloatingText(text, color);
    }
  }

  public virtual void ChangeArmor(int amount)
  {
    if (_dead)
      return;

    armor += amount;

    string text = $"+{amount} Armor";
    Color color = Colors.Yellow;
    if (amount < 0)
    {
      text = $"{amount} Armor";
      color = Colors.Red;
    }

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(text, color));
    else
    {
      _affected = true;
      _sprite.Modulate = new Color(1, 1, 1, 0.5f);
      SpawnFloatingText(text, color);
    }
  }

  public virtual void ChangeSpeed(int amount)
  {
    if (_dead)
      return;

    speed += amount;

    string text = $"+{amount} Speed";
    Color color = Colors.Yellow;
    if (amount < 0)
    {
      text = $"{amount} Speed";
      color = Colors.Red;
    }

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(text, color));
    else
    {
      _affected = true;
      _sprite.Modulate = new Color(1, 1, 1, 0.5f);
      SpawnFloatingText(text, color);
    }
    _globalSignals.EmitSignal(GlobalSignals.SignalName.SpeedChanged, this);
  }

  public virtual void ChangeCooldown(int amount)
  {
    if (_dead)
      return;

    cooldown += amount;

    string text = $"+{amount} Cooldown";
    Color color = Colors.Red;
    if (amount < 0)
    {
      text = $"{amount} Cooldown";
      color = Colors.Yellow;
    }

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(text, color));
    else
    {
      _affected = true;
      _sprite.Modulate = new Color(1, 1, 1, 0.5f);
      SpawnFloatingText(text, color);
    }
  }

  public void DeathRattle()
	{
		
	}

	public List<Vector2I> GetOccupiedCells()
	{
		List<Vector2I> occupiedCells = new List<Vector2I>();
		foreach (Vector2I relCell in this.occupiedCells)
		{
			occupiedCells.Add(occupiedMainCell + relCell);
    }
		return occupiedCells;
  }

	public void MoveToCell(Vector2I newCell, bool playing = false)
	{
		Vector2I oldMainCell = occupiedMainCell;
		if (!playing)
			startCell = newCell;
    occupiedMainCell = newCell;
		Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(occupiedCells);
    Vector2I relPos = GlobalFunctions.GetRelPosInCells(occupiedCells, occupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
		_globalSignals.EmitSignal(GlobalSignals.SignalName.UnitMoved, this, oldMainCell, playing);
  }

  public void SpawnFloatingText(string text, Color color = new Color())
  {
    PackedScene scene = GD.Load<PackedScene>(_floatingTextPath);
    FloatingText floatingText = scene.Instantiate<FloatingText>();

    floatingText.Text = text;
		floatingText.Modulate = color;
    floatingText.GlobalPosition = GlobalPosition - new Vector2(0, 0.5f * GlobalConstants.TileSize);

    GetTree().CurrentScene.AddChild(floatingText);
  }
}
