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

  // World generation
  private Panel _worldUi;
  private Button _worldMapButton;
  private Godot.Collections.Dictionary<string, LevelInfo> _world = new Godot.Collections.Dictionary<string, LevelInfo>();
  private int _numLayers = 30;
  private int _maxNodesPerLayer = 3;
  private int _buttonWidth = 75;
  private int _buttonHeight = 35;
  private LevelInfo _activeLevel = null!;
  private int _worldUiSpacing = 100;

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
    _worldUi = GetNode<Panel>("CanvasLayer/ScrollContainer/WorldUi");
    _worldMapButton = GetNode<Button>("CanvasLayer/BottomUi/WorldMapButton");

    _playButton.Pressed += OnPlayButtonPressed;
    _speedPopup = _speedButton.GetPopup();
    _speedPopup.IdPressed += OnSpeedSelected;
    _units = new List<Unit>();
    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _globalSignals.UnitDied += OnUnitDied;
    _globalSignals.UnitMoved += OnUnitMoved;
    _worldMapButton.Pressed += OnWorldMapPressed;

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

    GenerateWorld();
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

  private void StartLevel(UnitInfo[,] enemyUnits)
  {
    _playButton.Disabled = true;
    _worldMapButton.Disabled = true;
    _gridOverlay.SetInteractionLocked(true);
    UnitInfo[,] playerUnits = _gridOverlay.GetUnits();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        UnitInfo playerUnit = playerUnits[x, y];
        UnitInfo enemyUnit = enemyUnits[x, y];
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
          if (enemyUnit.UnitInstance == null)
          {
            Unit unitInstance = GD.Load<PackedScene>(enemyUnit.ScenePath).Instantiate() as Unit;
            unitInstance.startCell = new Vector2I(x, y);
            unitInstance.occupiedCells = enemyUnit.OccupiedCells;
            unitInstance.side = false;
            enemyUnit.UnitInstance = unitInstance;
            _unitsNode.AddChild(unitInstance);
            _levelUnits.Add(enemyUnit);
          }

          if (!_units.Contains(enemyUnit.UnitInstance!))
          {
            _units.Add(enemyUnit.UnitInstance!);
            _enemyUnitsCount += 1;
          }
          _unitsGrid[x, y] = enemyUnit.UnitInstance!;
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

  private void OnPlayButtonPressed()
  {
    UnitInfo[,] enemyUnits = LoadRandomLevel(_level);
    StartLevel(enemyUnits);
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

  public UnitInfo[,] LoadRandomLevel(int difficulty)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    int budget = difficulty + (difficulty / 2);
    int maxStage = GetMaxStageForDifficulty(difficulty);

    RandomNumberGenerator rng = new();
    rng.Randomize();

    while (budget > 0)
    {
      int stage = rng.RandiRange(0, maxStage);

      List<UnitInfo> possibleUnits = _unitsPerStage[stage];
      if (possibleUnits.Count == 0)
        continue;


      UnitInfo selectedUnitInfo = possibleUnits[rng.RandiRange(0, possibleUnits.Count - 1)];
      // Create a copy of the UnitInfo to avoid modifying the original data when assigning UnitInstance
      UnitInfo unitInfo = new UnitInfo(
        selectedUnitInfo.Id,
        selectedUnitInfo.Name,
        selectedUnitInfo.Texture,
        selectedUnitInfo.ScenePath,
        selectedUnitInfo.OccupiedCells,
        selectedUnitInfo.Cost,
        null
      );

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
      else if (unitInfo.Name == "Sniper")
      {
        // Position snipers in back portion of the grid
        List<Vector2I> backPositions = new List<Vector2I>();
        int minY = GlobalConstants.GridSize.Y - 1;
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.Y < minY)
          {
            backPositions.Clear();
            minY = cell.Y;
            backPositions.Add(cell);
          }
          else if (cell.Y == minY)
          {
            backPositions.Add(cell);
          }
        }
        cellPos = backPositions[rng.RandiRange(0, backPositions.Count - 1)];
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
          if (cell.X + 1 < GlobalConstants.GridSize.X && unitGrid[cell.X + 1, cell.Y] != null) // TODO AKN reintroduce && unitGrid[cell.X + 1, cell.Y].damage > 0)
          {
            adjacentCell = cell;
            break;
          }
        }
        cellPos = adjacentCell;
      }

      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[cellPos.X + cell.X, cellPos.Y + cell.Y] = unitInfo;
      }

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

    if (_activeLevel != null)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        {
          if (_activeLevel.Units![x, y] != null)
            _activeLevel.Units![x, y].UnitInstance = null;
        }
      }
    }

    // Clear message responses
    foreach (Node child in _messageResponses.GetChildren())
    {
      child.QueueFree();
    }

    _gridOverlay.SetInteractionLocked(false);
    _playButton.Disabled = false;
    _worldMapButton.Disabled = false;
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

    if (_activeLevel != null)
    {
      _activeLevel.Completed = true;
      _activeLevel.LevelButton.Disabled = true;
      foreach (string nextNodeId in _activeLevel.NextNodes)
      {
        if (_world.ContainsKey(nextNodeId) && !_world[nextNodeId].Unlocked)
        {
          _world[nextNodeId].Unlocked = true;
          _world[nextNodeId].LevelButton.Disabled = false;
        }
      }
    }
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

  int GetMaxStageForDifficulty(int difficulty)
  {
    if (difficulty < 3) return 0;
    if (difficulty < 6) return 1;
    if (difficulty < 10) return 2;
    return 3;
  }

  void GenerateWorld()
  {
    _worldUi.CustomMinimumSize = new Vector2(
      _maxNodesPerLayer * _worldUiSpacing + 300,
      _numLayers * 0.5f * _worldUiSpacing + 200
    );
    _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_vertical", Mathf.FloorToInt(_numLayers * 0.5f * _worldUiSpacing + 200));

    List<List<LevelInfo>> layers = new();

    int currentNodesInLayer = 1; // Start with 1 node in the first layer
    for (int layer = 0; layer < _numLayers; layer++)
    {
      List<LevelInfo> layerNodes = new();
      for (int node = 0; node < currentNodesInLayer; node++)
      {
        string nodeId = $"L{layer}_N{node}";

        _world[nodeId] = new LevelInfo(
          id: nodeId,
          name: nodeId,
          completed: false,
          unlocked: layer == 0, // Only unlock first layer initially
          layer: layer,
          layerIndex: node,
          isBoss: false,
          nextNodes: new List<string>(),
          levelButton: new Button(),
          units: LoadRandomLevel(layer + 1)
        );
        layerNodes.Add(_world[nodeId]);

      }
      layers.Add(layerNodes);
      if (layer > 0)
      {
        ConnectLayers(layers[layer - 1], layers[layer]);
        foreach (LevelInfo levelNode in layers[layer - 1])
        {
          AddLevelUi(levelNode, layers[layer - 1].Count, layers[layer]);
        }
      }
      currentNodesInLayer = _rng.Next(1, _maxNodesPerLayer + 1);
    }
  }

  void ConnectLayers(List<LevelInfo> prev, List<LevelInfo> next)
  {
    int prevCount = prev.Count;
    int nextCount = next.Count;

    // Guarantee every next node has at least one incoming
    for (int j = 0; j < nextCount; j++)
    {
      int closestPrev = Mathf.RoundToInt(
          (float)j / (nextCount - 1) * (prevCount - 1)
      );

      prev[closestPrev].NextNodes.Add(next[j].Id);
    }

    // Add some extra forward connections (optional)
    for (int i = 0; i < prevCount; i++)
    {
      if (_rng.NextDouble() < 0.5)
      {
        int target = Mathf.Clamp(i + _rng.Next(-1, 2), 0, nextCount - 1);

        // Block crossing connections
        if (i > 0)
        {
          bool blockTarget = false;
          foreach (string levelNode in prev[i - 1].NextNodes)
          {
            if (next.IndexOf(_world[levelNode]) > target)
            {
              blockTarget = true; 
              break;
            }
          }
          if (blockTarget)
            continue;
        }

        if (!prev[i].NextNodes.Contains(next[target].Id))
        {
          prev[i].NextNodes.Add(next[target].Id);
        }
      }
    }
  }

  void AddLevelUi(LevelInfo levelNode, int nodesInLayer, List<LevelInfo> nextLevelNodes)
  {
    Vector2 buttonSize = new Vector2(_buttonWidth, _buttonHeight);
    Vector2 buttonPos = GetLevelButtonPosition(levelNode.Layer, levelNode.LayerIndex, nodesInLayer);

    levelNode.LevelButton.Text = levelNode.Name;
    levelNode.LevelButton.Disabled = !levelNode.Unlocked;
    levelNode.LevelButton.CustomMinimumSize = buttonSize;
    levelNode.LevelButton.Position = buttonPos;
    levelNode.LevelButton.Pressed += () => OnLevelSelected(levelNode);
    _worldUi.AddChild(levelNode.LevelButton);

    //Vector2 fromCenter = btn.Position + btn.Size / 2.0f;
    // Convert to global coordinates relative to the CanvasLayer so Line2D (a Node2D) can be added there
    //Vector2 fromGlobal = _worldUi.RectGlobalPosition + fromCenter;
    Vector2 fromGlobal = buttonPos + buttonSize / 2.0f;

    foreach (string id in levelNode.NextNodes)
    {

      Vector2 nextButtonPos = GetLevelButtonPosition(levelNode.Layer + 1, nextLevelNodes.IndexOf(_world[id]), nextLevelNodes.Count);

      //Vector2 toCenter = toBtn.Position + toBtn.RectSize / 2.0f;
      Vector2 toGlobal = nextButtonPos + buttonSize / 2.0f;

      Line2D line = new Line2D();

      line.Points = new Vector2[] { fromGlobal, toGlobal };
      line.Width = 2.0f;
      line.DefaultColor = new Color(1, 1, 1, 0.8f);
      _worldUi.AddChild(line);
    }
  }

  Vector2 GetLevelButtonPosition(int layer, int layerIndex, int nodesInLayer)
  {
    // Calculate X so the nodes in the layer are centered around the panel's center.
    // For n nodes we place them with a fixed spacing and center the group:
    // startX = center - ((n-1) * spacing) / 2
    double centerX = _worldUi.CustomMinimumSize.X / 2.0;
    double totalWidth = _worldUiSpacing * (nodesInLayer - 1);
    double startX = centerX - (totalWidth / 2.0);
    double xPos = startX + layerIndex * _worldUiSpacing;
    return new Vector2((float)xPos, (float)(_worldUi.CustomMinimumSize.Y - 100 - layer * _worldUiSpacing * 0.5f));
  }

  void OnLevelSelected(LevelInfo levelNode)
  {
    _worldUi.GetParent<ScrollContainer>().Hide();
    _activeLevel = levelNode;
    StartLevel(levelNode.Units!);
  }

  void OnWorldMapPressed()
  {
    _worldUi.GetParent<ScrollContainer>().Visible = !_worldUi.GetParent<ScrollContainer>().Visible;
  }
}
