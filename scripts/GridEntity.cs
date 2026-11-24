using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GridEntity : Node2D
{
	// Stats
  public virtual int MaxHealth { get; set; } = 10;
  public virtual int Health { get; set; } = 10;
  public virtual int Damage { get; set; } = 1;
  public virtual int Armor { get; set; } = 0;
  public virtual int Speed { get; set; } = 1;

  // Info
  public string Id { get; set; } = null!;
  public string DisplayName { get; set; } = null!;
	public virtual List<Vector2I> OccupiedCells { get; set; } = [new Vector2I(0, 0)];
	public string Description { get; set; } = null!;
	public string Rarity { get; set; } = null!;
	public List<string> Types { get; set; } = null!;
  public int Cost { get; set; }
  public int Stage { get; set; }
  public bool Movable { get; set; }
  public bool Damagable { get; set; }
  public bool Blocking { get; set; }
  public bool Side = true; // true for player, false for enemy

  public Vector2I OccupiedMainCell;
	public Vector2I StartCell;
  public Texture2D Texture = null!;
  public string ScenePath = null!;

  // Children
  protected Sprite2D _sprite = null!;
	protected TextureProgressBar _healthBar = null!;

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

    LayoutBars();
    _healthBar.MaxValue = MaxHealth;
    UpdateHealthBar();
    if (!Damagable)
      _healthBar.Hide();
    OccupiedMainCell = StartCell;
    Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(OccupiedCells);
		Vector2I relPos = GlobalFunctions.GetRelPosInCells(OccupiedCells, OccupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(OccupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
		_sprite.Texture = Texture;
    UpdateHealthBar();

    _globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntitySpawned, this, !_placed);

    Start();
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    AdvanceMove((float)delta);

    if (Damagable)
    {
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
  }

	protected virtual void Start()
	{

  }

  public virtual bool CanAct()
	{
    return false;
  }

  public virtual void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
	{
  }

	public virtual List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
	{
    return [];
  }

  public virtual void GameStart()
  {
  }

  public virtual void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
  }

  public virtual void DeathRattle(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
  }

  // Rest scale of the sprite, which the hit and buff reactions return to
  protected Vector2 _restScale = Vector2.One;

  protected Vector2 GetPixelSize()
  {
    return GlobalFunctions.CellsToDimensions(OccupiedCells) * GlobalConstants.TileSize;
  }

  // The health bar runs along the top row of the entity's cells, so it never floats next to an L shape
  public virtual void LayoutBars()
  {
    if (!Damagable)
      return;

    PlaceBar(_healthBar, bottom: false);
  }

  protected void PlaceBar(Control bar, bool bottom)
  {
    Vector2 size = GetPixelSize();
    int tile = GlobalConstants.TileSize;
    int minX = OccupiedCells.Min(c => c.X);
    int rowY = bottom ? OccupiedCells.Max(c => c.Y) : OccupiedCells.Min(c => c.Y);
    List<int> rowCells = OccupiedCells.Where(c => c.Y == rowY).Select(c => c.X - minX).ToList();
    float left = -size.X / 2 + rowCells.Min() * tile + 3;
    float right = -size.X / 2 + (rowCells.Max() + 1) * tile - 3;
    float y = bottom ? size.Y / 2 - 5 : -size.Y / 2 + 1;
    bar.Position = new Vector2(left, y) + VisualOffset;
    bar.Size = new Vector2(right - left, 4);
  }

  // Where the entity is drawn relative to its real position. Moving changes GlobalPosition at once, which the
  // game logic relies on, while this offset shrinks to zero so the entity visibly travels to its new cell.
  private Vector2 _visualOffset = Vector2.Zero;
  private Vector2 _moveStart = Vector2.Zero;
  private float _moveDuration = 0;
  private float _moveTime = 0;
  public Vector2 VisualOffset
  {
    get => _visualOffset;
    protected set
    {
      _visualOffset = value;
      _sprite.Offset = value / _restScale;
      LayoutBars();
    }
  }

  private void PlayMove(Vector2 fromGlobalPosition)
  {
    StopMove();
    Vector2 start = fromGlobalPosition - GlobalPosition;
    if (start == Vector2.Zero)
      return;

    _moveDuration = Mathf.Max(0.05f, 0.9f * GlobalConstants.MoveTicks / GlobalConstants.TicksPerSecond / (CombatFx.Instance?.GameSpeed ?? 1.0f));
    _moveTime = 0;
    _moveStart = start;
    VisualOffset = start;
    ZIndex = 5;
  }

  // Runs in _Process and not in a tween, because tweens update after nodes redraw and the unit's outline would trail it
  private void AdvanceMove(float delta)
  {
    if (_moveDuration <= 0)
      return;

    _moveTime += delta;
    float t = _moveTime / _moveDuration;
    if (t >= 1)
    {
      StopMove();
      return;
    }
    float eased = 0.5f - 0.5f * Mathf.Cos(t * Mathf.Pi);
    VisualOffset = _moveStart * (1 - eased) + Vector2.Up * Mathf.Sin(eased * Mathf.Pi) * 10;
  }

  protected void StopMove()
  {
    _moveDuration = 0;
    ZIndex = 0;
    VisualOffset = Vector2.Zero;
  }

  // Direction and distance the sprite travels while its death plays
  protected virtual Vector2 GetDeathDrift()
  {
    return Vector2.Zero;
  }

  protected virtual void UpdateHealthBar()
  {
    _healthBar.Value = Health;
    _healthBar.Visible = Damagable && !_dead && Health < MaxHealth;
  }

  public virtual void Die()
	{
    _dead = true;
    _globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntityDied, this);
    _healthBar.Hide();
    CombatFx.Instance?.ShowDeath(_sprite, VisualOffset, GetDeathDrift());
    _sprite.Hide();
  }

  public virtual void Remove()
  {
    QueueFree();
  }

  public virtual int ChangeHealth(int amount, GridEntity? unit)
  {
    if (_dead || !Damagable)
      return 0;

    string displayedText = "";
    Color displayedColor = Colors.White;
    int effectiveAmount = amount;

    if (amount <= 0)
    {
      int damage = -amount;
      int effectiveDamage = Mathf.Max(0, damage - Armor);
      int damageTaken = Mathf.Min(effectiveDamage, Health);
      Health -= effectiveDamage;
      displayedText = effectiveDamage.ToString();

      effectiveAmount = damageTaken;
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
      if (unit != null && this is Unit) // Only track healing done to units
        _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingDone, unit, effectiveHealing);
      effectiveAmount = effectiveHealing;
    }
    UpdateHealthBar();

    if (_affected)
      _floatingTextQueue.Enqueue(new Tuple<string, Color>(displayedText, displayedColor));
    else
    {
      _affected = true;
      ModulateSprite(new Color(1, 1, 1, 0.5f));
      SpawnFloatingText(displayedText, displayedColor);
    }

    // Emit signals after everything is done
    if (amount <= 0)
    {
      _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageTaken, this, effectiveAmount);
      if (unit != null && this is Unit)  // Only track damage dealt to units
        _globalSignals.EmitSignal(GlobalSignals.SignalName.DamageDealt, unit, effectiveAmount);
    }
    else
    {
      _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingReceived, this, effectiveAmount);
      if (unit != null && this is Unit) // Only track healing done to units
        _globalSignals.EmitSignal(GlobalSignals.SignalName.HealingDone, unit, effectiveAmount);
    }

    return effectiveAmount;
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
    UpdateHealthBar();

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

	public List<Vector2I> GetOccupiedCells()
	{
		List<Vector2I> occupiedCells = new List<Vector2I>();
		foreach (Vector2I relCell in OccupiedCells)
		{
			occupiedCells.Add(OccupiedMainCell + relCell);
    }
		return occupiedCells;
  }

  public List<Vector2I> GetSurroundingCells(bool includeFront = true, bool includeBack = true, bool includeLeft = true, bool includeRight = true, bool includeDiagonals = false)
  {
    return GlobalFunctions.GetSurroundingCells(OccupiedMainCell, OccupiedCells, Side, includeFront, includeBack, includeLeft, includeRight, includeDiagonals);
  }

  public void MoveToCell(Vector2I newCell, bool playing)
	{
    if (!Movable)
      return;

		Vector2I oldMainCell = OccupiedMainCell;
    Vector2 oldVisualPosition = GlobalPosition + VisualOffset;
		if (!playing)
			StartCell = newCell;
    OccupiedMainCell = newCell;
		Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(OccupiedCells);
    Vector2I relPos = GlobalFunctions.GetRelPosInCells(OccupiedCells, OccupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(OccupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
    if (playing)
      PlayMove(oldVisualPosition);
		_globalSignals.EmitSignal(GlobalSignals.SignalName.GridEntityMoved, this, oldMainCell, playing);
  }

  public void SpawnFloatingText(string text, Color color = new Color())
  {
    PackedScene scene = GD.Load<PackedScene>(_floatingTextPath);
    FloatingText floatingText = scene.Instantiate<FloatingText>();

    floatingText.Text = text;
		floatingText.Modulate = color;
    floatingText.GlobalPosition = GlobalPosition + VisualOffset - new Vector2(0, 0.5f * GlobalConstants.TileSize);

    GetTree().CurrentScene.AddChild(floatingText);
  }

  public void ModulateSprite(Color color)
  {
    _sprite.Modulate = color;
  }

  // ---- Procedural combat motion (sprite tweens) ----
  private Tween? _spriteTween = null;

  protected static float FxTimeScale()
  {
    return CombatFx.Instance?.TimeScale ?? 1.0f;
  }

  // Kill any running sprite motion so lunges / reactions never stack and always return to rest
  protected Tween RestartSpriteTween()
  {
    if (_spriteTween != null && _spriteTween.IsValid())
      _spriteTween.Kill();
    _sprite.Scale = _restScale;
    _spriteTween = CreateTween();
    return _spriteTween;
  }

  // Knock the sprite away from the attacker and back
  public void PlayHitReaction(Vector2 fromPosition)
  {
    if (_dead || _sprite == null)
      return;
    Vector2 dir = (GlobalPosition - fromPosition).Normalized();
    if (dir == Vector2.Zero)
      dir = Vector2.Up;
    float t = FxTimeScale();
    Tween tween = RestartSpriteTween();
    tween.TweenProperty(_sprite, "position", dir * 4.0f, 0.04f * t);
    tween.TweenProperty(_sprite, "position", -dir * 1.5f, 0.06f * t);
    tween.TweenProperty(_sprite, "position", Vector2.Zero, 0.08f * t);
  }

  // Small "pop" for buffs / heals
  public void PlaySupportReaction()
  {
    if (_dead || _sprite == null)
      return;
    float t = FxTimeScale();
    Tween tween = RestartSpriteTween();
    _sprite.Position = Vector2.Zero;
    tween.TweenProperty(_sprite, "scale", _restScale * 1.2f, 0.06f * t);
    tween.TweenProperty(_sprite, "scale", _restScale, 0.14f * t);
  }
}
