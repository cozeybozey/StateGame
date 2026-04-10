using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading;
using static Godot.Control;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class World : Node2D
{
  private CanvasLayer _canvasLayer;
  private Label _levelCounter;
  private Label _turnCounter;
  private Label _coinsCounter;
  private Panel _messagePanel;
  private RichTextLabel _message;
  private HBoxContainer _messageResponses;
  private GridOverlay _gridOverlay;
  private GlobalSignals _globalSignals;
  private Node _unitsNode;
  private Node _terrainNode;
  private Node _propsNode;
  private OverlayLayer _playerSideUnitsLayer;
  private OverlayLayer _enemySideUnitsLayer;
  private OverlayLayer _selectedCellsLayer;
  private OverlayLayer _activeUnitLayer;
  private OverlayLayer _targetedCellsLayer;
  private MenuButton _speedButton;
  private PopupMenu _speedPopup;
  private InfoGui _infoGui;
  private Button _playPauseButton;
  private Button _surrenderButton;
  private Button _startButton;
  private Button _quitButton;
  private DecksHandler _decksHandler;
  private LevelInfoContainer _levelInfoContainer;
  private Label _levelsUntilNextUnlock;

  private List<Unit> _units;
  private List<Unit> _unitsToAct;
  private List<Unit> _removedUnits;
  private Unit[,] _unitsGrid;
  private Terrain[,] _terrainGrid;
  private Prop[,] _propsGrid;
  private Unit? _unitToAct = null;
  private UnitInfo[,] _playerUnitsGridBeforeLevel;
  private int _playerUnitsCount = 0;
  private int _enemyUnitsCount = 0;
  List<Vector2I> _selectedTargets;

  private bool _playing = false;
  private int _level = 1;
  private int _completedLevels = 0;
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
  private int _bossLevelsDefeated = 0;

  public int Coins = 0;
  private int _coinsPerWin = 20;
  private int _levelRedoCost = 100;

  private List<UnitInfo> _levelUnits; // List of units in the current level, used for rewards
  private Dictionary _levelsData;
  private Random _rng = new Random();
  private UnitInfo? _selectedReward = null;

  // World generation
  public Godot.Collections.Dictionary<string, LevelInfo> Levels = new Godot.Collections.Dictionary<string, LevelInfo>();
  public int[,] AmountOfNodesPerLayerPerSection;
  private Godot.Collections.Dictionary<string, Button> _levelButtons = new();
  private Panel _worldUi;
  private Button _worldMapButton;
  private int _numLayers = 12;
  private int _maxNodesPerLayer = 8;
  private int _buttonWidth = 85;
  private int _buttonHeight = 65;
  private LevelInfo _activeLevel = null!;
  private int _activeGauntletLevelIndex = 0;
  private int _worldUiSpacing = 100;
  private bool _openedWorldOnce = false;
  private int _maxNodesInLayer = 0;
  List<string> _levelSections = ["Earth", "Heaven", "Future", "Graveyard", "Hell"];

  private Vector2 _worldCenter;
  private float _ringSpacing = 150f; // pixels between rings
  private int _numLevelSections = 5;

  // Notify variables
  private string _explanationMarkScenePath = "res://scenes/explanation_mark.tscn";
  private Node2D[,] _explanationMarks = new Node2D[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

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
    _levelCounter = GetNode<Label>("CanvasLayer/BottomUi/LevelCounter/Counter");
    _turnCounter = GetNode<Label>("CanvasLayer/BottomUi/TurnCounter/Counter");
    _coinsCounter = GetNode<Label>("CanvasLayer/BottomUi/CoinsCounter/Counter");
    _messagePanel = GetNode<Panel>("CanvasLayer/MessagePanel");
    _message = _messagePanel.GetNode<RichTextLabel>("MessageContainer/Message");
    _messageResponses = _messagePanel.GetNode<HBoxContainer>("MessageContainer/Responses");
    _gridOverlay = GetNode<GridOverlay>("GridOverlay");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _unitsNode = GetNode("Units");
    _terrainNode = GetNode("Terrain");
    _propsNode = GetNode("Props");
    _playerSideUnitsLayer = GetNode<OverlayLayer>("PlayerSideUnitsLayer");
    _enemySideUnitsLayer = GetNode<OverlayLayer>("EnemySideUnitsLayer");
    _selectedCellsLayer = GetNode<OverlayLayer>("SelectedUnitLayer");
    _activeUnitLayer = GetNode<OverlayLayer>("ActiveUnitLayer");
    _targetedCellsLayer = GetNode<OverlayLayer>("TargetedCellsLayer");
    _speedButton = GetNode<MenuButton>("CanvasLayer/BottomUi/SpeedButton");
    _worldUi = GetNode<Panel>("CanvasLayer/ScrollContainer/WorldUi");
    _worldMapButton = GetNode<Button>("CanvasLayer/BottomUi/WorldMapButton");
    _infoGui = GetNode<InfoGui>("CanvasLayer/SelectionUi/HBoxContainer/InfoContainer");
    _playPauseButton = GetNode<Button>("CanvasLayer/BottomUi/PlayPauseButton");
    _surrenderButton = GetNode<Button>("CanvasLayer/BottomUi/SurrenderButton");
    _startButton = GetNode<Button>("CanvasLayer/BottomUi/StartButton");
    _quitButton = GetNode<Button>("CanvasLayer/BottomUi/QuitButton");
    _decksHandler = GetNode<DecksHandler>("CanvasLayer/SelectionUi/HBoxContainer/DecksHandler");
    _levelInfoContainer = GetNode<LevelInfoContainer>("CanvasLayer/SelectionUi/HBoxContainer/LevelInfoContainer");
    _levelsUntilNextUnlock = GetNode<Label>("CanvasLayer/SelectionUi/HBoxContainer/LevelInfoContainer/UnlockInfo/Value");

    // Activity UI
    _gameStats = GetNode<Panel>("CanvasLayer/GameStats");
    _statisticsButton = GetNode<MenuButton>("CanvasLayer/GameStats/VBoxContainer/StatisticsButton");
    _damageDealtStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/DamageDealt/VBoxContainer");
    _damageTakenStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/DamageTaken/VBoxContainer");
    _healingDoneStats = GetNode<VBoxContainer>("CanvasLayer/GameStats/VBoxContainer/HealingDone/VBoxContainer");

    _canvasLayer.Offset = new Vector2I(GlobalConstants.TileSize, GlobalConstants.TileSize);
    _speedPopup = _speedButton.GetPopup();
    _speedPopup.IdPressed += OnSpeedSelected;
    _playPauseButton.Pressed += OnPlayPauseButtonPressed;
    _surrenderButton.Pressed += OnSurrenderButtonPressed;
    _startButton.Pressed += OnStartPlayingButtonPressed;
    _quitButton.Pressed += OnQuitButtonPressed;
    _units = new List<Unit>();
    _unitsToAct = new List<Unit>();
    _removedUnits = new List<Unit>();
    _unitsGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _terrainGrid = new Terrain[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _propsGrid = new Prop[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _playerUnitsGridBeforeLevel = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _globalSignals.GridEntityDied += OnGridEntityDied;
    _globalSignals.GridEntityMoved += OnGridEntityMoved;
    _globalSignals.GridEntitySpawned += OnGridEntitySpawned;
    _globalSignals.SpeedChanged += OnGridEntitySpeedChanged;
    _globalSignals.SizeChanged += OnGridEntitySizeChanged;
    _globalSignals.DamageDealt += OnGridEntityDamageDealt;
    _globalSignals.DamageTaken += OnGridEntityDamageTaken;
    _globalSignals.HealingDone += OnGridEntityHealingDone;
    _globalSignals.HealingReceived += OnGridEntityHealingReceived;
    _globalSignals.SideChanged += OnUnitSideChanged;
    _globalSignals.UnitRemoved += OnUnitRemoved;
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
    _selectedCellsLayer.OutlineColor = Colors.Blue;
    _selectedCellsLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _selectedCellsLayer.OutlineColor = Colors.Blue;
    _selectedCellsLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _activeUnitLayer.OutlineColor = Colors.Yellow;
    _activeUnitLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _targetedCellsLayer.OutlineColor = Colors.Red;

    _levelUnits = new List<UnitInfo>();
    ParseUnitsJson();
    ParseTerrainsJson();
    ParsePropsJson();

    string levelsJson = FileAccess.Open("res://scripts/levels.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(levelsJson);
    _levelsData = (Dictionary)parsed;

    // Start with 2 archers
    _decksHandler.AddUnit(GlobalConstants.UnitsData["archer"]);
    _decksHandler.AddUnit(GlobalConstants.UnitsData["archer"]);


    // World generation
    _worldCenter = new Vector2(
      _maxNodesPerLayer * _ringSpacing,
      _numLayers * _ringSpacing
    );
    AmountOfNodesPerLayerPerSection = new int[_numLayers, _numLevelSections];
    GenerateWorld();
    _gridOverlay.SetUnits(_unitsGrid);
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    // Turn on if there are issues with player losing before all units are dead
    int friendlyUnitCount = 0;
    foreach (Unit unit in _units)
    {
      if (unit.Side)
        friendlyUnitCount++;
    }
    if (friendlyUnitCount != _playerUnitsCount)
      GD.Print("hi");

    if (_paused)
      return;

    if (_acting)
    {
      _actingCooldown -= delta;
      if (_actingCooldown <= 0)
      {
        if (_unitToAct != null)
          _unitToAct.Act(_selectedTargets, _unitsGrid, _terrainGrid, _propsGrid, _unitsToAct, _removedUnits);
        AdvanceToNextUnit();
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
        if (_unitToAct != null)
        {
          _selectedTargets = _unitToAct.GetTargets(_unitsGrid, _terrainGrid, _propsGrid, _unitsToAct, _removedUnits);
          _targetedCellsLayer.ShowCells(_selectedTargets);
          _acting = true;
          if (_selectedTargets.Count > 0)
            _actingCooldown = _actingStartCooldown;
          else
            _actingCooldown = 0; // If there are no targets, skip directly to acting phase
        }
        _targeting = false;
      }
    }
    else if (_playing)
    {
      _turnCooldown -= delta;
      if (_turnCooldown <= 0)
      {
        if (_unitToAct == null)
          TurnEnd();

        _activeUnitLayer.Clear();
        _activeUnit = null!;
        _targetedCellsLayer.Clear();
        if (_unitToAct != null && _unitToAct.CanAct())
        {
          // Show overlay on selected cells
          _activeUnitLayer.ShowCells(_unitToAct.GetOccupiedCells());
          _activeUnit = _unitToAct;
          _targeting = true;
        }
        else
          AdvanceToNextUnit();
      }
    }
  }

  // After a unit acts, find the next one
  private void AdvanceToNextUnit()
  {
    if (_unitToAct == null)
      return;

    int currentIndex = _unitsToAct.IndexOf(_unitToAct);
    _unitToAct = currentIndex + 1 < _unitsToAct.Count ? _unitsToAct[currentIndex + 1] : null;
  }

  private void StartLevel(TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid, UnitInfo[,] enemyUnits, bool loadBeforeLevel = true)
  {
    _worldMapButton.Visible = false;
    _startButton.Visible = true;
    _quitButton.Visible = true;
    if (loadBeforeLevel)
      _decksHandler.LoadBeforeLevel();

    // Load terrain and props
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        TerrainInfo terrainInfo = terrainGrid[x, y];
        if (terrainInfo != null)
        {
          bool side = y >= GlobalConstants.GridSize.Y * 0.5;
          Terrain terrainInstance = GD.Load<PackedScene>(terrainInfo.ScenePath).Instantiate() as Terrain;
          terrainInstance!.Initialize(terrainInfo, new Vector2I(x, y), placed: true);
          _terrainNode.AddChild(terrainInstance);
          if (!side)
            terrainInstance.ModulateSprite(new Color(0.8f, 0.8f, 0.8f));  // Darken the sprite to make it clear it's the enemy side
        }

        if (_propsGrid[x, y] != null)
          continue;

        PropInfo propInfo = propsGrid[x, y];
        if (propInfo != null)
        {
          //bool side = y >= GlobalConstants.GridSize.Y * 0.5;
          Prop propInstance = GD.Load<PackedScene>(propInfo.ScenePath).Instantiate() as Prop;
          propInstance!.Initialize(propInfo, new Vector2I(x, y), placed: true);
          _propsNode.AddChild(propInstance);
        }
      }
    }

    // Load enemy units
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

    // Check whether there are no units on blocking terrain or props
    foreach (Unit unit in _units)
    {
      foreach (Vector2I cell in unit.GetOccupiedCells())
      {
        if ((_terrainGrid[cell.X, cell.Y] != null && _terrainGrid[cell.X, cell.Y].Blocking) ||
          (_propsGrid[cell.X, cell.Y] != null && _propsGrid[cell.X, cell.Y].Blocking))
        {
          Node2D explanationMarkInstance = GD.Load<PackedScene>(_explanationMarkScenePath).Instantiate() as Node2D;
          explanationMarkInstance!.GlobalPosition = GlobalFunctions.CellToGlobalPosition(cell, 1, 1, new Vector2I(0, 0));
          _unitsNode.AddChild(explanationMarkInstance);
          _explanationMarks[cell.X, cell.Y] = explanationMarkInstance!;
        }
      }
    }

    _gridOverlay.SetTerrain(_terrainGrid);
    _gridOverlay.SetProps(_propsGrid);
  }

  private void StartPlaying()
  {
    // Store current units right before the level, so we can load that again if the player loses
    _playerUnitsGridBeforeLevel = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    foreach (Unit unit in _units)
    {
      if (unit.Side)
        _playerUnitsGridBeforeLevel[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y] = unit.GetInfo();
    }

    _startButton.Visible = false;
    _quitButton.Visible = false;
    _playPauseButton.Visible = true;
    _surrenderButton.Visible = true;
    _gridOverlay.SetInteractionLocked(true);

    // Show units side
    foreach (Unit unit in _units)
    {
      if (unit.Side)
        _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
      else
        _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
    }

    // Copy units to create a list of units that are going to act this turn
    _unitsToAct = [.. _units];
    foreach (Unit unit in _unitsToAct)
      unit.GameStart(_unitsGrid, _terrainGrid, _propsGrid, _unitsToAct);
    _playing = true;

    // Set unit to act to first unit
    _unitToAct = _unitsToAct.FirstOrDefault();

    // Immediately win or lose when you or the enemy has no units
    if (_playerUnitsCount == 0)
      Lose();
    else if (_enemyUnitsCount == 0)
      Win();
  }

  private Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> LoadLevel(string levelId)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    TerrainInfo[,] terrainGrid = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    PropInfo[,] propsGrid = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
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

    return new Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]>(terrainGrid, propsGrid, mainCellsUnitGrid);
  }

  private Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> LoadRandomLevel(int difficulty, int levelSection)
  {
    // Only place a limited amount of blocking terrains and props. So there is enough room for units.
    int expectedUserUnitSlots = difficulty;
    int requiredFreeTilesUser = expectedUserUnitSlots + 10;
    int expectedEnemyUnitSlots = difficulty + (difficulty / 2);
    if (difficulty > 3)
      expectedEnemyUnitSlots += difficulty;  // Ramp up difficulty even more after a while.
    int requiredFreeTilesEnemy = expectedEnemyUnitSlots + 10;
    int halfTiles = GlobalConstants.GridSize.X * Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    int maxBlockingUser = halfTiles - requiredFreeTilesUser;
    int maxBlockingEnemy = halfTiles - requiredFreeTilesEnemy;

    TerrainInfo[,] terrain = LoadRandomLevelTerrain(difficulty, maxBlockingUser, maxBlockingEnemy, levelSection);
    PropInfo[,] props = LoadRandomLevelProps(difficulty, maxBlockingUser, maxBlockingEnemy, terrain, levelSection);
    UnitInfo[,] units = LoadRandomLevelUnits(difficulty, terrain, props, levelSection, expectedEnemyUnitSlots);
    return new Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]>(terrain, props, units);
  }

  private TerrainInfo[,] LoadRandomLevelTerrain(int difficulty, int maxBlockingUser, int maxBlockingEnemy, int levelSection)
  {
    TerrainInfo[,] terrain = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    List<string> nonBlockingTerrains = ["grass", "white_floor_tiles", "stone_floor", "dark_grass", "gray_floor_tiles"]; // TODO move
    List<string> blockingTerrains1 = ["water", "holy_water", "oil", "poisoned_water", "blood"]; // TODO move
    List<string> blockingTerrains2 = ["mountain", "green_mountain", "hole_in_floor", "mud", "lower_gray_floor_tiles"]; // TODO move

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        terrain[x, y] = GlobalConstants.TerrainsData[nonBlockingTerrains[levelSection]];

    FastNoiseLite mountainNoise = new();
    mountainNoise.Seed = _rng.Next();
    mountainNoise.Frequency = 0.035f;
    mountainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

    FastNoiseLite waterNoise = new();
    waterNoise.Seed = _rng.Next();
    waterNoise.Frequency = 0.035f;
    waterNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

    int topHalfBlocking = 0;
    int bottomHalfBlocking = 0;
    int midY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        bool isTopHalf = y < midY;
        if ((isTopHalf && topHalfBlocking >= maxBlockingEnemy) ||
          (!isTopHalf && bottomHalfBlocking >= maxBlockingUser))
          continue;

        float mountainVal = mountainNoise.GetNoise2D(x, y);
        float waterVal = waterNoise.GetNoise2D(x, y);

        if (mountainVal > 0.40f)
        {
          terrain[x, y] = GlobalConstants.TerrainsData[blockingTerrains1[levelSection]];
          if (isTopHalf) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
        else if (waterVal > 0.40f)
        {
          terrain[x, y] = GlobalConstants.TerrainsData[blockingTerrains2[levelSection]];
          if (isTopHalf) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
      }
    }

    return terrain;
  }

  private PropInfo[,] LoadRandomLevelProps(int difficulty, int maxBlockingUser, int maxBlockingEnemy, TerrainInfo[,] terrainGrid, int levelSection)
  {
    PropInfo[,] props = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    List<string> movableProps = ["bush", "vase", "broken_turret", "dark_bush", "demonic_vase"];
    List<string> damagableProps = ["rock", "white_statue", "rock", "gravestone", "statue"];
    List<string> specialProps1 = ["fire", "fire", "fire", "fire", "fire"];
    List<string> specialProps2 = ["wild_fire", "wild_fire", "wild_fire", "wild_fire", "wild_fire"];

    // Count already blocking terrain per half
    int midY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    int topHalfBlocking = 0;
    int bottomHalfBlocking = 0;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        if (terrainGrid[x, y] != null && terrainGrid[x, y].Blocking)
        {
          if (y < midY) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
      }
    }

    bool CanPlaceBlocking(int x, int y)
    {
      bool isTopHalf = y < midY;
      if ((isTopHalf && topHalfBlocking >= maxBlockingEnemy) ||
        (!isTopHalf && bottomHalfBlocking >= maxBlockingUser))
        return false;
      else
        return true;
    }

    void PlaceBlocking(int x, int y)
    {
      if (y < midY) topHalfBlocking++;
      else bottomHalfBlocking++;
    }

    // Movable prop
    int rockCount = _rng.Next(0, 4);
    PropInfo propInfo = GlobalConstants.PropsData[movableProps[levelSection]];
    for (int i = 0; i < rockCount; i++)
    {
      List<Vector2I> candidates = new();
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
          if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), _unitsGrid, terrainGrid, props) &&
              CanPlaceBlocking(x, y))
            candidates.Add(new Vector2I(x, y));

      if (candidates.Count == 0) break;
      Vector2I cell = candidates[_rng.Next(candidates.Count)];
      props[cell.X, cell.Y] = propInfo;
      PlaceBlocking(cell.X, cell.Y);
    }

    // Damagable prop
    int bushCount = _rng.Next(0, 4);
    propInfo = GlobalConstants.PropsData[damagableProps[levelSection]];
    for (int i = 0; i < bushCount; i++)
    {
      List<Vector2I> candidates = new();
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
          // There are no units yet, so pass empty grid for units
          if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, props) &&
              CanPlaceBlocking(x, y))
            candidates.Add(new Vector2I(x, y));

      if (candidates.Count == 0) break;
      Vector2I cell = candidates[_rng.Next(candidates.Count)];
      props[cell.X, cell.Y] = propInfo;
      PlaceBlocking(cell.X, cell.Y);
    }

    // Special prop
    bool spawnFire = _rng.NextDouble() < 0.25;
    propInfo = GlobalConstants.PropsData[specialProps1[levelSection]];
    if (spawnFire)
    {
      List<Vector2I> cluster = SpawnClusterOfPropType(propInfo, terrainGrid, props, _rng.Next(2, 6));
      foreach (Vector2I cell in cluster)
        props[cell.X, cell.Y] = propInfo;
    }

    // Special prop 2
    bool spawnWildFire = _rng.NextDouble() < 0.15;
    propInfo = GlobalConstants.PropsData[specialProps2[levelSection]];
    if (spawnWildFire)
    {
      List<Vector2I> cluster = SpawnClusterOfPropType(propInfo, terrainGrid, props, _rng.Next(2, 6));
      foreach (Vector2I cell in cluster)
        props[cell.X, cell.Y] = propInfo;
    }

    return props;
  }

  private List<Vector2I> SpawnClusterOfPropType(PropInfo propInfo, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid, int clusterSize, Func<int, int, bool>? extraCheck = null)
  {
    List<Vector2I> candidates = new();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        // There are no units yet, so pass empty grid for units
        if (GlobalFunctions.CanSpawnProp(propInfo, new Vector2I(x, y), new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, propsGrid) &&
            (extraCheck == null || extraCheck(x, y)))
          candidates.Add(new Vector2I(x, y));

    if (candidates.Count == 0) return [];

    Vector2I origin = candidates[_rng.Next(candidates.Count)];
    List<Vector2I> cluster = new() { origin };
    List<Vector2I> frontier = new() { origin };

    while (cluster.Count < clusterSize && frontier.Count > 0)
    {
      Vector2I current = frontier[_rng.Next(frontier.Count)];
      frontier.Remove(current);

      Vector2I[] neighbors = {
            new(current.X + 1, current.Y),
            new(current.X - 1, current.Y),
            new(current.X, current.Y + 1),
            new(current.X, current.Y - 1),
        };

      foreach (Vector2I neighbor in neighbors)
      {
        if (cluster.Count >= clusterSize) break;
        if (neighbor.X < 0 || neighbor.Y < 0 ||
            neighbor.X >= GlobalConstants.GridSize.X ||
            neighbor.Y >= GlobalConstants.GridSize.Y) continue;
        if (cluster.Contains(neighbor)) continue;
        // There are no units yet, so pass empty grid for units
        if (!GlobalFunctions.CanSpawnProp(propInfo, neighbor, new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y], terrainGrid, propsGrid)) continue;
        if (extraCheck != null && !extraCheck(neighbor.X, neighbor.Y)) continue;

        cluster.Add(neighbor);
        frontier.Add(neighbor);
      }
    }

    return cluster;
  }

  private UnitInfo[,] LoadRandomLevelUnits(int difficulty, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid, int levelSection, int expectedEnemyUnitSlots)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    List<string> dominantUnitTypes = ["primal", "holy", "machinery", "rotten", "demonic"]; // TODO move
    string dominantType = dominantUnitTypes[levelSection];

    int budget = expectedEnemyUnitSlots;
    int maxStage = GetMaxStageForDifficulty(difficulty);

    // Build unit pools
    List<UnitInfo> dominantPool = GlobalConstants.UnitsData.Values
        .Where(u => u.Types.Contains(dominantType) && u.Stage <= maxStage)
        .ToList();

    List<UnitInfo> typelessPool = GlobalConstants.UnitsData.Values
        .Where(u => u.Types.Count == 0 && u.Stage <= maxStage)
        .ToList();

    List<UnitInfo> otherPool = GlobalConstants.UnitsData.Values
        .Where(u => !u.Types.Contains(dominantType) && u.Types.Count > 0 && u.Stage <= maxStage)
        .ToList();

    RandomNumberGenerator rng = new();
    rng.Randomize();

    while (budget > 0)
    {
      // It is possible for a randomly chosen unit to not have any possible positions on the grid due to terrain or props/
      // If that happens it can only be because of multi celled units, because there are always enough non-blocking tiles on each level
      // Therefore we simply retry selecting random units until we find one that does have a possible location.
      UnitInfo unitInfo = GlobalConstants.UnitsData["turret"];
      List<Vector2I> possiblePositions = new List<Vector2I>();
      int cost = unitInfo.OccupiedCells.Count;
      int nrOfTries = 0;
      while (possiblePositions.Count == 0 && nrOfTries < 10)
      {
        nrOfTries++;

        // Pick pool based on probability
        double roll = rng.Randf();
        List<UnitInfo> pool;
        if (roll < 0.75f)
          pool = dominantPool;
        else if (roll < 0.90f)
          pool = typelessPool;
        else
          pool = otherPool;

        if (pool.Count == 0)
          pool = typelessPool.Count > 0 ? typelessPool : GlobalConstants.UnitsData.Values.ToList();

        // Pick random unit from pool within budget
        List<UnitInfo> affordableUnits = pool
            .Where(u => u.OccupiedCells.Count <= budget)
            .ToList();
        if (affordableUnits.Count == 0)
          continue;
        unitInfo = affordableUnits[rng.RandiRange(0, affordableUnits.Count - 1)];
        cost = unitInfo.OccupiedCells.Count;

        // Get random position for the unit, ensuring it fits within the grid and doesn't overlap with existing units
        possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, false);
      }

      // We failed finding a different unit
      if (nrOfTries >= 10)
      {
        unitInfo = GlobalConstants.UnitsData["turret"];
        possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, false);
        cost = unitInfo.OccupiedCells.Count;
        GD.Print("Unit placement failed. Placing turret instead.");
      }

      // Determine optimal positions for this unit and pick the semi-best one
      Vector2I cellPos = possiblePositions
          .OrderBy(_ => rng.Randi())
          .OrderByDescending(p => unitInfo.ScorePlacement(p, unitInfo, unitGrid, terrainGrid, propsGrid) + rng.RandiRange(0, 5))
          .First();

      foreach (Vector2I cell in unitInfo.OccupiedCells)
      {
        unitGrid[cellPos.X + cell.X, cellPos.Y + cell.Y] = unitInfo;
      }
      mainCellsUnitGrid[cellPos.X, cellPos.Y] = unitInfo;

      budget -= cost; // TODO AKN unitInfo.Cost;
    }

    return mainCellsUnitGrid;
  }

  private void Reset(bool reloadLevel = false)
  {
    // Reset units
    while (_units.Count > 0)
      _units[_units.Count - 1].Remove();
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
    _gridOverlay.SetUnits(_unitsGrid);

    // Reset terrain
    foreach (Terrain terrain in _terrainGrid)
      if (IsInstanceValid(terrain))
        terrain.QueueFree();
    _terrainGrid = new Terrain[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _gridOverlay.SetTerrain(_terrainGrid);

    // Reset props
    foreach (Prop prop in _propsGrid)
      if (IsInstanceValid(prop))
        prop.QueueFree();
    _propsGrid = new Prop[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    _gridOverlay.SetProps(_propsGrid);

    foreach (Node2D explanationMark in _explanationMarks)
    {
      if (explanationMark != null && IsInstanceValid(explanationMark))
        explanationMark.QueueFree();
    }
    _explanationMarks = new Node2D[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

    _unitToAct = null;
    _turn = 0;
    _turnCounter.Text = _turn.ToString();
    _turnCooldown = _turnStartCooldown;
    ClearMessagePanel();
    _messagePanel.Hide();
    ResetGameStats();
    _levelCounter.Text = _level.ToString();
    _activeUnitLayer.Clear();
    _targetedCellsLayer.Clear();
    _turnEndDamage = 1;
    _levelUnits.Clear();
    _playerSideUnitsLayer.Clear();
    _enemySideUnitsLayer.Clear();
    if (reloadLevel)
      _decksHandler.ReloadAfterLevel(_playerUnitsGridBeforeLevel);
    else
      _decksHandler.LoadAfterLevel();
    _enemyUnitsCount = 0;

    _gridOverlay.SetInteractionLocked(false);
    _worldMapButton.Visible = true;
    _playPauseButton.Visible = false;
    _surrenderButton.Visible = false;
    _startButton.Visible = false;
    _quitButton.Visible = false;
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

    if (_activeLevel.Boss && !_activeLevel.Completed)
      _bossLevelsDefeated++;

    if (_bossLevelsDefeated == 5)
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
      if (!_activeLevel.Completed)
      {
        _activeLevel.Completed = true;
        _completedLevels++;
        _levelsUntilNextUnlock.Text = (_completedLevels % 5).ToString() + "/5";

        // Increase max unit slots every 5 completed levels
        if (_completedLevels % 5 == 0)
          _gridOverlay.IncreaseUnitCount(1);
      }

      // Set button to green to indicate it has been completed
      Color color = Colors.LightGreen;
      StyleBoxFlat style = GetRewardButtonStyle(color);
      StyleBoxFlat stylePressed = GetRewardButtonStyle(color.Darkened(0.2f));
      StyleBoxFlat styleHover = GetRewardButtonStyle(color.Lightened(0.2f));
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("normal", style);
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("pressed", stylePressed);
      _levelButtons[_activeLevel.Id].AddThemeStyleboxOverride("hover", styleHover);

      foreach (string nextNodeId in _activeLevel.NextNodes)
      {
        if (Levels.ContainsKey(nextNodeId) && !Levels[nextNodeId].Unlocked)
        {
          Levels[nextNodeId].Unlocked = true;
          _levelButtons[nextNodeId].Visible = true;
        }

        // Connect lines to next levels
        LevelInfo nextLevel = Levels[nextNodeId];

        Vector2 fromCenter = GetLevelButtonPosition(_activeLevel.Layer, _activeLevel.LayerIndex, AmountOfNodesPerLayerPerSection[_activeLevel.Layer, _activeLevel.LevelSection], _activeLevel.LevelSection);
        Vector2 toCenter = GetLevelButtonPosition(nextLevel.Layer, nextLevel.LayerIndex, AmountOfNodesPerLayerPerSection[nextLevel.Layer, nextLevel.LevelSection], nextLevel.LevelSection);

        Line2D line = new();
        line.Points = new Vector2[] { fromCenter, toCenter };
        line.Width = 2.0f;
        line.DefaultColor = new Color(1, 1, 1, 0.8f);
        _worldUi.AddChild(line);
        _worldUi.MoveChild(line, 0); // Make sure lines appear behind buttons
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

  private void OnGridEntityDied(GridEntity gridEntity)
  {
    gridEntity.DeathRattle(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
    if (gridEntity is Unit unit)
    {
      if (unit == _unitToAct)
        AdvanceToNextUnit();

      RemoveUnit(unit);
      _removedUnits.Add(unit);

      if (_enemyUnitsCount == 0)
        Win();
      else if (_playerUnitsCount == 0)
        Lose();
    }
    else if (gridEntity is Terrain terrain)
    {
      foreach (Vector2I cell in terrain.GetOccupiedCells())
        _terrainGrid[cell.X, cell.Y] = null!;
    }
    else if (gridEntity is Prop prop)
    {
      foreach (Vector2I cell in prop.GetOccupiedCells())
        _propsGrid[cell.X, cell.Y] = null!;
    }
  }

  private void OnUnitRemoved(Unit unit)
  {
    RemoveUnit(unit);
    foreach (Vector2I cell in unit.GetOccupiedCells())
    {
      Node2D explanationMark = _explanationMarks[cell.X, cell.Y];
      if (explanationMark != null && IsInstanceValid(explanationMark))
      {
        _explanationMarks[cell.X, cell.Y].QueueFree();
        _explanationMarks[cell.X, cell.Y] = null!;
      }
    }
  }

  private void RemoveUnit(Unit unit)
  {
    if (_infoGui.GetSelectedUnit() == unit)
    {
      _infoGui.Reset();
      _selectedCellsLayer.Clear();
    }

    if (_playing)
    {
      // Update unit side layers
      if (unit.Side)
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
    if (unit.Side ^ unit.SwitchedSides)
      _playerUnitsCount--;
    else
      _enemyUnitsCount--;
  }

  private void OnGridEntityMoved(GridEntity gridEntity, Vector2I oldCell, bool playing)
  {
    if (gridEntity is Unit unit)
    {
      if (_infoGui.GetSelectedUnit() == unit)
      {
        _selectedCellsLayer.Clear();
        _selectedCellsLayer.ShowCells(unit.GetOccupiedCells());
        _infoGui.SetSelectedInfo(unit, _terrainGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y], 
          _propsGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y], unit.OccupiedMainCell);
      }
      if (_activeUnit == unit)
      {
        _activeUnitLayer.Clear();
        _activeUnitLayer.ShowCells(unit.GetOccupiedCells());
      }

      List<Vector2I> removedCells = new List<Vector2I>();
      foreach (Vector2I cell in unit.OccupiedCells)
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
        if (unit.Side)
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
      else
      {
        foreach (Vector2I cell in removedCells)
        {
          Node2D explanationMark = _explanationMarks[cell.X, cell.Y];
          if (explanationMark != null && IsInstanceValid(explanationMark))
          {
            _explanationMarks[cell.X, cell.Y].QueueFree();
            _explanationMarks[cell.X, cell.Y] = null!;
          }
        }
      }

      if (_playing)
        SortUnits(_unitsToAct);
      else
        SortUnits(_units);
    }
    else if (gridEntity is Terrain terrain)
    {
      List<Vector2I> removedCells = new List<Vector2I>();
      foreach (Vector2I cell in terrain.OccupiedCells)
      {
        Vector2I removedCell = new Vector2I(oldCell.X + cell.X, oldCell.Y + cell.Y);
        if (_terrainGrid[removedCell.X, removedCell.Y] == terrain)  // Swapping can make it so a new terrain is on this terrain's old position
          _terrainGrid[removedCell.X, removedCell.Y] = null!;
        removedCells.Add(removedCell);
      }
      foreach (Vector2I cell in terrain.GetOccupiedCells())
      {
        _terrainGrid[cell.X, cell.Y] = terrain;
      }

      if (_infoGui.GetSelectedCell() == oldCell)
        _infoGui.SetSelectedInfo(_unitsGrid[oldCell.X, oldCell.Y], _terrainGrid[oldCell.X, oldCell.Y],
          _propsGrid[oldCell.X, oldCell.Y], oldCell);
    }
    else if (gridEntity is Prop prop)
    {
      List<Vector2I> removedCells = new List<Vector2I>();
      foreach (Vector2I cell in prop.OccupiedCells)
      {
        Vector2I removedCell = new Vector2I(oldCell.X + cell.X, oldCell.Y + cell.Y);
        if (_propsGrid[removedCell.X, removedCell.Y] == prop)  // Swapping can make it so a new terrain is on this terrain's old position
          _propsGrid[removedCell.X, removedCell.Y] = null!;
        removedCells.Add(removedCell);
      }
      foreach (Vector2I cell in prop.GetOccupiedCells())
      {
        _propsGrid[cell.X, cell.Y] = prop;
      }

      if (_infoGui.GetSelectedCell() == oldCell)
        _infoGui.SetSelectedInfo(_unitsGrid[oldCell.X, oldCell.Y], _terrainGrid[oldCell.X, oldCell.Y],
          _propsGrid[oldCell.X, oldCell.Y], oldCell);
    }
  }

  private void OnGridEntitySpawned(GridEntity gridEntity, bool playing)
  {
    if (gridEntity is Unit unit)
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
        if (unit.Side)
          _playerSideUnitsLayer.AddCells(unit.GetOccupiedCells());
        else
          _enemySideUnitsLayer.AddCells(unit.GetOccupiedCells());
      }

      if (unit.Side)
        _playerUnitsCount++;
      else
        _enemyUnitsCount++;

      if (_playing)
        SortUnits(_unitsToAct);
      else
        SortUnits(_units);
    }
    else if (gridEntity is Terrain terrain)
    {
      foreach (Vector2I cell in terrain.GetOccupiedCells())
      {
        _terrainGrid[cell.X, cell.Y] = terrain;
      }
    }
    else if (gridEntity is Prop prop)
    {
      foreach (Vector2I cell in prop.GetOccupiedCells())
      {
        _propsGrid[cell.X, cell.Y] = prop;
      }
    }

    if (_infoGui.GetSelectedCell() == gridEntity.OccupiedMainCell)
      _infoGui.SetSelectedInfo(_unitsGrid[gridEntity.OccupiedMainCell.X, gridEntity.OccupiedMainCell.Y], _terrainGrid[gridEntity.OccupiedMainCell.X, gridEntity.OccupiedMainCell.Y],
        _propsGrid[gridEntity.OccupiedMainCell.X, gridEntity.OccupiedMainCell.Y], gridEntity.OccupiedMainCell);
  }

  private void OnGridEntitySizeChanged(GridEntity gridEntity, Godot.Collections.Array<Vector2I> oldOccupiedCells)
  {
    if (gridEntity is Unit unit)
    {
      if (_infoGui.GetSelectedUnit() == unit)
      {
        _selectedCellsLayer.Clear();
        _selectedCellsLayer.ShowCells(unit.GetOccupiedCells());
        _infoGui.SetSelectedInfo(unit, _terrainGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y],
          _propsGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y], unit.OccupiedMainCell);
      }
      if (_activeUnit == unit)
      {
        _activeUnitLayer.Clear();
        _activeUnitLayer.ShowCells(unit.GetOccupiedCells());
      }

      // Update unit side layers
      if (unit.Side)
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
    else if (gridEntity is Terrain terrain)
    {
      foreach (Vector2I cell in oldOccupiedCells)
      {
        _terrainGrid[cell.X, cell.Y] = null!;
      }

      foreach (Vector2I cell in terrain.GetOccupiedCells())
      {
        _terrainGrid[cell.X, cell.Y] = terrain;
      }
    }
    else if (gridEntity is Prop prop)
    {
      foreach (Vector2I cell in oldOccupiedCells)
      {
        _propsGrid[cell.X, cell.Y] = null!;
      }

      foreach (Vector2I cell in prop.GetOccupiedCells())
      {
        _propsGrid[cell.X, cell.Y] = prop;
      }
    }
  }

  private void OnGridEntitySpeedChanged(GridEntity gridEntity)
  {
    if (gridEntity is Unit unit)
    {
      if (_playing)
        SortUnits(_unitsToAct);
      else
        SortUnits(_units);
    }
  }

  private void OnUnitSideChanged(Unit unit)
  {
    // Update unit side layers
    if (unit.Side)
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

  private void OnGridEntityDamageDealt(GridEntity gridEntity, int amount)
  {
    if (gridEntity is Unit unit)
      _unitsActivity[unit].DamageDealth += amount;
  }

  private void OnGridEntityDamageTaken(GridEntity gridEntity, int amount)
  {
    if (gridEntity is Unit unit)
      _unitsActivity[unit].DamageTaken += amount;
  }

  private void OnGridEntityHealingDone(GridEntity gridEntity, int amount)
  {
    if (gridEntity is Unit unit)
      _unitsActivity[unit].HealingDone += amount;
  }

  private void OnGridEntityHealingReceived(GridEntity gridEntity, int amount)
  {
    if (gridEntity is Unit unit)
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
    _selectedReward = null;
    Reset();
  }

  private void OnReturnButtonPressed()
  {
    Reset(reloadLevel: true);
    StartLevel(_activeLevel.Terrains[_activeGauntletLevelIndex], _activeLevel.Props[_activeGauntletLevelIndex], _activeLevel.Units[_activeGauntletLevelIndex], loadBeforeLevel: false);
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
      int stage = (int)unitData["stage"];

      Godot.Collections.Array cells = (Godot.Collections.Array)unitData["cells"];
      List<Vector2I> occupiedCells = new();
      foreach (Godot.Collections.Array cell in cells)
      {
        int x = (int)cell[0];
        int y = (int)cell[1];
        occupiedCells.Add(new Vector2I(x, y));
      }

      GlobalConstants.UnitsData[unitId] = new UnitInfo(unitId, displayName, texture, scenePath, occupiedCells, 
        cost, health, health, damage, armor, speed, cooldown, cooldown, description, rarity, types, stage);

      // Link unit info score placement function to unit specific class function using reflection
      string fileName = scenePath.GetFile().GetBaseName();
      string className = ToPascalCase(fileName);
      Type type = Type.GetType(className);
      if (type == null) continue;
      var method = type.GetMethod("ScorePlacement", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
      if (method == null) continue;
      GlobalConstants.UnitsData[unitId].ScorePlacement = (pos, unitInfo, unitsGrid, terrainGrid, propsGrid) => (int)method.Invoke(null, new object[] { pos, unitInfo, unitsGrid, terrainGrid, propsGrid });
    }
  }

  string ToPascalCase(string snakeCase)
  {
    return string.Concat(snakeCase.Split('_')
        .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
  }

  private void ParseTerrainsJson()
  {
    string terrainJson = FileAccess.Open("res://scripts/terrains/terrains.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(terrainJson);
    Dictionary terrainsData = (Dictionary)parsed;
    foreach (KeyValuePair<Variant, Variant> entry in terrainsData)
    {
      string terrainId = entry.Key.ToString();
      Dictionary terrainData = (Dictionary)entry.Value;

      string displayName = (string)terrainData["display_name"];
      string texturePath = (string)terrainData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)terrainData["scene"];
      string description = (string)terrainData["description"];
      string rarity = (string)terrainData["rarity"];
      bool blocking = (bool)terrainData["blocking"];
      List<string> types = terrainData["types"].AsGodotArray().Select(t => t.AsString()).ToList();

      Godot.Collections.Array cells = (Godot.Collections.Array)terrainData["cells"];
      List<Vector2I> occupiedCells = new();
      foreach (Godot.Collections.Array cell in cells)
      {
        int x = (int)cell[0];
        int y = (int)cell[1];
        occupiedCells.Add(new Vector2I(x, y));
      }

      GlobalConstants.TerrainsData[terrainId] = new TerrainInfo(terrainId, displayName, texture, scenePath, 
        occupiedCells, description, rarity, blocking, types);
    }
  }

  private void ParsePropsJson()
  {
    string propsJson = FileAccess.Open("res://scripts/props/props.json", FileAccess.ModeFlags.Read).GetAsText();
    Variant parsed = Json.ParseString(propsJson);
    Dictionary propsData = (Dictionary)parsed;
    foreach (KeyValuePair<Variant, Variant> entry in propsData)
    {
      string propsId = entry.Key.ToString();
      Dictionary propData = (Dictionary)entry.Value;

      string displayName = (string)propData["display_name"];
      string texturePath = (string)propData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)propData["scene"];
      int health = (int)propData["health"];
      int damage = (int)propData["damage"];
      int armor = (int)propData["armor"];
      string description = (string)propData["description"];
      string rarity = (string)propData["rarity"];
      bool damagable = (bool)propData["damagable"];
      bool movable = (bool)propData["movable"];
      bool blocking = (bool)propData["blocking"];
      List<string> types = propData["types"].AsGodotArray().Select(t => t.AsString()).ToList();

      Godot.Collections.Array cells = (Godot.Collections.Array)propData["cells"];
      List<Vector2I> occupiedCells = new();
      foreach (Godot.Collections.Array cell in cells)
      {
        int x = (int)cell[0];
        int y = (int)cell[1];
        occupiedCells.Add(new Vector2I(x, y));
      }

      GlobalConstants.PropsData[propsId] = new PropInfo(propsId, displayName, texture, scenePath,
        occupiedCells, health, health, damage, armor, description, rarity, damagable, movable, blocking, types);
    }
  }

  int GetMaxStageForDifficulty(int difficulty)
  {
    if (difficulty < 2) return 0;
    if (difficulty < 4) return 1;
    if (difficulty < 6) return 2;
    if (difficulty < 8) return 3;
    if (difficulty < 10) return 4;
    return 5;
  }

  void PopulateLevels()
  {
    // Layer 0: single start node
    string startId = "Start";
    Levels[startId] = CreateLevelInfo(startId, 0, 0, false, 0);
    AmountOfNodesPerLayerPerSection[0, 0] = 1;
    AmountOfNodesPerLayerPerSection[0, 1] = 1;
    AmountOfNodesPerLayerPerSection[0, 2] = 1;
    AmountOfNodesPerLayerPerSection[0, 3] = 1;
    AmountOfNodesPerLayerPerSection[0, 4] = 1;

    for (int q = 0; q < _numLevelSections; q++)
    {
      List<List<LevelInfo>> sectionLayers = new();

      // Layer 1: one entry node per section
      string entryId = $"Q{q}_L1_N0";
      Levels[entryId] = CreateLevelInfo(entryId, 1, 0, false, q);
      Levels[startId].NextNodes.Add(entryId);
      sectionLayers.Add(new List<LevelInfo> { Levels[entryId] });
      AmountOfNodesPerLayerPerSection[1, q] = 1;

      // Remaining layers expand like a cone
      for (int layer = 2; layer < _numLayers; layer++)
      {
        bool isBoss = layer % (_numLayers - 1) == 0;
        int nodesInLayer = 0;
        if (layer < (_numLayers * 0.75f - 1))
          nodesInLayer = AmountOfNodesPerLayerPerSection[layer - 1, q] + 2;
        else if (layer < (_numLayers - 1)) // After a while the layers converge to one boss layer
          nodesInLayer = AmountOfNodesPerLayerPerSection[layer - 1, q] - 3;
        else
          nodesInLayer = 1;

        if (nodesInLayer > _maxNodesInLayer)
          _maxNodesInLayer = nodesInLayer;

        List<LevelInfo> layerNodes = new();

        for (int node = 0; node < nodesInLayer; node++)
        {
          string nodeId = isBoss
              ? $"Q{q}_L{layer}_Boss"
              : $"Q{q}_L{layer}_N{node}";
          Levels[nodeId] = CreateLevelInfo(nodeId, layer, node, isBoss, q);
          layerNodes.Add(Levels[nodeId]);
        }

        ConnectRings(sectionLayers[^1], layerNodes, isBoss);
        sectionLayers.Add(layerNodes);
        AmountOfNodesPerLayerPerSection[layer, q] = nodesInLayer;
      }
    }
  }

  void ConnectRings(List<LevelInfo> inner, List<LevelInfo> outer, bool isBoss)
  {
    if (isBoss)
    {
      foreach (LevelInfo node in inner)
        node.NextNodes.Add(outer[0].Id);
      return;
    }

    int innerCount = inner.Count;
    int outerCount = outer.Count;

    // Pass 1: map each inner node to its proportional outer node
    for (int i = 0; i < innerCount; i++)
    {
      int mapped = Mathf.Clamp(
          Mathf.RoundToInt((float)i / (innerCount == 1 ? 1 : innerCount - 1) * (outerCount - 1)),
          0, outerCount - 1);
      if (!inner[i].NextNodes.Contains(outer[mapped].Id))
        inner[i].NextNodes.Add(outer[mapped].Id);
    }

    // Ensure every outer node has at least one incoming connection
    for (int j = 0; j < outerCount; j++)
    {
      if (!inner.Any(n => n.NextNodes.Contains(outer[j].Id)))
      {
        int closest = Mathf.Clamp(
            Mathf.RoundToInt((float)j / (outerCount == 1 ? 1 : outerCount - 1) * (innerCount - 1)),
            0, innerCount - 1);
        inner[closest].NextNodes.Add(outer[j].Id);
      }
    }

    // Pass 2: add extra adjacent connections, using actual connected ranges
    // to prevent crossings
    for (int i = 0; i < innerCount; i++)
    {
      if (_rng.Next(0, 3) == 0) continue; // skip extra connections sometimes

      List<int> connected = Enumerable.Range(0, outerCount)
          .Where(j => inner[i].NextNodes.Contains(outer[j].Id))
          .ToList();

      int leftmost = connected.Min();
      int rightmost = connected.Max();

      // The strict bounds: never go past what neighboring inner nodes connect to
      int strictLeft = i > 0
          ? Enumerable.Range(0, outerCount)
              .Where(j => inner[i - 1].NextNodes.Contains(outer[j].Id))
              .DefaultIfEmpty(0).Max() // left neighbor's rightmost
          : 0;

      int strictRight = i < innerCount - 1
          ? Enumerable.Range(0, outerCount)
              .Where(j => inner[i + 1].NextNodes.Contains(outer[j].Id))
              .DefaultIfEmpty(outerCount - 1).Min() // right neighbor's leftmost
          : outerCount - 1;

      // Try extending left
      if (leftmost - 1 >= 0 && leftmost - 1 >= strictLeft && _rng.NextDouble() < 0.5)
        inner[i].NextNodes.Add(outer[leftmost - 1].Id);

      // Try extending right
      if (rightmost + 1 <= outerCount - 1 && rightmost + 1 <= strictRight && _rng.NextDouble() < 0.5)
        inner[i].NextNodes.Add(outer[rightmost + 1].Id);
    }
  }

  private LevelInfo CreateLevelInfo(string nodeId, int layer, int node, bool isBoss, int section)
  {
    if (isBoss)
    {
      string levelId;
      string nodeName;

      if (section == 0)
      {
        nodeName = "Earth Mother";
        levelId = "earth_mother";
      }
      else if (section == 1)
      {
        nodeName = "Michael";
        levelId = "michael";
      }
      else if (section == 2)
      {
        nodeName = "Nuke";
        levelId = "nuke";
      }
      else if (section == 3)
      {
        nodeName = "Blob";
        levelId = "blob";
      }
      else
      {
        nodeName = "Lucifer";
        levelId = "lucifer";
      }

      Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> level = LoadLevel(levelId);
      return new LevelInfo(
          id: nodeId,
          name: nodeName,
          completed: false,
          unlocked: layer == 0,
          layer: layer,
          layerIndex: node,
          boss: true,
          gauntlet: false,
          rewards: [],
          coinsReward: _coinsPerWin,
          section,
          nextNodes: new List<string>(),
          terrains: [level.Item1],
          props: [level.Item2],
          units: [level.Item3]
      );
    }

    if (layer > 4 && _rng.NextDouble() < 0.2)
    {
      int numGauntletLevels = 4;
      List<TerrainInfo[,]> gauntletTerrains = new();
      List<PropInfo[,]> gauntletProps = new();
      List<UnitInfo[,]> gauntletUnits = new();

      for (int i = 0; i < numGauntletLevels; i++)
      {
        Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> level = LoadRandomLevel(layer, section);
        gauntletTerrains.Add(level.Item1);
        gauntletProps.Add(level.Item2);
        gauntletUnits.Add(level.Item3);
      }

      List<UnitInfo> rewardUnits = gauntletUnits
          .SelectMany(grid => grid.Cast<UnitInfo>())
          .Where(u => u != null)
          .DistinctBy(u => u.Id)
          .OrderBy(_ => _rng.Next())
          .Take(3)
          .ToList();

      return new LevelInfo(
          id: nodeId,
          name: $"{_levelSections[section]}\nGauntlet {node}",
          completed: false,
          unlocked: layer == 0,
          layer: layer,
          layerIndex: node,
          boss: false,
          gauntlet: true,
          rewards: rewardUnits,
          coinsReward: _coinsPerWin * 5,
          section,
          nextNodes: new List<string>(),
          terrains: gauntletTerrains,
          props: gauntletProps,
          units: gauntletUnits
      );
    }

    string name = layer == 0 ? "Start" : $"{_levelSections[section]}\nLevel {node}";
    Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> randomLevel = LoadRandomLevel(layer + 1, section);
    return new LevelInfo(
        id: nodeId,
        name: name,
        completed: false,
        unlocked: layer == 0,
        layer: layer,
        layerIndex: node,
        boss: false,
        gauntlet: false,
        rewards: [],
        coinsReward: _coinsPerWin,
        section,
        nextNodes: new List<string>(),
        terrains: [randomLevel.Item1],
        props: [randomLevel.Item2],
        units: [randomLevel.Item3]
    );
  }


  public void GenerateWorldUi()
  {
    float diameter = _numLayers * _ringSpacing * 2 + 300;
    _worldUi.CustomMinimumSize = new Vector2(diameter, diameter);
    _worldCenter = _worldUi.CustomMinimumSize / 2.0f;

    var layers = Levels.Values
        .GroupBy(l => l.Layer)
        .OrderBy(g => g.Key)
        .Select(g => g.OrderBy(l => l.LayerIndex).ToList())
        .ToList();

    for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
    {
      bool unlockedLayer = false;
      foreach (LevelInfo levelNode in layers[layerIndex])
      {
        AddLevelUi(levelNode, layers[layerIndex].Count);
        if (levelNode.Unlocked)
          unlockedLayer = true;

        if (levelNode.Completed)
        {
          foreach (string nextNodeId in levelNode.NextNodes)
          {
            // Connect lines to next levels
            LevelInfo nextLevel = Levels[nextNodeId];

            Vector2 fromCenter = GetLevelButtonPosition(levelNode.Layer, levelNode.LayerIndex, AmountOfNodesPerLayerPerSection[levelNode.Layer, levelNode.LevelSection], levelNode.LevelSection);
            Vector2 toCenter = GetLevelButtonPosition(nextLevel.Layer, nextLevel.LayerIndex, AmountOfNodesPerLayerPerSection[nextLevel.Layer, nextLevel.LevelSection], nextLevel.LevelSection);

            Line2D line = new();
            line.Points = new Vector2[] { fromCenter, toCenter };
            line.Width = 2.0f;
            line.DefaultColor = new Color(1, 1, 1, 0.8f);
            _worldUi.AddChild(line);
            _worldUi.MoveChild(line, 0); // Make sure lines appear behind buttons
          }

          _completedLevels++;
        }
      }
    }

    _gridOverlay.IncreaseUnitCount(Mathf.FloorToInt(_completedLevels * 0.2f));
  }

  void AddLevelUi(LevelInfo levelNode, int nodesInLayer)
  {
    Vector2 buttonSize = new Vector2(_buttonWidth, _buttonHeight);
    Vector2 buttonPos = GetLevelButtonPosition(levelNode.Layer, levelNode.LayerIndex, AmountOfNodesPerLayerPerSection[levelNode.Layer, levelNode.LevelSection], levelNode.LevelSection);

    Button button = new();
    button.Text = levelNode.Name;
    button.Visible = levelNode.Unlocked;
    button.CustomMinimumSize = buttonSize;
    button.Position = buttonPos - buttonSize / 2.0f; // center the button on the position
    button.Pressed += () => OnLevelSelected(levelNode);

    if (levelNode.Completed)
    {
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
  }

  Vector2 GetLevelButtonPosition(int layer, int layerIndex, int nodesInLayer, int section)
  {
    if (layer == 0)
      return _worldCenter;

    float radius = layer * _ringSpacing;
    float sectionAngle = section * (Mathf.Tau / _numLevelSections) - Mathf.Pi / 2.0f;

    // Shrink the cone based on how many nodes are in this layer
    float maxConeWidth = Mathf.Tau / _numLevelSections * 0.95f; // 95% of available space per branch
    float coneWidth = Mathf.Min(maxConeWidth, _maxNodesInLayer * 0.15f);

    float nodeAngle;
    if (nodesInLayer <= 1)
      nodeAngle = sectionAngle;
    else
    {
      float margin = coneWidth * 0.1f;
      nodeAngle = sectionAngle - coneWidth / 2.0f + margin +
                  (float)layerIndex / (nodesInLayer - 1) * (coneWidth - margin * 2);
    }

    return _worldCenter + new Vector2(
        Mathf.Cos(nodeAngle) * radius,
        Mathf.Sin(nodeAngle) * radius
    );
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
      StartLevel(levelInfo.Terrains[0], levelInfo.Props[0], levelInfo.Units[0]);
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
    StartLevel(levelInfo.Terrains[0], levelInfo.Props[0], levelInfo.Units[0]);
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
      _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_vertical", Mathf.FloorToInt(_numLayers * _ringSpacing - 100));
      _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_horizontal", Mathf.FloorToInt(_numLayers * _ringSpacing - 100));
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
      Control hovered = GetViewport().GuiGetHoveredControl();
      if (hovered is Button || hovered is MenuButton)
        return;

      Vector2 mousePos = mouse.Position;
      Vector2I cell = _gridOverlay.GetCellUnderMouse(mousePos) - new Vector2I(1, 1);

      if (!GlobalFunctions.IsCellInsideGrid(cell))
      {
        _infoGui.Reset();
        _selectedCellsLayer.Clear();
        return;
      }
      else
      {
        _infoGui.SetSelectedInfo(_unitsGrid[cell.X, cell.Y], _terrainGrid[cell.X, cell.Y], _propsGrid[cell.X, cell.Y], cell);
        _selectedCellsLayer.Clear();

        if (_unitsGrid[cell.X, cell.Y] != null)
          _selectedCellsLayer.ShowCells(_unitsGrid[cell.X, cell.Y].GetOccupiedCells());
        else
          _selectedCellsLayer.ShowCells([cell]);
      }
    }
  }

  private void OnPlayPauseButtonPressed()
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

  private void OnSurrenderButtonPressed()
  {
    if (!_playing)
      return;
    Lose();
  }

  private void OnStartPlayingButtonPressed()
  {
    // Check whether there are no units on blocking terrain
    foreach (Unit unit in _units)
    {
      foreach (Vector2I cell in unit.GetOccupiedCells())
      {
        if ((_terrainGrid[cell.X, cell.Y] != null && _terrainGrid[cell.X, cell.Y].Blocking) ||
          (_propsGrid[cell.X, cell.Y] != null && _propsGrid[cell.X, cell.Y].Blocking))
        {
          ClearMessagePanel();
          _message.Text = "You have units placed on blocking terrain or blocking props. Please move those units before starting the level.";
          Button btn = new Button();
          btn.Text = "Okay";
          btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
          btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
          btn.CustomMinimumSize = new Vector2I(150, 50);
          btn.Pressed += () =>
          {
            ClearMessagePanel();
            _messagePanel.Hide();
          };
          _messageResponses.AddChild(btn);
          _messagePanel.Show();
          return;
        }
      }
    }

    StartPlaying();
  }

  private void OnQuitButtonPressed()
  {
    _activeGauntletLevelIndex = 0;
    Reset();
  }

  private void RespawnZombies()
  {
    List<Unit> removedZombies = new List<Unit>();
    foreach (Unit unit in _removedUnits)
    {
      if (unit.Id == "zombie")
      {
        removedZombies.Add(unit);

        Vector2I? spawnCell = GlobalFunctions.GetRandomGridEntitySpawnLocation(_unitsGrid, _terrainGrid, _propsGrid, unit.OccupiedCells, unit.Side);
        if (spawnCell == null)
          continue;

        Unit unitInstance = GD.Load<PackedScene>(unit.ScenePath).Instantiate() as Unit;
        unitInstance!.Initialize(unit.GetStartInfo(), unit.Side, spawnCell.Value);
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
    int currentIndex = 0;
    if (_unitToAct != null)
      currentIndex = units.IndexOf(_unitToAct);
    if (_targeting || _acting)
      currentIndex++;  // Increase current index by one to ensure the unit that is acting now is not resorted

    // Sort units by speed, then by position for consistent turn order
    List<Unit> sorted = units
    .Skip(currentIndex)  // Skip units that have already played their turn
    .OrderByDescending(u => u.Speed)  // Speed first
    .ThenBy(u => u.GlobalPosition.X)  // Leftmost first
    .ThenBy(u =>
        u.Side
            ? u.OccupiedMainCell.Y  // Player: lower Y first
            : GlobalConstants.GridSize.Y - 1 - u.OccupiedMainCell.Y)  // Enemy: higher Y first
    .ToList();

    units.RemoveRange(currentIndex, units.Count - currentIndex);
    units.AddRange(sorted);
  }

  private void TurnEnd()
  {
    _turn += 1;
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
    }

    // Possibly respawn zombies
    RespawnZombies();

    // Call turn end function of every unit
    // Use shallow copy to prevent spawned units having their turn end called as well
    foreach (Unit unit in _units.ToList())  
      unit.TurnEnd(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);

    // Call turn end function of every prop
    // Use shallow copy to prevent spawned props having their turn end called as well
    foreach (Prop prop in _propsGrid.Cast<Prop>().Where(p => p != null && IsInstanceValid(p)).ToList())
      prop.TurnEnd(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);

    SortUnits(_units);
    _unitsToAct = [.. _units];  // Reset units to act to level units list

    // Set unit to act to first unit again, because sorting might have changed the first unit
    _unitToAct = _unitsToAct.FirstOrDefault();
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
      AddStatRow(_damageDealtStats, entry.Key.DisplayName, entry.Value.DamageDealth, maxDamageDealt, entry.Key.Side);

    var sortedByDamageTaken = _unitsActivity
        .OrderByDescending(x => x.Value.DamageTaken)
        .ToList();
    int maxDamageTaken = sortedByDamageTaken.FirstOrDefault().Value?.DamageTaken ?? 1;

    foreach (var entry in sortedByDamageTaken)
      AddStatRow(_damageTakenStats, entry.Key.DisplayName, entry.Value.DamageTaken, maxDamageTaken, entry.Key.Side);

    var sortedByHealingDone = _unitsActivity
        .OrderByDescending(x => x.Value.HealingDone)
        .ToList();
    int maxHealingDone = sortedByHealingDone.FirstOrDefault().Value?.HealingDone ?? 1;

    foreach (var entry in sortedByHealingDone)
      AddStatRow(_healingDoneStats, entry.Key.DisplayName, entry.Value.HealingDone, maxHealingDone, entry.Key.Side);

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
    StartLevel(_activeLevel.Terrains[_activeGauntletLevelIndex], _activeLevel.Props[_activeGauntletLevelIndex], _activeLevel.Units[_activeGauntletLevelIndex]);
  }
}
