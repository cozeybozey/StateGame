using Godot;
using System;
using System.Collections.Generic;

public partial class Unit : Node2D
{
  public virtual int maxHealth { get; set; } = 10;
  public virtual int health { get; set; } = 10;
  public virtual int damage { get; set; } = 1;
  public virtual int armor { get; set; } = 0;
  public virtual int startingCooldown { get; set; } = 1;
  public virtual int cooldown { get; set; } = 1;
  public virtual int speed { get; set; } = 1;
	public bool side = true; // true for player, false for enemy
  public Vector2I occupiedMainCell;

  private Sprite2D _sprite;
	private TextureProgressBar _healthBar;
  private GlobalSignals _globalSignals;
	private bool _affected = false;
	private double _damageIndicatorDuration = 0.2f;
  private double _damageIndicatorTime = 0.2f;
	private List<Vector2I> _extraOccupiedCells = new List<Vector2I>();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite");
		_healthBar = GetNode<TextureProgressBar>("Health");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    Initialize();
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
				_affected = false;
				_damageIndicatorTime = _damageIndicatorDuration;
			}
    }
  }

	private void Initialize()
	{
		_healthBar.MaxValue = maxHealth;
		health = maxHealth;
		_healthBar.Value = health;
		GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell);
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
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
		{
			if (side)
			{
				for (int y = GlobalConstants.GridSize.Y - 1; y > 0; y--)
				{
					if (unitsGrid[x, y] != null && unitsGrid[x, y].side != side)
						return [new Vector2I(x, y)];
				}
			}
			else
			{
				for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
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
		if (amount <= 0)
		{
			int damage = -amount;
      int effectiveDamage = Mathf.Max(0, damage - armor);
			health -= effectiveDamage;
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
    }

		_affected = true;
		_sprite.Modulate = new Color(1, 1, 1, 0.5f);
    _healthBar.Value = health;
  }

	public void DeathRattle()
	{
		
	}

	public List<Vector2I> GetOccupiedCells()
	{
		List<Vector2I> occupiedCells = new List<Vector2I> { occupiedMainCell };
		foreach (Vector2I relCell in _extraOccupiedCells)
		{
			occupiedCells.Add(occupiedMainCell + relCell);
    }
		return occupiedCells;
  }

	public void MoveToCell(Vector2I newCell)
	{
		occupiedMainCell = newCell;
		GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell);
  }
}
