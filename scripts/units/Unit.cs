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
  public bool side = true; // true for player, false for enemy
  public Vector2I occupiedMainCell;
	public Vector2I startCell;
	public Texture2D texture;
	public string scenePath;
	public int cost { get; set; }
	public UnitInfo startUnitInfo;

  private Sprite2D _sprite;
	private TextureProgressBar _healthBar;
  private GlobalSignals _globalSignals;
	private bool _affected = false;
	private double _damageIndicatorDuration = 0.3f;
  private double _damageIndicatorTime = 0.3f;
	private string _floatingTextPath = "res://scenes/FloatingText.tscn";
	private Queue<Tuple<string, Color>> _floatingTextQueue = new Queue<Tuple<string, Color>>();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite");
		_healthBar = GetNode<TextureProgressBar>("Health");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    _healthBar.MaxValue = maxHealth;
    _healthBar.Value = health;
    occupiedMainCell = startCell;
    Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(occupiedCells);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell, cellDimensions.X, cellDimensions.Y);
		_sprite.Texture = texture;
    if (!side)
      _sprite.FlipV = true; // Flip the sprite for enemy units

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
        }
      }
    }
  }

	public void Initialize(UnitInfo unitInfo, bool _side, Vector2I _startCell)
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
		side = _side;
		startCell = _startCell;
		startUnitInfo = unitInfo;
	}

	protected virtual void Start()
	{

  }

	public UnitInfo GetInfo()
	{
		return new UnitInfo(id, name, texture, scenePath, occupiedCells, cost, maxHealth, 
			health, damage, armor, speed, startingCooldown, cooldown, description);
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
				targetUnit.ChangeHealth(-damage);
      }
    }
  }

	public virtual List<Vector2I> GetTargets(Unit[,] unitsGrid)
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

  public void ChangeHealth(int amount)
  {
		string displayedText = amount.ToString();
		Color displayedColor = Colors.White;

		if (amount <= 0)
		{
			int damage = -amount;
      int effectiveDamage = Mathf.Max(0, damage - armor);
			health -= effectiveDamage;
			displayedText = effectiveDamage.ToString();
      if (health <= 0)
      {
        DeathRattle();
        _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitDied, this);
      }
    }
		else
		{
			health += amount;
			if (health > maxHealth)
			health = maxHealth;
			displayedColor = Colors.Green;
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

  public void ChangeDamage(int amount)
  {
    damage += amount;

		if (_affected)
			_floatingTextQueue.Enqueue(new Tuple<string, Color>($"+{amount} Damage", Colors.Yellow));
		else
		{
			_affected = true;
			_sprite.Modulate = new Color(1, 1, 1, 0.5f);
			SpawnFloatingText($"+{amount} Damage", Colors.Yellow);
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
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell, cellDimensions.X, cellDimensions.Y);
		if (playing)
			_globalSignals.EmitSignal(GlobalSignals.SignalName.UnitMoved, this, oldMainCell);
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
