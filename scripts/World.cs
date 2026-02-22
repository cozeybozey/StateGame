using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices.Marshalling;
using static Godot.Control;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class World : Node2D
{
	private TextureButton _playButton;
	private Label _levelCounter;
	private Label _turnCounter;
	private Panel _messagePanel;
	private RichTextLabel _message;
	private HBoxContainer _messageResponses;
	private GridOverlay _gridOverlay;
  private GlobalSignals _globalSignals;
  private Node _unitsNode;
  private VBoxContainer _unitsSelectionContainer;

  private List<Unit> _units;
  private int _unitIndex = 0;
	private int _playerUnitsCount = 0;
	private int _enemyUnitsCount = 0;
  private Godot.Collections.Dictionary<int, UnitGui> _unitsGui = null!;
  private string _unitsSelectionScenePath = "res://scenes/units/unit_selection.tscn";

  private bool _playing = false;
	private int _level = 1;
  private int _turn = 0;
  private double _turnCooldown = 0.1f;
  private double _turnStartCooldown = 0.1f;
  bool _gameEnd = false;
  private double _gameEndCooldown = 4.0f;
  private double _gameEndStartCooldown = 4.0f;

	private Dictionary _unitsData;
	private Dictionary _levelsData;
	private Godot.Collections.Array _levelUnits; // List of units in the current level, used for rewards
  private Random _rng = new Random();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
	{
		_playButton = GetNode<TextureButton>("CanvasLayer/BottomUi/PlayButton");
		_levelCounter = GetNode<Label>("CanvasLayer/BottomUi/LevelCounter/Counter");
		_turnCounter = GetNode<Label>("CanvasLayer/BottomUi/TurnCounter/Counter");
		_messagePanel = GetNode<Panel>("CanvasLayer/MessagePanel");
		_message = _messagePanel.GetNode<RichTextLabel>("MessageContainer/Message");
		_messageResponses = _messagePanel.GetNode<HBoxContainer>("MessageContainer/Responses");
    _gridOverlay = GetNode<GridOverlay>("GridOverlay");
		_globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _unitsNode = GetNode("Units");
    _unitsSelectionContainer = GetNode<VBoxContainer>("CanvasLayer/SelectionUi/HBoxContainer/UnitsSelectionContainer");

    _playButton.Pressed += OnPlayButtonPressed;
		_units = new List<Unit>();
    _globalSignals.UnitDied += OnUnitDied;

    string unitsJson = FileAccess.Open("res://scripts/units/units.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(unitsJson);
		_unitsData = (Dictionary)parsed;

    string levelsJson = FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText();
    parsed = Json.ParseString(levelsJson);
    _levelsData = (Dictionary)parsed;

    // Add initial turrent selection unit
    _unitsGui = new Godot.Collections.Dictionary<int, UnitGui>();
    UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
    Dictionary turretData = (Dictionary)_unitsData["turret"];
    int id = (int)turretData["id"];
    string displayName = (string)turretData["display_name"];
    string texturePath = (string)turretData["texture"];
    Texture2D texture = GD.Load<Texture2D>(texturePath);
    string scenePath = (string)turretData["scene"];
    unitGui.Info = new UnitInfo(id, displayName, texture, scenePath, null);
    unitGui.Amount = 2;
    _unitsSelectionContainer.AddChild(unitGui);
    _unitsGui[id] = unitGui;
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
				Reset();
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
    _levelUnits = (Godot.Collections.Array)levelData["units"];
    foreach (Dictionary unitData in _levelUnits)
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

	private void Reset()
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
    _message.Text = "";
    _messagePanel.Hide();
		_gameEnd = false;
		_gridOverlay.LoadUnits();
		_playButton.Disabled = false;
    _levelCounter.Text = _level.ToString();

    // Clear message responses
    foreach (Node child in _messageResponses.GetChildren())
    {
      child.QueueFree();
    }
  }

	private void Win()
	{
		_playing = false;
		_message.Text = "You Win!\nChoose your reward:";
    _level += 1;
    ShowRewards();
    _messagePanel.Show();
  }

  private void Lose()
  {
    _playing = false;
    _message.Text = "You Lose...";
    
    Button btn = new Button();
    btn.Text = "Return";
    //btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    btn.SizeFlagsVertical = SizeFlags.ExpandFill;
    btn.Pressed += () => OnReturnButtonPressed();
    // Add button to message responses
    _messageResponses.AddChild(btn);

    _messagePanel.Show();
  }

	private void ShowRewards()
	{
    // Pick up to 3 random units from the level
    int maxButtons = Mathf.Min(3, _levelUnits.Count);
    var chosenIndices = new HashSet<int>();

    while (chosenIndices.Count < maxButtons)
    {
      int index = _rng.Next(0, _levelUnits.Count);
      chosenIndices.Add(index);
    }

    foreach (int i in chosenIndices)
    {
      // Get unit data
      Dictionary unitData = (Dictionary)_levelUnits[i];
      string name = (string)unitData["name"];
      Dictionary unitTemplate = (Dictionary)_unitsData[name];
      string texturePath = (string)unitTemplate["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
			string displayName = (string)unitTemplate["display_name"];

      // Create button with unit texture
      Button btn = new Button();
			btn.Text = displayName;
      btn.Icon = texture;
      btn.IconAlignment = HorizontalAlignment.Center;
      btn.VerticalIconAlignment = VerticalAlignment.Bottom;
      btn.ExpandIcon = true;
      //btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      btn.SizeFlagsVertical = SizeFlags.ExpandFill;
      btn.Pressed += () => OnRewardButtonPressed(unitTemplate);

      // Add button to message responses
      _messageResponses.AddChild(btn);
    }
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
			Win();
		else if (_playerUnitsCount == 0)
			Lose();
  }

	private void OnRewardButtonPressed(Dictionary unitTemplate)
	{
    int id = (int)unitTemplate["id"];
    if (_unitsGui.ContainsKey(id))
    {
      _unitsGui[id].UpdateAmount(_unitsGui[id].Amount + 1);
    }
    else
    {
      UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
      string displayName = (string)unitTemplate["display_name"];
      string texturePath = (string)unitTemplate["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)unitTemplate["scene"];
      unitGui.Info = new UnitInfo(id, displayName, texture, scenePath, null);
      unitGui.Amount = 1;
      _unitsSelectionContainer.AddChild(unitGui);
      _unitsGui[id] = unitGui;
    }

    Reset();
  }

  private void OnReturnButtonPressed()
  {
    Reset();
  }
}
