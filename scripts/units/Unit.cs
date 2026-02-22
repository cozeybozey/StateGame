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

  private Sprite2D _sprite;
	private TextureProgressBar _healthBar;
  private GlobalSignals _globalSignals;

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
	}

	private void Initialize()
	{
		_healthBar.MaxValue = maxHealth;
		health = maxHealth;
		_healthBar.Value = health;
  }

	public virtual void Act(List<Unit> units)
	{
		cooldown -= 1;
		if (cooldown <= 0)
		{
			List<Vector2I> targets = GetTargets(units);
			cooldown = startingCooldown;
		}
  }

	protected virtual List<Vector2I> GetTargets(List<Unit> units)
	{
		foreach (var unit in units)
		{
			if (unit.side != side)
			{
				unit.ChangeHealth(-damage);

				int cellX = Mathf.FloorToInt(unit.GlobalPosition.X / GlobalConstants.TileSize);
				int cellY = Mathf.FloorToInt(unit.GlobalPosition.Y / GlobalConstants.TileSize);

				return [new Vector2I(cellX, cellY)];
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

    _healthBar.Value = health;
  }

	public void DeathRattle()
	{
		
	}
}
