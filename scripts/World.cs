using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Serialization;
using static Godot.Control;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class World : Node2D
{
  private CanvasLayer _canvasLayer;
  private TextureButton _playButton;
  private Label _levelCounter;
  private Label _turnCounter;
  private Label _coinsCounter;
  private Panel _messagePanel;
  private RichTextLabel _message;
  private HBoxContainer _messageResponses;
  private GridOverlay _gridOverlay;
  private GlobalSignals _globalSignals;
  private Node _unitsNode;
  private OverlayLayer _playerSideUnitsLayer;
  private OverlayLayer _enemySideUnitsLayer;
  private OverlayLayer _selectedUnitLayer;
  private OverlayLayer _activeUnitLayer;
  private OverlayLayer _targetedCellsLayer;
  private MenuButton _speedButton;
  private PopupMenu _speedPopup;
  private UnitsInfoGui _unitsGuiInfo;
  private Button _playPauseButton;
  private Button _surrenderButton;
  private DecksHandler _decksHandler;
  private LevelInfoContainer _levelInfoContainer;

  private List<Unit> _units;
  private List<Unit> _unitsToAct;
  private List<Unit> _removedUnits;
  private Unit[,] _unitsGrid;
  private int _unitIndex = 0;
  private int _playerUnitsCount = 0;
  private int _enemyUnitsCount = 0;
  List<Vector2I> _selectedTargets;

  private bool _playing = false;
  private int _level = 1;
  private int _completedLayers = 0;
  private int _slowDownLayer = 15;
  private int _turn = 0;
  private double _turnStartCooldown = 1.0f;
  private double _turnCooldown = 1.0f;
  private bool _acting = false;
  private double _actingCooldown = 0.5f;
  private double _actingStartCooldown = 0.5f;
  private bool _targeting = false;
  private double _speed = 1.0f;
  private int _turnEndDamage = 1;
  private Unit _activeUnit = null!;
  private bool _paused = false;

  public int Coins = 0;
  private int _coinsPerWin = 20;
  private int _levelRedoCost = 100;

  private List<List<UnitInfo>> _unitsPerStage;
  private List<UnitInfo> _levelUnits; // List of units in the current level, used for rewards
  private Dictionary _levelsData;
  private Random _rng = new Random();
  private UnitInfo? _selectedReward = null;

  // World generation
  public Godot.Collections.Dictionary<string, LevelInfo> Levels = new Godot.Collections.Dictionary<string, LevelInfo>();
  public int[] AmountOfNodesPerLayer;
  private Godot.Collections.Dictionary<string, Button> _levelButtons = new();
  private Panel _worldUi;
  private Button _worldMapButton;
  private int _numLayers = 30;
  private int _maxNodesPerLayer = 3;
  private int _buttonWidth = 75;
  private int _buttonHeight = 35;
  private LevelInfo _activeLevel = null!;
  private int _activeGauntletLevelIndex = 0;
  private int _worldUiSpacing = 100;
  private bool _openedWorldOnce = false;

  // Damage and healing tracking
  // Specifically use use C# dict so unit reference does not dissapear when it gets queue freed
  private System.Collections.Generic.Dictionary<Unit, UnitActivity> _unitsActivity = new System.Collections.Generic.Dictionary<Unit, UnitActivity>(); 
  private Panel _gameStats;
  private MenuButton _statisticsButton;
  private PopupMenu _statisticsPopup;
  private VBoxContainer _damageDealtStats;
  private VBoxContainer _damageTakenStats;
  private VBoxContainer _healingDoneStats;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
    _playButton = GetNode<TextureButton>("CanvasLayer/BottomUi/PlayButton");
    _levelCounter = GetNode<Label>("CanvasLayer/BottomUi/LevelCounter/Counter");
    _turnCounter = GetNode<Label>("CanvasLayer/BottomUi/TurnCounter/Counter");
    _coinsCounter = GetNode<Label>("CanvasLayer/BottomUi/CoinsCounter/Counter");
    _messagePanel = GetNode<Panel>("CanvasLayer/MessagePanel");
    _message = _messagePanel.GetNode<RichTextLabel>("MessageContainer/Message");
    _messageResponses = _messagePanel.GetNode<HBoxContainer>("MessageContainer/Responses");
    _gridOverlay = GetNode<GridOverlay>("GridOverlay");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _unitsNode = GetNode("Units");
    _playerSideUnitsLayer = GetNode<OverlayLayer>("PlayerSideUnitsLayer");
    _enemySideUnitsLayer = GetNode<OverlayLayer>("EnemySideUnitsLayer");
    _selectedUnitLayer = GetNode<OverlayLayer>("SelectedUnitLayer");
    _activeUnitLayer = GetNode<OverlayLayer>("ActiveUnitLayer");
    _targetedCellsLayer = GetNode<OverlayLayer>("TargetedCellsLayer");
    _speedButton = GetNode<MenuButton>("CanvasLayer/BottomUi/SpeedButton");
    _worldUi = GetNode<Panel>("CanvasLayer/ScrollContainer/WorldUi");
    _worldMapButton = GetNode<Button>("CanvasLayer/BottomUi/WorldMapButton");
    _unitsGuiInfo = GetNode<UnitsInfoGui>("CanvasLayer/SelectionUi/HBoxContainer/UnitsInfoContainer");
    _playPauseButton = GetNode<Button>("CanvasLayer/BottomUi/PlayPauseButton");
    _surrenderButton = GetNode<Button>("CanvasLayer/BottomUi/SurrenderButton");
    _decksHandler = GetNode<DecksHandler>("CanvasLayer/SelectionUi/HBoxContainer/DecksHandler");
    _levelInfoContainer = GetNode<LevelInfoContainer>("CanvasLayer/SelectionUi/HBoxContainer/LevelInfoContainer");

    // Activity UI
    _gameStats = GetNode<Panel>("CanvasLayer/GameStats");
    _statisticsButton = GetNode<MenuButton>("CanvasLayer/GameStats/VBoxContainer/StatisticsButton");
    _damageDealtStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/DamageDealt/VBoxContainer");
    _damageTakenStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/DamageTaken/VBoxContainer");
    _healingDoneStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/HealingDone/VBoxContainer");

    _canvasLayer.Offset = new Vector2I(GlobalConstants.TileSize, GlobalConstants.TileSize);
    _playButton.Pressed += OnPlayButtonPressed;
    _speedPopup = _speedButton.GetPopup();
    _speedPopup.IdPressed += OnSpeedSelected;
    _playPauseButton.Pressed += OnPlayPauseButtonPressed;
    _surrenderButton.Pressed += OnSurrenderButtonPressed;
    _units = new List<Unit>();
    _unitsToAct = new List<Unit>();
    _removedUnits = new List<Unit>();
    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _globalSignals.UnitDied += OnUnitDied;
    _globalSignals.UnitMoved += OnUnitMoved;
    _globalSignals.UnitSpawned += OnUnitSpawned;
    _globalSignals.UnitRemoved += OnUnitRemoved;
    _globalSignals.SpeedChanged += OnUnitSpeedChanged;
    _globalSignals.SizeChanged += OnUnitSizeChanged;
    _globalSignals.SideChanged += OnUnitSideChanged;
    _globalSignals.DamageDealt += OnUnitDamageDealt;
    _globalSignals.DamageTaken += OnUnitDamageTaken;
    _globalSignals.HealingDone += OnUnitHealingDone;
    _globalSignals.HealingReceived += OnUnitHealingReceived;
    _worldMapButton.Pressed += OnWorldMapPressed;
    _statisticsPopup = _statisticsButton.GetPopup();
    _statisticsPopup.IdPressed += OnStatisticSelected;
    _levelInfoContainer.StartLevelPressed += OnStartLevelPressed;
    _playerSideUnitsLayer.OutlineColor = Colors.Green;
    _playerSideUnitsLayer.OutlineCells = false;
    _playerSideUnitsLayer.HighlightCells = true;
    _playerSideUnitsLayer.HighlightColor = new Color(0, 1, 0, 0.15f);
    _playerSideUnitsLayer.LineWidth = 2f;
    _enemySideUnitsLayer.OutlineColor = Colors.Red;
    _enemySideUnitsLayer.OutlineCells = false;
    _enemySideUnitsLayer.HighlightCells = true;
    _enemySideUnitsLayer.HighlightColor = new Color(1, 0, 0, 0.15f);
    _enemySideUnitsLayer.LineWidth = 2f;
    _selectedUnitLayer.OutlineColor = Colors.Blue;
    _selectedUnitLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _selectedUnitLayer.OutlineColor = Colors.Blue;
    _selectedUnitLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _activeUnitLayer.OutlineColor = Colors.Yellow;
    _activeUnitLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _targetedCellsLayer.OutlineColor = Colors.Red;
    AmountOfNodesPerLayer = new int[_numLayers];

    _levelUnits = new List<UnitInfo>();
    _unitsPerStage = new List<List<UnitInfo>>(10);
    for (int i = 0; i < 11; i++)
    {
      _unitsPerStage.Add(new List<UnitInfo>());
    }
    ParseUnitsJson();

    string levelsJson = FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(levelsJson);
    _levelsData = (Dictionary)parsed;

    // Start with 2 turrets
    _decksHandler.AddUnit(GlobalConstants.UnitsData["turret"]);
    _decksHandler.AddUnit(GlobalConstants.UnitsData["turret"]);

    GenerateWorld();
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (_paused)
      return;

    if (_acting)
    {
      _actingCooldown -= delta;
      if (_actingCooldown <= 0)
      {
        _unitsToAct[_unitIndex].Act(_selectedTargets, _unitsGrid);
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
        _selectedTargets = _unitsToAct[_unitIndex].GetTargets(_unitsGrid, _unitsToAct, _removedUnits);
        _targetedCellsLayer.ShowCells(_selectedTargets);
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
        if (_unitIndex >= _unitsToAct.Count)
        {
          TurnEnd();
        }

        _activeUnitLayer.Clear();
        _activeUnit = null!;
        _targetedCellsLayer.Clear();
        if (_unitsToAct[_unitIndex].CanAct())
        {
          // Show overlay on selected cells
          _activeUnitLayer.ShowCells(_unitsToAct[_unitIndex].GetOccupiedCells());
          _activeUnit = _unitsToAct[_unitIndex];
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
    _worldMapButton.Visible = false;
    _playPauseButton.Visible = true;
    _surrenderButton.Visible = true;
    _gridOverlay.SetInteractionLocked(true);

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y * 0.5; y++)
      {
        if (_unitsGrid[x, y] != null)
          continue;

        UnitInfo enemyUnit = enemyUnits[x, y];

        if (enemyUnit != null) 
        {
          Unit unitInstance = GD.Load<PackedScene>(enemyUnit.ScenePath).Instantiate() as Unit;
          unitInstance!.Initialize(enemyUnit, false, new Vector2I(x, y), placed: true);
          _unitsNode.AddChild(unitInstance);
          _levelUnits.Add(enemyUnit);
        }
      }
    }

    // Show units side
    foreach (Unit unit in _units)
    {
      if (unit.side)
        _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
      else
        _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }

    // Copy units to create a list of units that are going to act this turn
    _unitsToAct = [.._units];
    _playing = true;

    // Immediately win or lose when you or the enemy has no units
    if (_playerUnitsCount == 0)
      Lose();
    else if (_enemyUnitsCount == 0)
      Win();
  }

  private void OnPlayButtonPressed()
  {
    UnitInfo[,] enemyUnits = LoadRandomLevel(_level);
    StartLevel(enemyUnits);
  }

  private UnitInfo[,] LoadLevel(string levelId)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    Dictionary levelData = (Dictionary)_levelsData[levelId];
    Godot.Collections.Array levelUnits = (Godot.Collections.Array)levelData["units"];
    foreach (Dictionary unitData in levelUnits)
    {
      int x = (int)unitData["x"];
      int y = (int)unitData["y"];
      string name = (string)unitData["name"];

      UnitInfo unitInfo = GlobalConstants.UnitsData[name];

      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[x + cell.X, y + cell.Y] = unitInfo;
      }
      mainCellsUnitGrid[x, y] = unitInfo;
    }

    return mainCellsUnitGrid;
  }

  public UnitInfo[,] LoadRandomLevel(int difficulty)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    int budget = difficulty; //+ (difficulty / 2);
    int maxStage = GetMaxStageForDifficulty(difficulty);

    RandomNumberGenerator rng = new();
    rng.Randomize();

    while (budget > 0)
    {
      int stage = rng.RandiRange(0, maxStage);

      List<UnitInfo> possibleUnits = _unitsPerStage[stage];
      if (possibleUnits.Count == 0)
        continue;

      UnitInfo unitInfo = possibleUnits[rng.RandiRange(0, possibleUnits.Count - 1)];
      int cost = unitInfo.OccupiedCells.Count;
      if (cost > budget) // TODO AKN (unitInfo.Cost > budget)
        continue;

      // Get random position for the unit, ensuring it fits within the grid and doesn't overlap with existing units
      List<Vector2I> possiblePositions = GlobalFunctions.GetPossibleUnitLocations(unitGrid, unitInfo.OccupiedCells, false);

      Vector2I cellPos = possiblePositions[rng.RandiRange(0, possiblePositions.Count - 1)];
      if (unitInfo.Id == "tank" || unitInfo.Id == "laser" || unitInfo.Id == "saboteur" || unitInfo.Id == "masochist" || unitInfo.Id == "bulwark")
      {
        // Position these units in the front portion of the grid
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
      else if (unitInfo.Id == "sniper")
      {
        // Position these units in back portion of the grid
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
      else if (unitInfo.Id == "pusher")
      {
        // Position these units in the right portion of the grid
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
      else if (unitInfo.Id == "booster")
      {
        // Position boosters next to another unit if possible
        Vector2I adjacentCell = new Vector2I(cellPos.X, cellPos.Y);
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.X + 1 < GlobalConstants.GridSize.X && unitGrid[cell.X + 1, cell.Y] != null && unitGrid[cell.X + 1, cell.Y].Damage > 0)
          {
            adjacentCell = cell;
            break;
          }
        }
        cellPos = adjacentCell;
      }
      else if (unitInfo.Id == "nurse")
      {
        // Position nurses behind another unit if possible
        Vector2I behindCell = new Vector2I(cellPos.X, cellPos.Y);
        foreach (Vector2I cell in possiblePositions)
        {
          if (cell.Y + 1 < GlobalConstants.GridSize.Y && unitGrid[cell.X, cell.Y + 1] != null)
          {
            behindCell = cell;
            break;
          }
        }
        cellPos = behindCell;
      }

      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[cellPos.X + cell.X, cellPos.Y + cell.Y] = unitInfo;
      }
      mainCellsUnitGrid[cellPos.X, cellPos.Y] = unitInfo;

      budget -= cost; // TODO AKN unitInfo.Cost;
    }

    return mainCellsUnitGrid;
  }

  private void Reset()
  {
    foreach (Unit unit in _units)
      unit.QueueFree();
    _units.Clear();
    _unitsToAct.Clear();

    // Removed units can still be active so make sure to remove those as well
    foreach (Unit unit in _removedUnits)
    {
      if (IsInstanceValid(unit))
        unit.QueueFree();
    }
    _removedUnits.Clear();

    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _unitIndex = 0;
    _enemyUnitsCount = 0;
    _playerUnitsCount = 0;
    _turn = 0;
    _turnCounter.Text = _turn.ToString();
    _turnCooldown = _turnStartCooldown;
    ClearMessagePanel();
    _messagePanel.Hide();
    ResetGameStats();
    _gridOverlay.LoadUnits();
    _levelCounter.Text = _level.ToString();
    _activeUnitLayer.Clear();
    _targetedCellsLayer.Clear();
    _turnEndDamage = 1;
    _levelUnits.Clear();
    _playerSideUnitsLayer.Clear();
    _enemySideUnitsLayer.Clear();

    _gridOverlay.SetInteractionLocked(false);
    _playButton.Disabled = false;
    _worldMapButton.Visible = true;
    _playPauseButton.Visible = false;
    _surrenderButton.Visible = false;
  }

  private void Win()
  {
    if (!_playing)
      return;
    _playing = false;
    
    ShowGameStats();

    if (_activeLevel.Gauntlet && _activeGauntletLevelIndex + 1 < _activeLevel.Units.Count)
    {
      int levelsLeft = _activeLevel.Units.Count - (_activeGauntletLevelIndex + 1);
      string levelText = levelsLeft > 1 ? "levels" : "level";
      _message.Text = $"You won level {_activeGauntletLevelIndex + 1} of the gauntlet.\n{levelsLeft} more {levelText} to go.";
      _activeGauntletLevelIndex++;
      Button btn = new Button();
      btn.Text = "Next level";
      btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
      btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
      btn.CustomMinimumSize = new Vector2I(150, 50);
      btn.Pressed += OnNextGauntletLevelButtonPressed;
      _messageResponses.AddChild(btn);
      _messagePanel.Show();
      return;
    }

    if (_activeLevel.Layer == _numLayers - 1)
    {
      _message.Text = "You win the demo!\nThanks for playing!";
      _messagePanel.Show();
      return;
    }

    _activeGauntletLevelIndex = 0;
    _message.Text = "You Win!\nChoose your reward:";
    _level += 1;
    ShowRewards(_activeLevel.Rewards);
    _messagePanel.Show();
    UpdateCoins(_activeLevel.CoinsReward);

    if (_activeLevel != null)
    {
      _activeLevel.Completed = true;

      // Set button to green to indicate it has been completed
      Color color = Colors.LightGreen;
      StyleBoxFlat style = GetRewardButtonStyle(color);
      StyleBoxFlat stylePressed = GetRewardButtonStyle(color.Darkened(0.2f));
      StyleBoxFlat styleHover = GetRewardButtonStyle(color.Lightened(0.2f));
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("normal", style);
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("pressed", stylePressed);
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("hover", styleHover);

      if (_activeLevel.NextNodes.Count > 0)
      {
        // Up number of units slot if the player unlocked a new layer
        if (_activeLevel.Layer >= _completedLayers)
        {
          _completedLayers++;
          if (_completedLayers < _slowDownLayer && _completedLayers % 2 == 0)
            _gridOverlay.IncreaseUnitCount(1);
          else if (_completedLayers % 4 == 0)  // Only increase max unit slots by 1 every 4 layers after layer 20
            _gridOverlay.IncreaseUnitCount(1);
        }
      }

      foreach (string nextNodeId in _activeLevel.NextNodes)
      {
        if (Levels.ContainsKey(nextNodeId) && !Levels[nextNodeId].Unlocked)
        {
          Levels[nextNodeId].Unlocked = true;
          _levelButtons[nextNodeId].Disabled = false;
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
    ShowGameStats();
    _activeGauntletLevelIndex = 0;
  }

  private void ShowRewards(List<UnitInfo> rewardUnits)
  {
    List<UnitInfo> localRewardUnits = [.. rewardUnits];

    // No specific rewards given means show 3 random units from this level
    if (localRewardUnits.Count == 0)
    {
      int maxButtons = Mathf.Min(3, _levelUnits.Count);
      var chosenIndices = new List<int>();

      while (chosenIndices.Count < maxButtons)
      {
        int index = _rng.Next(0, _levelUnits.Count);
        if (!chosenIndices.Contains(index))
        {
          chosenIndices.Add(index);
          localRewardUnits.Add(_levelUnits[index]);
        }
      }
    }


    VBoxContainer vbox = new();
    vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
    _messageResponses.AddChild(vbox);

    HBoxContainer hbox = new();
    hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
    hbox.Alignment = BoxContainer.AlignmentMode.Center;
    vbox.AddChild(hbox);

    List<Button> rewardButtons = new();

    foreach (UnitInfo unitInfo in localRewardUnits)
    {
      Button btn = new Button();
      btn.Text = unitInfo.Name;
      btn.Icon = unitInfo.Texture;
      btn.IconAlignment = HorizontalAlignment.Center;
      btn.VerticalIconAlignment = VerticalAlignment.Bottom;
      btn.ExpandIcon = true;
      btn.SizeFlagsVertical = SizeFlags.ExpandFill;

      Color rarityColor = GetRarityColor(unitInfo.Rarity);
      btn.AddThemeStyleboxOverride("normal", GetRewardButtonStyle(rarityColor));
      btn.AddThemeStyleboxOverride("pressed", GetRewardButtonStyle(rarityColor.Darkened(0.2f)));
      btn.AddThemeStyleboxOverride("hover", GetRewardButtonStyle(rarityColor.Lightened(0.2f)));

      btn.Pressed += () => OnRewardButtonSelected(unitInfo, btn, rewardButtons);

      rewardButtons.Add(btn);
      hbox.AddChild(btn);
    }

    // Add confirm button
    Button confirmBtn = new();
    confirmBtn.Text = "Confirm";
    confirmBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    confirmBtn.CustomMinimumSize = new Vector2I(150, 0);
    confirmBtn.Pressed += OnConfirmRewardPressed;
    vbox.AddChild(confirmBtn);
  }

  private StyleBoxFlat GetRewardButtonStyle(Color rarityColor)
  {
    StyleBoxFlat style = new();
    style.BgColor = rarityColor;
    style.CornerRadiusTopLeft = 6;
    style.CornerRadiusTopRight = 6;
    style.CornerRadiusBottomLeft = 6;
    style.CornerRadiusBottomRight = 6;
    style.ContentMarginLeft = 8;
    style.ContentMarginRight = 8;
    style.ContentMarginTop = 4;
    style.ContentMarginBottom = 4;

    return style;
  }

  private Color GetRarityColor(string rarity)
  {
    Color rarityColor;
    switch (rarity)
    {
      case "common":
        rarityColor = new Color("424040");
        break;
      case "rare":
        rarityColor = new Color("1f08c9");
        break;
      case "epic":
        rarityColor = new Color("8207ab");
        break;
      case "legendary":
        rarityColor = new Color("cf8621");
        break;
      default:
        rarityColor = new Color("424040");
        break;
    }

    return rarityColor;
  }

  private void OnUnitDied(Unit unit)
  {
    int index = _unitsToAct.IndexOf(unit);
    if (index <= _unitIndex)
      _unitIndex -= 1;

    RemoveUnit(unit);
    _removedUnits.Add(unit);

    if (_enemyUnitsCount == 0)
      Win();
    else if (_playerUnitsCount == 0)
      Lose();
  }

  private void OnUnitRemoved(Unit unit)
  {
    RemoveUnit(unit);
  }

  private void RemoveUnit(Unit unit)
  {
    if (_unitsGuiInfo.selectedUnit == unit)
    {
      _unitsGuiInfo.ResetSelectedUnit();
      _selectedUnitLayer.Clear();
    }

    if (_playing)
    {
      // Update unit side layers
      if (unit.side)
        _playerSideUnitsLayer.RemoveCells(unit.GetOccupiedCells());
      else
        _enemySideUnitsLayer.RemoveCells(unit.GetOccupiedCells());
    }
    else
    {
      // Remove units from unit activity that were removed outside of playing
      _unitsActivity.Remove(unit);
    }

    _units.Remove(unit);
    _unitsToAct.Remove(unit);

    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = null!;
    }

    // ^ = xor, which switches the condition if switched sides is true
    if (unit.side ^ unit.SwitchedSides)
      _playerUnitsCount--;
    else
      _enemyUnitsCount--;
  }

  private void OnUnitMoved(Unit unit, Vector2I oldCell, bool playing)
  {
    if (_unitsGuiInfo.selectedUnit == unit)
    {
      _selectedUnitLayer.Clear();
      _selectedUnitLayer.ShowCells(unit.GetOccupiedCells());
    }
    if (_activeUnit == unit)
    {
      _activeUnitLayer.Clear();
      _activeUnitLayer.ShowCells(unit.GetOccupiedCells());
    }

    List<Vector2I> removedCells = new List<Vector2I>();
    foreach (Vector2I cell in unit.occupiedCells)
    {
      Vector2I removedCell = new Vector2I(oldCell.X + cell.X, oldCell.Y + cell.Y);
      if (_unitsGrid[removedCell.X, removedCell.Y] == unit)  // Swapping can make it so a new unit is on this unit's old position
        _unitsGrid[removedCell.X, removedCell.Y] = null!;
      removedCells.Add(removedCell);
    }
    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = unit;
    }

    // Update unit side layers
    if (_playing)
    {
      if (unit.side)
      {
        _playerSideUnitsLayer.RemoveCells(removedCells);
        _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
      }
      else
      {
        _enemySideUnitsLayer.RemoveCells(removedCells);
        _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
      }
    }

    if (_playing)
      SortUnits(_unitsToAct);
    else
      SortUnits(_units);
  }

  private void OnUnitSpawned(Unit unit, bool playing)
  {
    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = unit;
    }
    _units.Add(unit);
    _unitsActivity[unit] = new UnitActivity(0, 0, 0, 0);
    if (_playing)
    {
      _unitsToAct.Add(unit);

      // Update unit side layers
      if (unit.side)
        _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
      else
        _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }

    if (unit.side)
      _playerUnitsCount++;
    else
      _enemyUnitsCount++;

    if (_playing)
      SortUnits(_unitsToAct);
    else
      SortUnits(_units);
  }

  private void OnUnitSizeChanged(Unit unit, Godot.Collections.Array<Vector2I> oldOccupiedCells)
  {
    if (_unitsGuiInfo.selectedUnit == unit)
    {
      _selectedUnitLayer.Clear();
      _selectedUnitLayer.ShowCells(unit.GetOccupiedCells());
    }
    if (_activeUnit == unit)
    {
      _activeUnitLayer.Clear();
      _activeUnitLayer.ShowCells(unit.GetOccupiedCells());
    }

    // Update unit side layers
    if (unit.side)
    {
      _playerSideUnitsLayer.RemoveCells([.. oldOccupiedCells]);
      _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }
    else
    {
      _enemySideUnitsLayer.RemoveCells([.. oldOccupiedCells]);
      _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }

    foreach (Vector2I cell in oldOccupiedCells)
    {
      _unitsGrid[cell.X, cell.Y] = null!;
    }

    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      _unitsGrid[cell.X, cell.Y] = unit;
    }

    if (_playing)
      SortUnits(_unitsToAct);
    else
      SortUnits(_units);
  }

  private void OnUnitSpeedChanged(Unit unit)
  {
    if (_playing)
      SortUnits(_unitsToAct);
    else
      SortUnits(_units);
  }

  private void OnUnitSideChanged(Unit unit)
  {
    // Update unit side layers
    if (unit.side)
    {
      _enemySideUnitsLayer.RemoveCells(unit.GetOccupiedCells());
      _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }
    else
    {
      _playerSideUnitsLayer.RemoveCells(unit.GetOccupiedCells());
      _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }
  }

  private void OnUnitDamageDealt(Unit unit, int amount)
  {
    _unitsActivity[unit].DamageDealth += amount;
  }

  private void OnUnitDamageTaken(Unit unit, int amount)
  {
    _unitsActivity[unit].DamageTaken += amount;
  }

  private void OnUnitHealingDone(Unit unit, int amount)
  {
    _unitsActivity[unit].HealingDone += amount;
  }

  private void OnUnitHealingReceived(Unit unit, int amount)
  {
    _unitsActivity[unit].HealingReceived += amount;
  }

  private void OnRewardButtonSelected(UnitInfo unitInfo, Button selected, List<Button> allButtons)
  {
    _selectedReward = unitInfo;
    _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitInfoSelected, unitInfo);

    // Dim all buttons then highlight the selected one
    foreach (Button btn in allButtons)
      btn.Modulate = new Color(0.5f, 0.5f, 0.5f);
    selected.Modulate = new Color(1f, 1f, 1f);
  }

  private void OnConfirmRewardPressed()
  {
    if (_selectedReward == null) return;

    _decksHandler.AddUnit(_selectedReward);
    _decksHandler.ResetUnitsSelection();
    _selectedReward = null;
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
      case 6:
        _speed = 32.0f;
        break;
    }
    _speedButton.Text = "Speed: " + _speedPopup.GetItemText((int)id);
    _turnStartCooldown = 1.0f / _speed;
    _actingStartCooldown = 0.5f / _speed;
  }

  private void ParseUnitsJson()
  {
    string unitsJson = FileAccess.Open("res://scripts/units/units.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(unitsJson);
    Dictionary unitsData = (Dictionary)parsed;
    foreach (KeyValuePair<Variant, Variant> entry in unitsData)
    {
      string unitId = entry.Key.ToString();
      Dictionary unitData = (Dictionary)entry.Value;

      string displayName = (string)unitData["display_name"];
      string texturePath = (string)unitData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)unitData["scene"];
      int cost = (int)unitData["cost"];
      int health = (int)unitData["health"];
      int damage = (int)unitData["damage"];
      int armor = (int)unitData["armor"];
      int speed = (int)unitData["speed"];
      int cooldown = (int)unitData["cooldown"];
      string description = (string)unitData["description"];
      string rarity = (string)unitData["rarity"];
      List<string> types = unitData["types"].AsGodotArray().Select(t => t.AsString()).ToList();

      Godot.Collections.Array cells = (Godot.Collections.Array)unitData["cells"];
      List<Vector2I> occupiedCells = new();
      foreach (Godot.Collections.Array cell in cells)
      {
        int x = (int)cell[0];
        int y = (int)cell[1];
        occupiedCells.Add(new Vector2I(x, y));
      }

      GlobalConstants.UnitsData[unitId] = new UnitInfo(unitId, displayName, texture, scenePath, occupiedCells, 
        cost, health, health, damage, armor, speed, cooldown, cooldown, description, rarity, types);

      int stage = (int)unitData["stage"];
      _unitsPerStage[stage].Add(GlobalConstants.UnitsData[unitId]);
    }
  }

  int GetMaxStageForDifficulty(int difficulty)
  {
    if (difficulty < 2) return 0;
    if (difficulty < 6) return 1;
    if (difficulty < 10) return 2;
    if (difficulty < 16) return 3;
    if (difficulty < 22) return 4;
    return 5;
  }

  void PopulateLevels()
  {
    List<List<LevelInfo>> layers = new();
    int currentNodesInLayer = 1;

    for (int layer = 0; layer < _numLayers; layer++)
    {
      List<LevelInfo> layerNodes = new();
      string nodeId;
      string nodeName;
      bool isBoss = (layer + 1) % 100 == 0;

      if (isBoss)
      {
        string levelId;
        if (layer + 1 == 10)
        {
          nodeName = "Golden Turret";
          levelId = "golden_turret";
        }
        else if (layer + 1 == 20)
        {
          nodeName = "Blob";
          levelId = "blob";
        }
        else
        {
          nodeName = "Nuke";
          levelId = "nuke";
        }

        nodeId = layer.ToString() + '_' + nodeName;
        Levels[nodeId] = new LevelInfo(
            id: nodeId,
            name: nodeName,
            completed: false,
            unlocked: layer == 0,
            layer: layer,
            layerIndex: 0,
            boss: true,
            gauntlet: false,
            rewards: [],  // Empty rewards means the player can choose from 1 of 3 random units in the level
            coinsReward: _coinsPerWin,
            nextNodes: new List<string>(),
            units: [LoadLevel(levelId)]
        );
        layerNodes.Add(Levels[nodeId]);
        AmountOfNodesPerLayer[layer] = 1;
      }
      else
      {
        for (int node = 0; node < currentNodesInLayer; node++)
        {
          // One in ten levels will be a gauntlet, but only after layer 5
          nodeId = $"L{layer + 1}_N{node + 1}";
          if (layer > 4 && _rng.NextDouble() < 0.2)
          {
            int numGauntletLevels = 4;
            List<UnitInfo[,]> gauntletUnits = new List<UnitInfo[,]>();
            for (int i = 0; i < numGauntletLevels; i++)
              gauntletUnits.Add(LoadRandomLevel(layer));

            List<UnitInfo> rewardUnits = gauntletUnits
              .SelectMany(grid => grid.Cast<UnitInfo>())
              .Where(u => u != null)
              .DistinctBy(u => u.Id)
              .OrderBy(_ => _rng.Next())
              .Take(3)
              .ToList();

            nodeName = $"Gauntlet";
            Levels[nodeId] = new LevelInfo(
                id: nodeId,
                name: nodeName,
                completed: false,
                unlocked: layer == 0,
                layer: layer,
                layerIndex: node,
                boss: false,
                gauntlet: true,
                rewards: rewardUnits,
                coinsReward: _coinsPerWin * 5,
                nextNodes: new List<string>(),
                units: gauntletUnits
            );
          }
          else
          {
            nodeName = $"L{layer + 1}_N{node + 1}";
            Levels[nodeId] = new LevelInfo(
                id: nodeId,
                name: nodeName,
                completed: false,
                unlocked: layer == 0,
                layer: layer,
                layerIndex: node,
                boss: false,
                gauntlet: false,
                rewards: [],  // Empty rewards means the player can choose from 1 of 3 random units in the level
                coinsReward: _coinsPerWin,
                nextNodes: new List<string>(),
                units: [LoadRandomLevel(layer + 1)]
            );
          }
          layerNodes.Add(Levels[nodeId]);
        }
        AmountOfNodesPerLayer[layer] = currentNodesInLayer;
      }

      layers.Add(layerNodes);

      if (layer > 0)
        ConnectLayers(layers[layer - 1], layers[layer], isBoss);

      currentNodesInLayer = _rng.Next(1, _maxNodesPerLayer + 1);
    }
  }

  public void GenerateWorldUi()
  {
    _worldUi.CustomMinimumSize = new Vector2(
        _maxNodesPerLayer * _worldUiSpacing + 300,
        _numLayers * 0.5f * _worldUiSpacing + 200
    );

    // Group levels by layer
    var layers = Levels.Values
        .GroupBy(l => l.Layer)
        .OrderBy(g => g.Key)
        .Select(g => g.OrderBy(l => l.LayerIndex).ToList())
        .ToList();

    // Add UI for all layers except the last
    for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
    {
      bool unlockedLayer = false;
      foreach (LevelInfo levelNode in layers[layerIndex])
      {
        AddLevelUi(levelNode, layers[layerIndex].Count);
        if (levelNode.Unlocked)
          unlockedLayer = true;
      }

      if (unlockedLayer && layerIndex > 0)
      {
        _completedLayers++;
        if (_completedLayers < _slowDownLayer && _completedLayers % 2 == 0)
          _gridOverlay.IncreaseUnitCount(1);
        else if (_completedLayers % 4 == 0)  // Only increase max unit slots by 1 every 4 layers after layer 20
          _gridOverlay.IncreaseUnitCount(1);
      }
    }
  }

  void ConnectLayers(List<LevelInfo> prev, List<LevelInfo> next, bool isBoss)
  {
    int prevCount = prev.Count;
    int nextCount = next.Count;

    if (isBoss)
    {
      // Simply connect all nodes to the boss if there is one
      for (int i = 0; i < prevCount; i++)
      {
        prev[i].NextNodes.Add(next[0].Id);
      }
      return;
    }

    // Guarantee every next node has at least one incoming
    for (int j = 0; j < nextCount; j++)
    {
      int closestPrev = nextCount == 1 ? 0 : Mathf.RoundToInt((float)j / (nextCount - 1) * (prevCount - 1));
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
            if (next.IndexOf(Levels[levelNode]) > target)
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

  void AddLevelUi(LevelInfo levelNode, int nodesInLayer)
  {
    Vector2 buttonSize = new Vector2(_buttonWidth, _buttonHeight);
    Vector2 buttonPos = GetLevelButtonPosition(levelNode.Layer, levelNode.LayerIndex, nodesInLayer);

    Button button = new();
    button.Text = levelNode.Name;
    button.Disabled = !levelNode.Unlocked;
    button.CustomMinimumSize = buttonSize;
    button.Position = buttonPos;
    button.Pressed += () => OnLevelSelected(levelNode);

    if (levelNode.Completed)
    {
      // Set button to green to indicate it has been completed
      Color color = Colors.LightGreen;
      StyleBoxFlat style = GetRewardButtonStyle(color);
      StyleBoxFlat stylePressed = GetRewardButtonStyle(color.Darkened(0.2f));
      StyleBoxFlat styleHover = GetRewardButtonStyle(color.Lightened(0.2f));
      button.AddThemeStyleboxOverride("normal", style);
      button.AddThemeStyleboxOverride("pressed", stylePressed);
      button.AddThemeStyleboxOverride("hover", styleHover);
    }

    _levelButtons[levelNode.Id] = button;
    _worldUi.AddChild(button);

    //Vector2 fromCenter = btn.Position + btn.Size / 2.0f;
    // Convert to global coordinates relative to the CanvasLayer so Line2D (a Node2D) can be added there
    //Vector2 fromGlobal = _worldUi.RectGlobalPosition + fromCenter;
    Vector2 fromGlobal = buttonPos + buttonSize / 2.0f;

    foreach (string id in levelNode.NextNodes)
    {

      Vector2 nextButtonPos = GetLevelButtonPosition(levelNode.Layer + 1, Levels[id].LayerIndex, AmountOfNodesPerLayer[Levels[id].Layer]);

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

  private void OnLevelSelected(LevelInfo levelInfo)
  {
    _levelInfoContainer.DisplayInfo(levelInfo);
  }

  private void OnStartLevelPressed(LevelInfo levelInfo)
  {
    _levelInfoContainer.Hide();
    _decksHandler.Show();

    // Ask player if he wants to redo the level
    if (levelInfo.Completed)
    {
      ClearMessagePanel();

      int levelRedoCost = levelInfo.Gauntlet ? Mathf.FloorToInt(_levelRedoCost * 2.5f) : _levelRedoCost;
      if (Coins >= levelRedoCost)
      {
        _message.Text = $"You have already completed this level. Do you want to repeat the level for {levelRedoCost} coins?";
        Button yesBtn = new Button();
        yesBtn.Text = "Yes";
        yesBtn.SizeFlagsVertical = SizeFlags.ExpandFill;
        yesBtn.Pressed += () => OnRedoConfirmed(levelInfo);
        yesBtn.CustomMinimumSize = new Vector2I(80, 0);
        _messageResponses.AddChild(yesBtn);

        Button noBtn = new Button();
        noBtn.Text = "No";
        noBtn.SizeFlagsVertical = SizeFlags.ExpandFill;
        noBtn.Pressed += OnRedoCanceled;
        noBtn.CustomMinimumSize = new Vector2I(80, 0);
        _messageResponses.AddChild(noBtn);
      }
      else
      {
        _message.Text = $"You have already completed this level. You do not have the required {levelRedoCost} coins to repeat this level.";
        Button btn = new Button();
        btn.Text = "Okay... :(";
        btn.SizeFlagsVertical = SizeFlags.ExpandFill;
        btn.Pressed += () => OnRedoCanceled();
        btn.CustomMinimumSize = new Vector2I(80, 0); 
        _messageResponses.AddChild(btn);
      }

      _worldUi.GetParent<ScrollContainer>().Visible = false;
      _messagePanel.Show();

    }
    else
    {
      _worldUi.GetParent<ScrollContainer>().Hide();
      _activeLevel = levelInfo;
      StartLevel(levelInfo.Units[0]);
    }
  }

  private void OnRedoConfirmed(LevelInfo levelInfo)
  {
    ClearMessagePanel();
    _messagePanel.Hide();

    int levelRedoCost = levelInfo.Gauntlet ? Mathf.FloorToInt(_levelRedoCost * 2.5f) : _levelRedoCost;
    UpdateCoins(-levelRedoCost);
    _worldUi.GetParent<ScrollContainer>().Hide();
    _activeLevel = levelInfo;
    StartLevel(levelInfo.Units[0]);
  }

  private void OnRedoCanceled()
  {
    ClearMessagePanel();
    _messagePanel.Hide();
    _worldUi.GetParent<ScrollContainer>().Visible = true;
  }

  private void ClearMessagePanel()
  {
    _message.Text = "";

    // Clear message responses
    foreach (Node child in _messageResponses.GetChildren())
    {
      child.QueueFree();
    }
  }

  private void OnWorldMapPressed()
  {
    if (_worldUi.GetParent<ScrollContainer>().Visible)
    {
      _worldUi.GetParent<ScrollContainer>().Hide();
      _decksHandler.Show();
      _levelInfoContainer.Hide();
    }
    else
    {
      _worldUi.GetParent<ScrollContainer>().Show();
      _decksHandler.Hide();
      _levelInfoContainer.Show();
    }

    if (!_openedWorldOnce)
    {
      _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_vertical", Mathf.FloorToInt(_numLayers * 0.5f * _worldUiSpacing + 200));
      _openedWorldOnce = true;
    }
  }

  private void GenerateWorld()
  {
    PopulateLevels();
    GenerateWorldUi();
  }

  public void LoadLevels()
  {
    // This function assumes the Levels variable has already been correctly loaded with new levels

    // Clear existing levels
    foreach (var child in _worldUi.GetChildren())
    {
      child.QueueFree();
    }

    // Add newly loaded levels
    GenerateWorldUi();
  }


  public override void _Input(InputEvent @event)
  {
    if (@event is InputEventMouseButton mouse &&
        mouse.ButtonIndex == MouseButton.Left &&
        mouse.Pressed)
    {
      Vector2 mousePos = mouse.Position;
      Vector2I cell = _gridOverlay.GetCellUnderMouse(mousePos) - new Vector2I(1, 1);

      if (!GlobalFunctions.IsCellInsideGrid(cell))
      {
        _unitsGuiInfo.ResetSelectedUnit();
        _selectedUnitLayer.Clear();
        return;
      }

      if (_playing)
      {
        Unit unit = _unitsGrid[cell.X, cell.Y];
        if (unit != null)
        {
          _unitsGuiInfo.SetSelectedUnit(unit);
          _selectedUnitLayer.Clear();
          _selectedUnitLayer.ShowCells(unit.GetOccupiedCells());
        }
        else
        {
          _unitsGuiInfo.ResetSelectedUnit();
          _selectedUnitLayer.Clear();
        }
      }
      else
      {
        _unitsGuiInfo.ResetSelectedUnit();
        _selectedUnitLayer.Clear();
        Unit gridOverlayUnit = _gridOverlay.GetUnits()[cell.X, cell.Y];
        if (gridOverlayUnit != null)
        {
          _unitsGuiInfo.SetSelectedUnit(gridOverlayUnit);
          _selectedUnitLayer.ShowCells(gridOverlayUnit.GetOccupiedCells());
        }
      }
    }
  }

  public void OnPlayPauseButtonPressed()
  {
    if (_paused)
    {
      _paused = false;
      _playPauseButton.Text = "Pause";
    }
    else
    {
      _paused = true;
      _playPauseButton.Text = "Play";
    }
  }

  public void OnSurrenderButtonPressed()
  {
    if (!_playing)
      return;
    Lose();
  }

  private void RespawnZombies()
  {
    List<Unit> removedZombies = new List<Unit>();
    foreach (Unit unit in _removedUnits)
    {
      if (unit.id == "zombie")
      {
        removedZombies.Add(unit);

        Vector2I? spawnCell = GlobalFunctions.GetRandomUnitSpawnLocation(_unitsGrid, unit.occupiedCells, unit.side);
        if (spawnCell == null)
          continue;

        Unit unitInstance = GD.Load<PackedScene>(unit.scenePath).Instantiate() as Unit;
        unitInstance!.Initialize(unit.GetStartInfo(), unit.side, spawnCell.Value);
        _unitsNode.AddChild(unitInstance);
        unitInstance.SpawnFloatingText("Revived", Colors.Green);
      }
    }

    foreach (Unit unit in removedZombies)
    {
      if (IsInstanceValid(unit))
        unit.QueueFree();
      _removedUnits.Remove(unit);
    }
  }

  private void SortUnits(List<Unit> units)
  {
    int currentIndex = _unitIndex;
    if (_targeting || _acting)
      currentIndex++;  // Increase current index by one to ensure the unit that is acting now is not resorted

    // Sort units by speed, then by position for consistent turn order
    List<Unit> sorted = units
    .Skip(currentIndex)  // Skip units that have already played their turn
    .OrderByDescending(u => u.speed)  // Speed first
    .ThenBy(u => u.GlobalPosition.X)  // Leftmost first
    .ThenBy(u =>
        u.side
            ? u.occupiedMainCell.Y  // Player: lower Y first
            : GlobalConstants.GridSize.Y - 1 - u.occupiedMainCell.Y)  // Enemy: higher Y first
    .ToList();

    units.RemoveRange(currentIndex, units.Count - currentIndex);
    units.AddRange(sorted);
  }

  private void TurnEnd()
  {
    _unitIndex = 0;
    _turn += 1;
    SortUnits(_units);
    _unitsToAct = [.._units];  // Reset units to act to level units list
    _turnCounter.Text = _turn.ToString();
    if (_turn > 10)
    {
      // Apply end of turn damage to all units
      int index = 0;
      while (index < _units.Count)
      {
        Unit unit = _units[index];
        unit.ChangeHealth(-_turnEndDamage, null);
        // If the unit died from end of turn damage, it will be removed from the list, so we don't increment the index
        if (_units.Contains(unit))
          index += 1;
      }
      _turnEndDamage += 1;  // Increase end of turn damage for next turn
      _turnCooldown = _turnStartCooldown;
      _unitIndex = 0;  // Set to 0 again incase a unit died and change the _unitIndex value
      return;
    }

    // Possibly respawn zombies
    RespawnZombies();

    // Call turn end function of every unit
    foreach (Unit unit in _units)
      unit.TurnEnd(_unitsGrid, _units, _removedUnits);
  }

  public void UpdateCoins(int amount)
  {
    Coins += amount;
    _coinsCounter.Text = Coins.ToString();
  }

  private void AddStatRow(Control container, string unitName, int value, int maxValue, bool side)
  {
    HBoxContainer row = new();

    Label nameLabel = new();
    nameLabel.Text = "  " + unitName;
    nameLabel.CustomMinimumSize = new Vector2(150, 0);
    row.AddChild(nameLabel);

    Panel barBackground = new();
    barBackground.CustomMinimumSize = new Vector2(200, 20);
    StyleBoxFlat bgStyle = new();
    bgStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);
    bgStyle.CornerRadiusTopLeft = bgStyle.CornerRadiusTopRight =
    bgStyle.CornerRadiusBottomLeft = bgStyle.CornerRadiusBottomRight = 4;
    barBackground.AddThemeStyleboxOverride("panel", bgStyle);

    Panel barFill = new();
    float fillRatio = maxValue > 0 ? (float)value / maxValue : 0;
    barFill.CustomMinimumSize = new Vector2(200 * fillRatio, 20);
    StyleBoxFlat fillStyle = new();
    fillStyle.BgColor = side ? Colors.LightGreen : Colors.LightSalmon;
    fillStyle.CornerRadiusTopLeft = fillStyle.CornerRadiusTopRight =
    fillStyle.CornerRadiusBottomLeft = fillStyle.CornerRadiusBottomRight = 4;
    barFill.AddThemeStyleboxOverride("panel", fillStyle);

    barBackground.AddChild(barFill);
    row.AddChild(barBackground);

    Label valueLabel = new();
    valueLabel.Text = value.ToString();
    valueLabel.CustomMinimumSize = new Vector2(50, 0);
    row.AddChild(valueLabel);

    container.AddChild(row);
  }

  private void ShowGameStats()
  {
    var sortedByDamageDealt = _unitsActivity
        .OrderByDescending(x => x.Value.DamageDealth)
        .ToList();
    int maxDamageDealt = sortedByDamageDealt.FirstOrDefault().Value?.DamageDealth ?? 1;

    foreach (var entry in sortedByDamageDealt)
      AddStatRow(_damageDealtStats, entry.Key.name, entry.Value.DamageDealth, maxDamageDealt, entry.Key.side);

    var sortedByDamageTaken = _unitsActivity
        .OrderByDescending(x => x.Value.DamageTaken)
        .ToList();
    int maxDamageTaken = sortedByDamageTaken.FirstOrDefault().Value?.DamageTaken ?? 1;

    foreach (var entry in sortedByDamageTaken)
      AddStatRow(_damageTakenStats, entry.Key.name, entry.Value.DamageTaken, maxDamageTaken, entry.Key.side);

    var sortedByHealingDone = _unitsActivity
        .OrderByDescending(x => x.Value.HealingDone)
        .ToList();
    int maxHealingDone = sortedByHealingDone.FirstOrDefault().Value?.HealingDone ?? 1;

    foreach (var entry in sortedByHealingDone)
      AddStatRow(_healingDoneStats, entry.Key.name, entry.Value.HealingDone, maxHealingDone, entry.Key.side);

    _gameStats.Show();
  }

  private void ResetGameStats()
  {
    _unitsActivity.Clear();
    _gameStats.Hide();
    foreach (Node child in _damageDealtStats.GetChildren())
      child.QueueFree();
    foreach (Node child in _damageTakenStats.GetChildren())
      child.QueueFree();
    foreach (Node child in _healingDoneStats.GetChildren())
      child.QueueFree();
  }

  private void OnStatisticSelected(long id)
  {
    _damageDealtStats.GetParent<ScrollContainer>().Hide();
    _damageTakenStats.GetParent<ScrollContainer>().Hide();
    _healingDoneStats.GetParent<ScrollContainer>().Hide();

    switch (id)
    {
      case 0:
        _damageDealtStats.GetParent<ScrollContainer>().Show();
        break;
      case 1:
        _damageTakenStats.GetParent<ScrollContainer>().Show();
        break;
      case 2:
        _healingDoneStats.GetParent<ScrollContainer>().Show();
        break;
    }
    _statisticsButton.Text = _statisticsPopup.GetItemText((int)id);
  }

  private void OnNextGauntletLevelButtonPressed()
  {
    Reset();
    _messagePanel.Hide();
    StartLevel(_activeLevel.Units[_activeGauntletLevelIndex]);
  }
}
