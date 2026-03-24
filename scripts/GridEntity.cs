using Godot;
using System;
using System.Collections.Generic;

public partial class GridEntity : Node2D
{
	// Stats
  public virtual int MaxHealth { get; set; } = 10;
  public virtual int Health { get; set; } = 10;
  public virtual int Damage { get; set; } = 1;
  public virtual int Armor { get; set; } = 0;
  public virtual int StartingCooldown { get; set; } = 1;
  public virtual int Cooldown { get; set; } = 1;
  public virtual int Speed { get; set; } = 1;

  // Info
  public string Id { get; set; } = null!;
  public string DisplayName { get; set; } = null!;
	public virtual List<Vector2I> OccupiedCells { get; set; } = [new Vector2I(0, 0)];
	public string Description { get; set; } = null!;
	public string Rarity { get; set; } = null!;
	public List<string> Types { get; set; } = null!;
  public int Cost { get; set; }
  public bool Movable { get; set; }
  public bool Damagable { get; set; }
  public bool Blocking { get; set; }
  public Vector2I OccupiedMainCell;
	public Vector2I StartCell;
  public Texture2D Texture = null!;
  public string ScenePath = null!;

  // Children
  protected Sprite2D _sprite = null!;
	private TextureProgressBar _healthBar = null!;

  // Private variables
  protected GlobalSignals _globalSignals = null!;
	private bool _affected = false;
	private double _damageIndicatorDuration = 0.3f;
  private double _damageIndicatorTime = 0.3f;
	private string _floatingTextPath = "res://scenes/FloatingText.tscn";
	private Queue<Tuple<string, Color>> _floatingTextQueue = new Queue<Tuple<string, Color>>();
  protected bool _dead = false;
  protected bool _placed = false;
  protected RandomNumberGenerator _rng = new RandomNumberGenerator();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
    _rng.Randomize();
    _sprite = GetNode<Sprite2D>("Sprite");
		_healthBar = GetNode<TextureProgressBar>("Health");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    _healthBar.MaxValue = MaxHealth;
    _healthBar.Value = Health;
    if (!Damagable)
      _healthBar.Hide();
    OccupiedMainCell = StartCell;
    Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(OccupiedCells);
		Vector2I relPos = GlobalFunctions.GetRelPosInCells(OccupiedCells, OccupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(OccupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
		_sprite.Texture = Texture;

    _globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntitySpawned, this, !_placed);

    Start();
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    if (!Damagable)
      return;

		if (_affected)
		{
			_damageIndicatorTime -= delta;
			if (_damageIndicatorTime <= 0)
			{
        ModulateSprite(new Color(1, 1, 1, 1));
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

	protected virtual void Start()
	{

  }

  public virtual bool CanAct()
	{
    return false;
  }

  public virtual void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid)
	{
  }

	public virtual List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
	{
    return [];
  }

  public virtual void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
  }

  public void Die()
	{
    _dead = true;
    DeathRattle();
    _globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntityDied, this);
    _healthBar.Hide();
    _sprite.Hide();
  }

  public virtual void Remove()
  {
    QueueFree();
  }

  public virtual void ChangeHealth(int amount, GridEntity? unit)
  {
		if (_dead || !Damagable)
			return;

		string displayedText = "";
		Color displayedColor = Colors.White;

		if (amount <= 0)
		{
			int damage = -amount;
      int effectiveDamage = Mathf.Max(0, damage - Armor);
      int damageTaken = Mathf.Min(effectiveDamage, Health);
			Health -= effectiveDamage;
			displayedText = effectiveDamage.ToString();
      _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageTaken, this, damageTaken);
      if (unit != null)
        _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageDealt, unit, damageTaken);
      if (Health <= 0)
      {
        Die();
      }
    }
		else
		{
      int effectiveHealing = Mathf.Min(MaxHealth - Health, amount);
      Health += effectiveHealing;
      displayedText = effectiveHealing.ToString();
			displayedColor = Colors.Green;
      _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingReceived, this, effectiveHealing);
      if (unit != null)
        _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingDone, unit, effectiveHealing);
    }
    _healthBar.Value = Health;

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(displayedText, displayedColor));
    else
    {
      _affected = true;
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(displayedText, displayedColor);
    }
  }

  public virtual void ChangeDamage(int amount)
  {
		if (_dead)
			return;

    Damage += amount;

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
      ModulateSprite(new Color(1, 1, 1, 0.5f));
			SpawnFloatingText(text, color);
		}
	}

  public virtual void ChangeMaxHealth(int amount)
  {
    if (_dead || !Damagable)
      return;

    MaxHealth += amount;
		Health += amount;
		_healthBar.MaxValue = MaxHealth;
    _healthBar.Value = Health;

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
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(text, color);
    }
  }

  public virtual void ChangeArmor(int amount)
  {
    if (_dead || !Damagable)
      return;

    Armor += amount;

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
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(text, color);
    }
  }

  public virtual void ChangeSpeed(int amount)
  {
    if (_dead)
      return;

    Speed += amount;

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
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(text, color);
    }
    _globalSignals.EmitSignal(GlobalSignals.SignalName.SpeedChanged, this);
  }

  public virtual void ChangeCooldown(int amount)
  {
    if (_dead)
      return;

    Cooldown += amount;

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
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(text, color);
    }
  }

  public void DeathRattle()
	{
		
	}

	public List<Vector2I> GetOccupiedCells()
	{
		List<Vector2I> occupiedCells = new List<Vector2I>();
		foreach (Vector2I relCell in OccupiedCells)
		{
			occupiedCells.Add(OccupiedMainCell + relCell);
    }
		return occupiedCells;
  }

	public void MoveToCell(Vector2I newCell, bool playing = false)
	{
    if (!Movable)
      return;

		Vector2I oldMainCell = OccupiedMainCell;
		if (!playing)
			StartCell = newCell;
    OccupiedMainCell = newCell;
		Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(OccupiedCells);
    Vector2I relPos = GlobalFunctions.GetRelPosInCells(OccupiedCells, OccupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(OccupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
		_globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntityMoved, this, oldMainCell, playing);
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

  public void ModulateSprite(Color color)
  {
    _sprite.Modulate = color;
  }
}
