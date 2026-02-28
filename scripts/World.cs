using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
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
  private TileMapLayer _overlayLayer;
  private MenuButton _speedButton;
  private PopupMenu _speedPopup;

  private List<Unit> _units;
  private Unit[,] _unitsGrid;
  private int _unitIndex = 0;
  private int _playerUnitsCount = 0;
  private int _enemyUnitsCount = 0;
  private Godot.Collections.Dictionary<int, UnitGui> _unitsGui = null!;
  private string _unitsSelectionScenePath = "res://scenes/units/unit_selection.tscn";
  List<Vector2I> _selectedTargets;

  private bool _playing = false;
  private int _level = 1;
  private int _turn = 0;
  private double _turnStartCooldown = 1.0f;
  private double _turnCooldown = 1.0f;
  private bool _acting = false;
  private double _actingCooldown = 0.5f;
  private double _actingStartCooldown = 0.5f;
  private bool _targeting = false;
  private double _speed = 1.0f;
  private int _turnEndDamage = 1;

  private Godot.Collections.Dictionary<string, UnitInfo> _unitsData;
  private List<List<UnitInfo>> _unitsPerStage;
  private List<UnitInfo> _levelUnits; // List of units in the current level, used for rewards
  private Dictionary _levelsData;
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
    _overlayLayer = GetNode<TileMapLayer>("OverlayLayer");
    _speedButton = GetNode<MenuButton>("CanvasLayer/BottomUi/SpeedButton");

    _playButton.Pressed += OnPlayButtonPressed;
    _speedPopup = _speedButton.GetPopup();
    _speedPopup.IdPressed += OnSpeedSelected;
    _units = new List<Unit>();
    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _globalSignals.UnitDied += OnUnitDied;
    _globalSignals.UnitMoved += OnUnitMoved;

    _levelUnits = new List<UnitInfo>();
    _unitsPerStage = new List<List<UnitInfo>>(3);
    for (int i = 0; i < 4; i++)
    {
      _unitsPerStage.Add(new List<UnitInfo>());
    }
    ParseUnitsJson();

    string levelsJson = FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(levelsJson);
    _levelsData = (Dictionary)parsed;

    // Add initial turrent selection unit
    _unitsGui = new Godot.Collections.Dictionary<int, UnitGui>();
    UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
    unitGui.Info = _unitsData["turret"];
    unitGui.Amount = 2;
    _unitsSelectionContainer.AddChild(unitGui);
    _unitsGui[_unitsData["turret"].Id] = unitGui;
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (_acting)
    {
      _actingCooldown -= delta;
      if (_actingCooldown <= 0)
      {
        _units[_unitIndex].Act(_selectedTargets, _unitsGrid);
        _unitIndex += 1;
        _turnCooldown = _turnStartCooldown;
        _acting = false;
        _actingCooldown = _actingStartCooldown;
      }
    }
    else if (_targeting)
    {
      _actingCooldown -= delta;
      if (_actingCooldown <= 0)
      {
        _selectedTargets = _units[_unitIndex].GetTargets(_unitsGrid);
        foreach (Vector2I target in _selectedTargets)
        {
          _overlayLayer.SetCell(target, 0, new Vector2I(3, 4)); // Show overlay on target cells
        }
        _acting = true;
        if (_selectedTargets.Count > 0)
          _actingCooldown = _actingStartCooldown;
        else
          _actingCooldown = 0; // If there are no targets, skip directly to acting phase
        _targeting = false;
        
      }
    }
    else if (_playing)
    {
      _turnCooldown -= delta;
      if (_turnCooldown <= 0)
      {
        if (_unitIndex >= _units.Count)
        {
          _unitIndex = 0;
          _turn += 1;
          _turnCounter.Text = _turn.ToString();
          if (_turn > 10)
          {
            // Apply end of turn damage to all units
            int index = 0;
            while (index < _units.Count)
            {
              Unit unit = _units[index];
              unit.ChangeHealth(-_turnEndDamage);
              // If the unit died from end of turn damage, it will be removed from the list, so we don't increment the index
              if (_units.Contains(unit))
                index += 1;
            }
            _turnEndDamage += 1;  // Increase end of turn damage for next turn
            _turnCooldown = _turnStartCooldown;
            _unitIndex = 0;  // Set to 0 again incase a unit died and change the _unitIndex value
            return;
          }
        }

        _overlayLayer.Clear();
        if (_units[_unitIndex].CanAct())
        {
          // Show overlay on selected cells
          foreach (var cell in _units[_unitIndex].GetOccupiedCells())
            _overlayLayer.SetCell(cell, 0, new Vector2I(2, 4));
          _targeting = true;
        }
        else
        {
          _unitIndex += 1;
        }
      }
    }
  }

  private void OnPlayButtonPressed()
  {
    _playButton.Disabled = true;
    _gridOverlay.SetInteractionLocked(true);
    UnitInfo[,] playerUnits = _gridOverlay.GetUnits();
    Unit[,] enemyUnits = LoadRandomLevel();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        UnitInfo playerUnit = playerUnits[x, y];
        Unit enemyUnit = enemyUnits[x, y];
        if (playerUnit != null)
        {
          if (!_units.Contains(playerUnit.UnitInstance!))
          {
            _units.Add(playerUnit.UnitInstance!);
            _playerUnitsCount += 1;
          }
          _unitsGrid[x, y] = playerUnit.UnitInstance!;
        }
        if (enemyUnit != null)
        {
          if (!_units.Contains(enemyUnit))
          {
            _units.Add(enemyUnit);
            _enemyUnitsCount += 1;
          }
          _unitsGrid[x, y] = enemyUnit;
        }
      }
    }

    // Sort units by speed, then by position for consistent turn order
    _units = _units
    .OrderByDescending(u => u.speed)  // Speed first
    .ThenBy(u => u.GlobalPosition.X)  // Leftmost first
    .ThenBy(u =>
        u.side
            ? u.occupiedMainCell.Y  // Player: lower Y first
            : GlobalConstants.GridSize.Y - 1 - u.occupiedMainCell.Y)  // Enemy: higher Y first
    .ToList();

    _playing = true;
  }

  private Unit[,] loadLevel()
  {
    Unit[,] unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    Dictionary levelData = (Dictionary)_levelsData["level_" + _level.ToString()];
    Godot.Collections.Array levelUnits = (Godot.Collections.Array)levelData["units"];
    foreach (Dictionary unitData in levelUnits)
    {
      int x = (int)unitData["x"];
      int y = (int)unitData["y"];
      string name = (string)unitData["name"];
      UnitInfo unitInfo = _unitsData[name];

      // Place the unit in the new cell
      PackedScene scene = GD.Load<PackedScene>(unitInfo.ScenePath);
      Node instance = scene.Instantiate();
      Unit unit = instance as Unit;
      unit.startCell = new Vector2I(x, y);
      unit.occupiedCells = unitInfo.OccupiedCells;
      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[x + cell.X, y + cell.Y] = unit;
      }
      unit.side = false;
      _unitsNode.AddChild(instance);
      _levelUnits.Add(unitInfo);
    }

    return unitGrid;
  }

  public Unit[,] LoadRandomLevel()
  {
    Unit[,] unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    int budget = _level + (_level / 2);
    int maxStage = GetMaxStageForLevel();

    RandomNumberGenerator rng = new();
    rng.Randomize();

    while (budget > 0)
    {
      int stage = rng.RandiRange(0, maxStage);

      List<UnitInfo> possibleUnits = _unitsPerStage[stage];
      if (possibleUnits.Count == 0)
        continue;

      UnitInfo unitInfo = possibleUnits[rng.RandiRange(0, possibleUnits.Count - 1)];

      if (unitInfo.Cost > budget)
        continue;

      // Get random position for the unit, ensuring it fits within the grid and doesn't overlap with existing units
      List<Vector2I> possiblePositions = new();
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        for (int y = 0; y < Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5); y++)
        {
          bool canPlace = true;
          foreach (Vector2I cell in unitInfo.OccupiedCells)
          {
            int checkX = x + cell.X;
            int checkY = y + cell.Y;
            if (checkX >= GlobalConstants.GridSize.X || checkY >= Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5) || unitGrid[checkX, checkY] != null)
            {
              canPlace = false;
              break;
            }
          }
          if (canPlace)
          {
            possiblePositions.Add(new Vector2I(x, y));
          }
        }
      }

      Vector2I cellPos = possiblePositions[rng.RandiRange(0, possiblePositions.Count - 1)];
      if (unitInfo.Name == "Tank" || unitInfo.Name == "Laser" || unitInfo.Name == "Saboteur")
      {
        // Position tanks, lasers and saboteurs in the front portion of the grid
        List<Vector2I> frontPositions = new List<Vector2I>();
        int maxY = 0;
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.Y > maxY)
          {
            frontPositions.Clear();
            maxY = cell.Y;
            frontPositions.Add(cell);
          }
          else if (cell.Y == maxY)
          {
            frontPositions.Add(cell);
          }
        }
        cellPos = frontPositions[rng.RandiRange(0, frontPositions.Count - 1)];
      }
      else if (unitInfo.Name == "Pusher")
      {
        // Position pushers in the right portion of the grid
        List<Vector2I> rightPositions = new List<Vector2I>();
        int maxX = 0;
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.X > maxX)
          {
            rightPositions.Clear();
            maxX = cell.X;
            rightPositions.Add(cell);
          }
          else if (cell.X == maxX)
          {
            rightPositions.Add(cell);
          }
        }
        cellPos = rightPositions[rng.RandiRange(0, rightPositions.Count - 1)];
      }
      else if (unitInfo.Name == "Booster")
      {
        // Position boosters next to another unit if possible
        Vector2I adjacentCell = new Vector2I(cellPos.X, cellPos.Y);
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.X + 1 < GlobalConstants.GridSize.X && unitGrid[cell.X + 1, cell.Y] != null && unitGrid[cell.X + 1, cell.Y].damage > 0)
          {
            adjacentCell = cell;
            break;
          }
        }
        cellPos = adjacentCell;
      }

      Unit unit = GD.Load<PackedScene>(unitInfo.ScenePath).Instantiate() as Unit;
      unit.startCell = cellPos;
      unit.occupiedCells = unitInfo.OccupiedCells;
      unit.side = false;
      _unitsNode.AddChild(unit);
      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[cellPos.X + cell.X, cellPos.Y + cell.Y] = unit;
      }
      _levelUnits.Add(unitInfo);

      budget -= unitInfo.Cost;
    }

    return unitGrid;
  }

  private void Reset()
  {
    foreach (Unit unit in _units)
      unit.QueueFree();
    _units.Clear();
    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _unitIndex = 0;
    _enemyUnitsCount = 0;
    _playerUnitsCount = 0;
    _turn = 0;
    _turnCounter.Text = _turn.ToString();
    _turnCooldown = _turnStartCooldown;
    _message.Text = "";
    _messagePanel.Hide();
    _gridOverlay.LoadUnits();
    _levelCounter.Text = _level.ToString();
    _overlayLayer.Clear();
    _turnEndDamage = 1;
    _levelUnits.Clear();

    // Clear message responses
    foreach (Node child in _messageResponses.GetChildren())
    {
      child.QueueFree();
    }

    _gridOverlay.SetInteractionLocked(false);
    _playButton.Disabled = false;
  }

  private void Win()
  {
    if (!_playing)
      return;
    _playing = false;
    _message.Text = "You Win!\nChoose your reward:";
    _level += 1;
    ShowRewards();
    _messagePanel.Show();
  }

  private void Lose()
  {
    if (!_playing)
      return;
    _playing = false;
    _message.Text = "You Lose...";

    Button btn = new Button();
    btn.Text = "Return";
    btn.SizeFlagsVertical = SizeFlags.ExpandFill;
    btn.Pressed += () => OnReturnButtonPressed();
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
      UnitInfo unitInfo = _levelUnits[i];

      // Create button with unit texture
      Button btn = new Button();
      btn.Text = unitInfo.Name;
      btn.Icon = unitInfo.Texture;
      btn.IconAlignment = HorizontalAlignment.Center;
      btn.VerticalIconAlignment = VerticalAlignment.Bottom;
      btn.ExpandIcon = true;
      //btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      btn.SizeFlagsVertical = SizeFlags.ExpandFill;
      btn.Pressed += () => OnRewardButtonPressed(unitInfo);

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
    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = null!;
    }

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

  private void OnUnitMoved(Unit unit, Vector2I oldCell)
  {
    if (!_playing)
      return;

    foreach (Vector2I cell in unit.occupiedCells)
    {
      _unitsGrid[oldCell.X + cell.X, oldCell.Y + cell.Y] = null!;
    }
    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = unit;
    }

    // Sort units by speed, then by position for consistent turn order
    _units = _units
    .OrderByDescending(u => u.speed)  // Speed first
    .ThenBy(u => u.GlobalPosition.X)  // Leftmost first
    .ThenBy(u =>
        u.side
            ? u.occupiedMainCell.Y  // Player: lower Y first
            : GlobalConstants.GridSize.Y - 1 - u.occupiedMainCell.Y)  // Enemy: higher Y first
    .ToList();
  }

  private void OnRewardButtonPressed(UnitInfo unitInfo)
  {
    if (_unitsGui.ContainsKey(unitInfo.Id))
    {
      _unitsGui[unitInfo.Id].UpdateAmount(_unitsGui[unitInfo.Id].Amount + 1);
    }
    else
    {
      UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;

      unitGui.Info = unitInfo;
      unitGui.Amount = 1;
      _unitsSelectionContainer.AddChild(unitGui);
      _unitsGui[unitInfo.Id] = unitGui;
    }

    Reset();
  }

  private void OnReturnButtonPressed()
  {
    Reset();
  }

  private void OnSpeedSelected(long id)
  {
    switch (id)
    {
      case 0:
        _speed = 0.5f;
        break;
      case 1:
        _speed = 1.0f;
        break;
      case 2:
        _speed = 2.0f;
        break;
      case 3:
        _speed = 4.0f;
        break;
      case 4:
        _speed = 8.0f;
        break;
      case 5:
        _speed = 16.0f;
        break;
    }
    _speedButton.Text = "Speed: " + _speedPopup.GetItemText((int)id);
    _turnStartCooldown = 1.0f / _speed;
    _actingStartCooldown = 0.5f / _speed;
  }

  private void ParseUnitsJson()
  {
    _unitsData = new Godot.Collections.Dictionary<string, UnitInfo>();
    string unitsJson = FileAccess.Open("res://scripts/units/units.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(unitsJson);
    Dictionary unitsData = (Dictionary)parsed;
    foreach (KeyValuePair<Variant, Variant> entry in unitsData)
    {
      string unitName = entry.Key.ToString();
      Dictionary unitData = (Dictionary)entry.Value;

      int id = (int)unitData["id"];
      string displayName = (string)unitData["display_name"];
      string texturePath = (string)unitData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)unitData["scene"];
      int cost = (int)unitData["cost"];

      Godot.Collections.Array cells = (Godot.Collections.Array)unitData["cells"];
      List<Vector2I> occupiedCells = new();

      foreach (Godot.Collections.Array cell in cells)
      {
        int x = (int)cell[0];
        int y = (int)cell[1];
        occupiedCells.Add(new Vector2I(x, y));
      }

      _unitsData[unitName] = new UnitInfo(id, displayName, texture, scenePath, occupiedCells, cost, null);

      int stage = (int)unitData["stage"];
      _unitsPerStage[stage].Add(_unitsData[unitName]);
    }
  }

  int GetMaxStageForLevel()
  {
    if (_level < 3) return 0;
    if (_level < 6) return 1;
    if (_level < 10) return 2;
    return 3;
  }
}
