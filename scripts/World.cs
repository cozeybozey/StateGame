using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class World : Node2D
{
	private TextureButton _playButton;
	private Label _levelCounter;
	private Label _turnCounter;
	private Panel _messagePanel;
	private GridOverlay _gridOverlay;
  private GlobalSignals _globalSignals;
  private Node _unitsNode;

  private List<Unit> _units;
  private int _unitIndex = 0;
	private int _playerUnitsCount = 0;
	private int _enemyUnitsCount = 0;
	
  private bool _playing = false;
	private int _level = 1;
  private int _turn = 0;
  private double _turnCooldown = 0.25f;
  private double _turnStartCooldown = 0.25f;
  bool _gameEnd = false;
  private double _gameEndCooldown = 4.0f;
  private double _gameEndStartCooldown = 4.0f;

	private Dictionary _unitsData;
	private Dictionary _levelsData;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
		_playButton = GetNode<TextureButton>("CanvasLayer/BottomUi/PlayButton");
		_levelCounter = GetNode<Label>("CanvasLayer/BottomUi/LevelCounter/Counter");
		_turnCounter = GetNode<Label>("CanvasLayer/BottomUi/TurnCounter/Counter");
		_messagePanel = GetNode<Panel>("CanvasLayer/MessagePanel");
		_gridOverlay = GetNode<GridOverlay>("GridOverlay");
		_globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _unitsNode = GetNode("Units");

		_playButton.Pressed += OnPlayButtonPressed;
		_units = new List<Unit>();
    _globalSignals.UnitDied += OnUnitDied;

    string unitsJson = FileAccess.Open("res://scripts/units/units.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(unitsJson);
		_unitsData = (Dictionary)parsed;

    string levelsJson = FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText();
    parsed = Json.ParseString(levelsJson);
    _levelsData = (Dictionary)parsed;
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_playing)
		{
			if (_unitIndex >= _units.Count)
			{
				_unitIndex = 0;
				_turn += 1;
				_turnCounter.Text = _turn.ToString();
			}

			_turnCooldown -= delta;
			if (_turnCooldown <= 0)
			{
				_units[_unitIndex].Act(_units);
				_unitIndex += 1;
				_turnCooldown = _turnStartCooldown;
			}
		}
		else if (_gameEnd)
		{
			if (!_messagePanel.Visible)
				_messagePanel.Show();
			_gameEndCooldown -= delta;
			if (_gameEndCooldown <= 0)
				reset();
    }
	}

	private void OnPlayButtonPressed()
	{
    _playButton.Disabled = true;
    UnitInfo[,] playerUnits = _gridOverlay.GetUnits();
		Unit[,] enemyUnits = loadLevel();

		for (int x = 0; x < GlobalConstants.GridSize.X; x++)
		{
			for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
			{
				UnitInfo playerUnit = playerUnits[x, y];
				Unit enemyUnit = enemyUnits[x, y];
				if (playerUnit != null)
				{
					_units.Add(playerUnit.UnitInstance!);
					_playerUnitsCount += 1;
        }
				if (enemyUnit != null)
				{
					_units.Add(enemyUnit);
					_enemyUnitsCount += 1;
				}
			}
		}

		_playing = true;
  }

	private Unit[,] loadLevel()
	{
		Unit[,] unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
		Dictionary levelData = (Dictionary)_levelsData["level_" + _level.ToString()];
    Godot.Collections.Array enemyUnitsData = (Godot.Collections.Array)levelData["units"];
    foreach (Dictionary unitData in enemyUnitsData)
    {
      int x = (int)unitData["x"];
      int y = (int)unitData["y"];
      string name = (string)unitData["name"];

      Dictionary unitTemplate = (Dictionary)_unitsData[name];

      // Parse template data
      string scenePath = (string)unitTemplate["scene"];

      // Place the unit in the new cell
      PackedScene scene = GD.Load<PackedScene>(scenePath);
      Node instance = scene.Instantiate();
      Unit unit = instance as Unit;
      unit.GlobalPosition = _gridOverlay.RelCellToGlobalPosition(new Vector2I(x, y), false);
      unit.side = false;
      _unitsNode.AddChild(instance);
      unitGrid[x, y] = unit;
    }

		return unitGrid;
  }

	private void reset()
	{
		foreach (Unit unit in _units)
			unit.QueueFree();
    _units.Clear();
		_enemyUnitsCount = 0;
		_playerUnitsCount = 0;
		_turn = 0;
    _turnCounter.Text = _turn.ToString();
    _gameEndCooldown = _gameEndStartCooldown;
		_turnCooldown = _turnStartCooldown;
    _messagePanel.GetNode<RichTextLabel>("Message").Text = "";
    _messagePanel.Hide();
		_gameEnd = false;
		_gridOverlay.LoadUnits();
		_playButton.Disabled = false;
    _levelCounter.Text = _level.ToString();
  }

	private void win()
	{
		_playing = false;
		_gameEnd = true;
		_messagePanel.GetNode<RichTextLabel>("Message").Text = "You Win!";
    _level += 1;
  }

  private void lose()
  {
    _playing = false;
    _gameEnd = true;
    _messagePanel.GetNode<RichTextLabel>("Message").Text = "You Lose...";
  }

  private void OnUnitDied(Unit unit)
  {
		// Remove from list
		int index = _units.IndexOf(unit);
		if (index <= _unitIndex)
			_unitIndex -= 1;
		_units.Remove(unit);

		if (unit.side)
			_playerUnitsCount--;
		else
			_enemyUnitsCount--;
    unit.QueueFree();

		if (_enemyUnitsCount == 0)
			win();
		else if (_playerUnitsCount == 0)
			lose();
  }
}
