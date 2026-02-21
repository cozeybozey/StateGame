using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;

public partial class World : Node2D
{
	private Button _playButton;
	private GridOverlay _gridOverlay;
  Node unitsNode;

  List<Unit> _units;
  int _unitIndex = 0;
  private bool _playing = false;
  private float cooldown = 250.0f;
  private float start_cooldown = 250.0f;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
	_playButton = GetNode<Button>("CanvasLayer/ControlButtons/PlayButton");
	_gridOverlay = GetNode<GridOverlay>("GridOverlay");
	unitsNode = GetNode("Units");
	_playButton.Pressed += OnPlayButtonPressed;
	_units = new List<Unit>();
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	if (_playing)
	{
	  cooldown -= (float)delta;
	  if (cooldown <= 0)
	  {
		_units[_unitIndex].Act(_units);
		_unitIndex += 1;
		cooldown = start_cooldown;
	  }
	}
	}

	private void OnPlayButtonPressed()
	{
		Unit[,] playerUnits = _gridOverlay.GetUnits();
		Unit[,] enemyUnits = loadLevel();

	for (int x = 0; x < GlobalConstants.GridSize.X; x++)
	{
	  for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
	  {
		Unit playerUnit = playerUnits[x, y];
		Unit enemyUnit = enemyUnits[x, y];
		if (playerUnit != null)
		{
		  _units.Add(playerUnit);
		}
		if (enemyUnit != null)
		{
		  _units.Add(enemyUnit);
		}
	  }
	}

	_playing = true;
  }

	private Unit[,] loadLevel()
	{
	Unit[,] unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

	// Place the unit in the new cell
	Vector2I relCel = new Vector2I(4, 4);
	UnitInfo unitInfo = new UnitInfo(1, "Turret", GD.Load<Texture2D>("res://sprites/units/blue_unit.png"), new Vector2I(1, 3), "res://scenes/units/turret.tscn");
	PackedScene scene = GD.Load<PackedScene>(unitInfo.ScenePath);
	Node instance = scene.Instantiate();
	Unit unit = instance as Unit;
	unit.GlobalPosition = _gridOverlay.RelCellToGlobalPosition(relCel);
	unitsNode.AddChild(instance);
	unitGrid[relCel.X, relCel.Y] = unit;

	return unitGrid;
  }
}
