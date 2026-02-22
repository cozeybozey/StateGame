using Godot;
using System;
using System.Collections.Generic;

public partial class Unit : Node2D
{
  public int maxHealth = 10;
  public int health = 10;
  public int damage = 1;
  public int armor = 0;
  public int startingCooldown = 1;
  public int cooldown = 1;
  public int speed = 1;
	public bool side = true; // true for player, false for enemy
	public UnitInfo unitInfo;

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
		_sprite.Texture = unitInfo.Texture;
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = health;
  }

	public void Act(List<Unit> units)
	{
		cooldown -= 1;
		if (cooldown <= 0)
		{
			List<Vector2I> targets = GetTargets(units);
			cooldown = startingCooldown;
		}
  }

	private List<Vector2I> GetTargets(List<Unit> units)
	{
		foreach (var unit in units)
		{
			if (unit.side != side)
			{
			unit.TakeDamage(damage);

			int cellX = Mathf.FloorToInt(unit.GlobalPosition.X / GlobalConstants.TileSize);
			int cellY = Mathf.FloorToInt(unit.GlobalPosition.Y / GlobalConstants.TileSize);

			return [new Vector2I(cellX, cellY)];
			}
		}

		return [];
  }

  public void TakeDamage(int amount)
  {
		int effectiveDamage = Mathf.Max(0, amount - armor);
		health -= effectiveDamage;
		_healthBar.Value = health;
		if (health <= 0)
		{
			DeathRattle();
      _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitDied, this);
    }
  }

	public void DeathRattle()
	{
		
	}
}
