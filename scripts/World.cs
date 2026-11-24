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
  private PanelContainer _messagePanel;
  private ColorRect _dimmer;
  private Label _message;
  private HBoxContainer _messageResponses;
  private GridOverlay _gridOverlay;
  private GlobalSignals _globalSignals;
  private Node _unitsNode;
  private Node _terrainNode;
  private Node _propsNode;
  private UnitOutlines _unitOutlines;
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
  private bool _gameEnd = false;
  private int _level = 1;
  private int _completedLevels = 0;
  private int _slowDownLayer = 15;
  private int _turn = 0;
  private bool _turnOrderDirty = false;
  private double _tickAccumulator = 0;
  private int _tick = 0;
  private List<Unit> _actionQueue = new();  // Units that became ready on the same tick, acting one by one
  private CombatFx _combatFx = null!;
  private TurnTimeline _turnTimeline = null!;
  private WorldBar _worldBar = null!;
  private double _speed = 1.0f;
  private int _turnEndDamage = 1;
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
  private int _numLayers = 10;
  private int _maxNodesPerLayer = 8;
  private int _buttonWidth = 84;
  private int _buttonHeight = 48;
  private LevelInfo _activeLevel = null!;
  private LevelInfo? _selectedLevel = null;
  private List<Label> _sectionLabels = new();
  private int _activeGauntletLevelIndex = 0;
  private int _worldUiSpacing = 100;
  private bool _openedWorldOnce = false;
  private int _maxNodesInLayer = 0;
  private List<string> _levelSections = ["Earth", "Heaven", "Future", "Graveyard", "Hell"];
  private List<string> _nonBlockingTerrains = ["grass", "white_floor_tiles", "stone_floor", "dark_grass", "gray_floor_tiles"];
  private List<string> _blockingTerrains1 = ["water", "holy_water", "oil", "poisoned_water", "blood"];
  private List<string> _blockingTerrains2 = ["mountain", "green_mountain", "hole_in_floor", "mud", "lower_gray_floor_tiles"];

  private Vector2 _worldCenter;
  private float _ringSpacing = 150f; // pixels between rings
  private int _numLevelSections = 5;

  // Notify variables
  private string _explanationMarkScenePath = "res://scenes/explanation_mark.tscn";
  private Node2D[,] _explanationMarks = new Node2D[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

  // Damage and healing tracking
  // Specifically use use C# dict so unit reference does not dissapear when it gets queue freed
  private System.Collections.Generic.Dictionary<Unit, UnitActivity> _unitsActivity = new System.Collections.Generic.Dictionary<Unit, UnitActivity>();
  private PanelContainer _gameStats;
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
    _messagePanel = GetNode<PanelContainer>("CanvasLayer/Results/MessagePanel");
    _dimmer = GetNode<ColorRect>("CanvasLayer/Dimmer");
    _message = _messagePanel.GetNode<Label>("MessageContainer/Message");
    _messageResponses = _messagePanel.GetNode<HBoxContainer>("MessageContainer/Responses");
    _gridOverlay = GetNode<GridOverlay>("GridOverlay");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _unitsNode = GetNode("Units");
    _terrainNode = GetNode("Terrain");
    _propsNode = GetNode("Props");
    _unitOutlines = GetNode<UnitOutlines>("UnitOutlines");
    _selectedCellsLayer = GetNode<OverlayLayer>("SelectedUnitLayer");
    _activeUnitLayer = GetNode<OverlayLayer>("ActiveUnitLayer");
    _targetedCellsLayer = GetNode<OverlayLayer>("TargetedCellsLayer");
    _combatFx = new CombatFx { Name = "CombatFx" };
    AddChild(_combatFx);
    _turnTimeline = new TurnTimeline { Name = "TurnTimeline" };
    AddChild(_turnTimeline);
    _worldBar = new WorldBar { Name = "WorldBar" };
    AddChild(_worldBar);
    AddChild(new UnitDragPreview { Name = "UnitDragPreview" });
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
    _levelsUntilNextUnlock = GetNode<Label>("CanvasLayer/SelectionUi/HBoxContainer/LevelInfoContainer/Content/UnlockInfo/Value");

    // Activity UI
    _gameStats = GetNode<PanelContainer>("CanvasLayer/Results/GameStats");
    _statisticsButton = GetNode<MenuButton>("CanvasLayer/Results/GameStats/VBoxContainer/Header/StatisticsButton");
    _damageDealtStats = GetNode<VBoxContainer>("CanvasLayer/Results/GameStats/VBoxContainer/DamageDealt/VBoxContainer");
    _damageTakenStats = GetNode<VBoxContainer>("CanvasLayer/Results/GameStats/VBoxContainer/DamageTaken/VBoxContainer");
    _healingDoneStats = GetNode<VBoxContainer>("CanvasLayer/Results/GameStats/VBoxContainer/HealingDone/VBoxContainer");

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
    _globalSignals.UnitRemoved += OnUnitRemoved;
    _worldMapButton.Pressed += OnWorldMapPressed;
    _worldUi.Draw += DrawWorldMap;
    _worldUi.GuiInput += OnWorldMapInput;
    _statisticsPopup = _statisticsButton.GetPopup();
    _statisticsPopup.IdPressed += OnStatisticSelected;
    _levelInfoContainer.StartLevelPressed += OnStartLevelPressed;
    _selectedCellsLayer.OutlineColor = GamePalette.Gold;
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
    //int friendlyUnitCount = 0;
    //foreach (Unit unit in _units)
    //{
    //  if (unit.Side)
    //    friendlyUnitCount++;
    //}
    //if (friendlyUnitCount != _playerUnitsCount)
    //  GD.Print("hi");

    if (!_playing && _turnOrderDirty)
      UpdateTurnOrderPreview();

    int worldTicksLeft = GlobalConstants.TicksPerWorldAction - _tick % GlobalConstants.TicksPerWorldAction;
    // While the queue runs, the tick that started it is still unfinished, so the world is one tick closer
    _turnTimeline.SetData(_units, _unitsGrid, _actionQueue, _actionQueue.Count > 0 ? worldTicksLeft - 1 : worldTicksLeft);
    _turnTimeline.Visible = _units.Count > 0 && !_worldUi.GetParent<ScrollContainer>().Visible;
    _worldBar.SetData(worldTicksLeft, _playing && !_gameEnd, _turnTimeline.WorldHovered, _propsGrid.Cast<Prop>().Where(p => p != null && IsInstanceValid(p)));
    _worldBar.Visible = _turnTimeline.Visible;
    _unitOutlines.SetData(_units);
    _unitOutlines.Visible = _playing;
    _dimmer.Visible = _messagePanel.Visible || _gameStats.Visible;

    if (_paused)
      return;

    if (_playing && !_gameEnd)
    {
      // Fixed-step simulation: run as many ticks as real time (scaled by game speed) allows.
      // Cap per frame so a hitch doesn't make the board jump.
      _tickAccumulator += delta * _speed;
      double tickDuration = 1.0 / GlobalConstants.TicksPerSecond;
      int ticksThisFrame = 0;
      while (_tickAccumulator >= tickDuration && ticksThisFrame < 32 && _playing && !_gameEnd)
      {
        _tickAccumulator -= tickDuration;
        ticksThisFrame++;
        SimTick();
      }
      if (ticksThisFrame == 32)
        _tickAccumulator = 0;
    }
  }

  // While placing units (deck screen or before a level), show the turn order:
  // a number per unit showing the order of first actions.
  // Uses the same prediction as the timeline, which mirrors the simulation.
  private void UpdateTurnOrderPreview()
  {
    _turnOrderDirty = false;
    _units.RemoveAll(u => !IsInstanceValid(u));
    _unitToAct = null;
    SortUnits(_units);
    foreach (Unit unit in _units)
      unit.ResetActionBar();

    // Number = place of the unit's first action (all bars start empty, so this is who opens the battle)
    List<TurnTimeline.PredictedAction> firstActions = TurnTimeline.Predict(_units, _units.Count * 2);
    int order = 1;
    HashSet<Unit> numbered = new();
    foreach (TurnTimeline.PredictedAction action in firstActions)
    {
      if (numbered.Add(action.Unit))
        action.Unit.ShowTurnOrder(order++);
    }
    foreach (Unit unit in _units)
    {
      if (!numbered.Contains(unit))
        unit.ShowTurnOrder(null);  // Never acts (speed 0)
    }
  }

  // One simulation tick. Normally every action bar fills and wind-ups count down.
  // When two or more units become ready on the same tick, they go into _actionQueue and the battle
  // freezes: only the front unit of the queue winds up and strikes, one unit after another in turn
  // order, while all other bars, wind-ups and the turn clock wait. The queue tick does not count as
  // simulation time, so every unit keeps exactly its own speed.
  private void SimTick()
  {
    if (_actionQueue.Count > 0)
    {
      ProcessActionQueue();
      if (_actionQueue.Count == 0)
        AdvanceClock();
      return;
    }

    // Copy, because units can die or spawn while acting
    List<Unit> units = _units.ToList();
    foreach (Unit unit in units)
    {
      if (!IsUnitActive(unit))
        continue;
      _unitToAct = unit;
      unit.FillActionBar();
      unit.AdvanceTelegraph();
      if (_gameEnd)
        break;
    }

    if (!_gameEnd)
    {
      List<Unit> ready = units.Where(u => IsUnitActive(u) && u.IsReady).ToList();
      if (ready.Count == 1)
      {
        _unitToAct = ready[0];
        ready[0].TryStartAction();
      }
      else if (ready.Count > 1)
      {
        _actionQueue.AddRange(ready);
        ProcessActionQueue();
      }
    }
    _unitToAct = null;

    // A running queue finishes this tick first
    if (_actionQueue.Count == 0)
      AdvanceClock();
  }

  private void AdvanceClock()
  {
    _tick++;
    if (_tick % GlobalConstants.TicksPerTurn == 0 && !_gameEnd)
      TurnEnd();
  }

  // Advance the front unit of the queue by one step: start its action, or count down its wind-up.
  // Units that act without a wind-up (no targets, or stunned) are handled right away so they add no delay.
  private void ProcessActionQueue()
  {
    while (_actionQueue.Count > 0 && !_gameEnd)
    {
      Unit unit = _actionQueue[0];
      if (!IsUnitActive(unit))
      {
        _actionQueue.RemoveAt(0);
        continue;
      }

      _unitToAct = unit;
      bool wasTelegraphing = unit.IsTelegraphing;
      if (wasTelegraphing)
        unit.AdvanceTelegraph();
      else
        unit.TryStartAction();

      // Still winding up: continue with this unit next tick
      if (unit.IsTelegraphing && IsUnitActive(unit))
        break;
      _actionQueue.RemoveAt(0);

      // After a strike, the next unit starts on the next tick, so strikes never land together
      if (wasTelegraphing)
        break;
    }
    _unitToAct = null;
  }

  private bool IsUnitActive(Unit unit)
  {
    return IsInstanceValid(unit) && _units.Contains(unit) && unit.Health > 0;
  }

  public IReadOnlyList<Unit> ActionQueue => _actionQueue;

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

    // Copy units to create a list of units that are going to act this turn
    _unitsToAct = [.. _units];
    foreach (Unit unit in _unitsToAct)
    {
      unit.SetPlayData(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits, _activeUnitLayer, _targetedCellsLayer);
      unit.GameStart();
    }

    // Everyone starts with an empty action bar; units filling up on the same tick go leftmost, then frontmost first
    _unitToAct = null;
    SortUnits(_units);
    foreach (Unit unit in _units)
    {
      unit.ResetActionBar();
      unit.ShowTurnOrder(null);
    }
    _turnOrderDirty = false;
    _tick = 0;
    _actionQueue.Clear();
    _tickAccumulator = 0;
    _playing = true;

    // Set unit to act to first unit
    _unitToAct = _unitsToAct.FirstOrDefault();

    // Immediately win or lose when you or the enemy has no units
    if (_playerUnitsCount == 0)
      Lose();
    else if (_enemyUnitsCount == 0)
      Win();
  }

  private Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> LoadLevel(string levelId, int levelSection)
  {
    UnitInfo[,] unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    TerrainInfo[,] terrainGrid = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    PropInfo[,] propsGrid = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    UnitInfo[,] mainCellsUnitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    Dictionary levelData = (Dictionary)_levelsData[levelId];

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        terrainGrid[x, y] = GlobalConstants.TerrainsData[_nonBlockingTerrains[levelSection]];

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

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        terrain[x, y] = GlobalConstants.TerrainsData[_nonBlockingTerrains[levelSection]];

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
          terrain[x, y] = GlobalConstants.TerrainsData[_blockingTerrains1[levelSection]];
          if (isTopHalf) topHalfBlocking++;
          else bottomHalfBlocking++;
        }
        else if (waterVal > 0.40f)
        {
          terrain[x, y] = GlobalConstants.TerrainsData[_blockingTerrains2[levelSection]];
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
      UnitInfo unitInfo = GlobalConstants.UnitsData["archer"];
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
        unitInfo = GlobalConstants.UnitsData["archer"];
        possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, false);
        cost = unitInfo.OccupiedCells.Count;
        GD.Print("Unit placement failed. Placing archer instead.");
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
    _playing = false;
    _gameEnd = false;

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
    _tick = 0;
    _actionQueue.Clear();
    _tickAccumulator = 0;
    _combatFx.ClearAll();
    ClearMessagePanel();
    _messagePanel.Hide();
    ResetGameStats();
    _levelCounter.Text = _level.ToString();
    _activeUnitLayer.Clear();
    _targetedCellsLayer.Clear();
    _turnEndDamage = 1;
    _levelUnits.Clear();
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
    if (!_playing || _gameEnd)
      return;
    _gameEnd = true;
    _playPauseButton.Visible = false;
    _surrenderButton.Visible = false;

    ShowGameStats();

    if (_activeLevel.Gauntlet && _activeGauntletLevelIndex + 1 < _activeLevel.Units.Count)
    {
      int levelsLeft = _activeLevel.Units.Count - (_activeGauntletLevelIndex + 1);
      string levelText = levelsLeft > 1 ? "levels" : "level";
      _message.Text = $"You won level {_activeGauntletLevelIndex + 1} of the gauntlet.\n{levelsLeft} more {levelText} to go.";
      _activeGauntletLevelIndex++;
      Button btn = UiHelpers.MakeButton("Next level", primary: true);
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
    _message.Text = "Victory! Choose your reward";
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

      StyleLevelButton(_activeLevel);

      foreach (string nextNodeId in _activeLevel.NextNodes)
      {
        if (Levels.ContainsKey(nextNodeId) && !Levels[nextNodeId].Unlocked)
        {
          Levels[nextNodeId].Unlocked = true;
          _levelButtons[nextNodeId].Visible = true;
        }

        AddLevelConnection(_activeLevel, Levels[nextNodeId]);
      }
      PlaceSectionLabels();
    }
  }

  private void Lose()
  {
    if (!_playing || _gameEnd)
      return;
    _gameEnd = true;
    _playPauseButton.Visible = false;
    _surrenderButton.Visible = false;
    _message.Text = "Defeat";

    Button btn = UiHelpers.MakeButton("Try again", primary: true);
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
    vbox.AddThemeConstantOverride("separation", 12);
    _messageResponses.AddChild(vbox);

    HBoxContainer hbox = new();
    hbox.Alignment = BoxContainer.AlignmentMode.Center;
    hbox.AddThemeConstantOverride("separation", 10);
    vbox.AddChild(hbox);

    // Confirming only makes sense once a reward is picked
    Button confirmBtn = UiHelpers.MakeButton("Take reward", primary: true);
    confirmBtn.Disabled = true;
    confirmBtn.Pressed += OnConfirmRewardPressed;

    List<Button> rewardButtons = new();

    foreach (UnitInfo unitInfo in localRewardUnits)
    {
      Button btn = new Button();
      btn.Text = unitInfo.Name;
      btn.Icon = unitInfo.Texture;
      btn.IconAlignment = HorizontalAlignment.Center;
      btn.VerticalIconAlignment = VerticalAlignment.Top;
      btn.ExpandIcon = true;
      btn.ClipText = true;
      btn.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
      btn.TooltipText = unitInfo.Description;
      btn.CustomMinimumSize = new Vector2(132, 108);
      btn.AddThemeConstantOverride("icon_max_width", 64);

      Color rarityColor = GamePalette.Rarity(unitInfo.Rarity);
      btn.AddThemeColorOverride("font_color", rarityColor);
      btn.AddThemeStyleboxOverride("normal", GamePalette.Box(GamePalette.PanelLight, new Color(rarityColor, 0.6f), 2, 8, 10, 10));
      btn.AddThemeStyleboxOverride("hover", GamePalette.Box(GamePalette.PanelLight.Lightened(0.1f), rarityColor, 2, 8, 10, 10));
      btn.AddThemeStyleboxOverride("pressed", GamePalette.Box(GamePalette.Panel, rarityColor, 2, 8, 10, 10));

      btn.Pressed += () =>
      {
        OnRewardButtonSelected(unitInfo, btn, rewardButtons);
        confirmBtn.Disabled = false;
      };

      rewardButtons.Add(btn);
      hbox.AddChild(btn);
    }

    vbox.AddChild(confirmBtn);
  }

  private void OnGridEntityDied(GridEntity gridEntity)
  {
    gridEntity.DeathRattle(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
    if (gridEntity is Unit unit)
    {
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

    if (!_playing)
    {
      // Remove units from unit activity that were removed outside of playing
      _unitsActivity.Remove(unit);
    }

    _units.Remove(unit);
    _unitsToAct.Remove(unit);
    if (!_playing)
      _turnOrderDirty = true;

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
      if (!_playing)
        _turnOrderDirty = true;

      if (_infoGui.GetSelectedUnit() == unit)
      {
        _selectedCellsLayer.Clear();
        _selectedCellsLayer.ShowCells(unit.GetOccupiedCells());
        _infoGui.SetSelectedInfo(unit, _terrainGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y], 
          _propsGrid[unit.OccupiedMainCell.X, unit.OccupiedMainCell.Y], unit.OccupiedMainCell);
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

      if (!_playing)
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
      {
        SortUnits(_units);
        _turnOrderDirty = true;
      }
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
      }

      if (unit.Side)
        _playerUnitsCount++;
      else
        _enemyUnitsCount++;

      if (_playing)
        SortUnits(_unitsToAct);
      else
      {
        SortUnits(_units);
        _turnOrderDirty = true;
      }
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
      {
        SortUnits(_units);
        _turnOrderDirty = true;
      }
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
      {
        SortUnits(_units);
        _turnOrderDirty = true;
      }
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
    // Visual effects shrink with game speed, but not fully, so they stay visible at high speeds
    _combatFx.GameSpeed = (float)_speed;
    _combatFx.TimeScale = (float)Mathf.Clamp(1.0 / Mathf.Sqrt(_speed), 0.2, 1.4);
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
        cost, health, health, damage, armor, speed, description, rarity, types, stage);

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
    if (difficulty < 3) return 1;
    if (difficulty < 5) return 2;
    if (difficulty < 7) return 3;
    if (difficulty < 9) return 4;
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
        if (layer == 2)
          nodesInLayer = AmountOfNodesPerLayerPerSection[layer - 1, q] + 2;
        else if (layer < (_numLayers * 0.75f - 1))
          nodesInLayer = AmountOfNodesPerLayerPerSection[layer - 1, q] + 1;
        else if (layer < (_numLayers - 1)) // After a while the layers converge to one boss layer
          nodesInLayer = AmountOfNodesPerLayerPerSection[layer - 1, q] - 2;
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

      Tuple<TerrainInfo[,], PropInfo[,], UnitInfo[,]> level = LoadLevel(levelId, section);
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
          name: $"{_levelSections[section]}\nLayer {layer + 1}\nGauntlet {node + 1}",
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

    string name = layer == 0 ? "Start" : $"{_levelSections[section]}\nLayer {layer + 1}\nLevel {node + 1}";
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
            AddLevelConnection(levelNode, Levels[nextNodeId]);
          }

          _completedLevels++;
        }
      }
    }

    AddSectionLabels();
    _gridOverlay.IncreaseUnitCount(Mathf.FloorToInt(_completedLevels * 0.2f));
  }

  void AddLevelUi(LevelInfo levelNode, int nodesInLayer)
  {
    Vector2 buttonSize = new Vector2(_buttonWidth, _buttonHeight);
    Vector2 buttonPos = GetLevelButtonPosition(levelNode.Layer, levelNode.LayerIndex, AmountOfNodesPerLayerPerSection[levelNode.Layer, levelNode.LevelSection], levelNode.LevelSection);

    Button button = new();
    button.Text = GetLevelButtonText(levelNode);
    button.Visible = levelNode.Unlocked;
    button.CustomMinimumSize = buttonSize;
    button.Position = buttonPos - buttonSize / 2.0f; // center the button on the position
    button.AddThemeFontSizeOverride("font_size", 13);
    button.Pressed += () => OnLevelSelected(levelNode);

    _levelButtons[levelNode.Id] = button;
    StyleLevelButton(levelNode);
    _worldUi.AddChild(button);
  }

  // Map nodes only have room for a short name; the full one is in the level panel
  private string GetLevelButtonText(LevelInfo level)
  {
    if (level.Layer == 0)
      return "Start";
    if (level.Boss)
      return $"BOSS\n{level.Name}";
    string number = $"{level.Layer + 1}-{level.LayerIndex + 1}";
    return level.Gauntlet ? $"Gauntlet\n{number}" : number;
  }

  // Filled = completed, outlined in the section's color = open. Bosses are red, gauntlets have a thick border.
  private void StyleLevelButton(LevelInfo level)
  {
    Button button = _levelButtons[level.Id];
    Color color = level.Layer == 0 ? GamePalette.Gold : level.Boss ? GamePalette.Enemy : GamePalette.Section(level.LevelSection);
    bool selected = level == _selectedLevel;
    int borderWidth = level.Boss || level.Gauntlet ? 4 : 2;
    Color background = level.Completed ? GamePalette.Panel.Lerp(color, 0.45f) : GamePalette.Panel.Lerp(color, 0.1f);
    Color border = selected ? Colors.White : level.Completed ? new Color(color, 0.6f) : color;

    button.AddThemeColorOverride("font_color", level.Completed ? GamePalette.Text : color.Lightened(0.3f));
    button.AddThemeStyleboxOverride("normal", GamePalette.Box(background, border, borderWidth, 14));
    button.AddThemeStyleboxOverride("hover", GamePalette.Box(background.Lightened(0.12f), Colors.White, borderWidth, 14));
    button.AddThemeStyleboxOverride("pressed", GamePalette.Box(background.Darkened(0.2f), Colors.White, borderWidth, 14));
  }

  private void AddLevelConnection(LevelInfo from, LevelInfo to)
  {
    Line2D line = new();
    line.Points = new Vector2[]
    {
      GetLevelButtonPosition(from.Layer, from.LayerIndex, AmountOfNodesPerLayerPerSection[from.Layer, from.LevelSection], from.LevelSection),
      GetLevelButtonPosition(to.Layer, to.LayerIndex, AmountOfNodesPerLayerPerSection[to.Layer, to.LevelSection], to.LevelSection),
    };
    line.Width = 3.0f;
    line.Antialiased = true;
    line.DefaultColor = new Color(GamePalette.Section(to.LevelSection), 0.6f);
    _worldUi.AddChild(line);
    _worldUi.MoveChild(line, 0); // Make sure lines appear behind buttons
  }

  private void AddSectionLabels()
  {
    _sectionLabels.Clear();
    for (int section = 0; section < _numLevelSections; section++)
    {
      Label label = new Label
      {
        Text = _levelSections[section].ToUpper(),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
        ThemeTypeVariation = "ValueLabel",
      };
      label.AddThemeColorOverride("font_color", GamePalette.Section(section));
      label.AddThemeFontSizeOverride("font_size", 13);
      label.AddThemeStyleboxOverride("normal", GamePalette.Box(GamePalette.Background, new Color(GamePalette.Section(section), 0.35f), 1, 10, 10, 2));
      _worldUi.AddChild(label);
      _sectionLabels.Add(label);
    }
    PlaceSectionLabels();
  }

  // A name is centered in its section on the first ring, and moves just above the section's first level once that is open
  private void PlaceSectionLabels()
  {
    for (int section = 0; section < _sectionLabels.Count; section++)
    {
      Label label = _sectionLabels[section];
      float angle = section * (Mathf.Tau / _numLevelSections) - Mathf.Pi / 2.0f;
      Vector2 center = _worldCenter + Vector2.FromAngle(angle) * _ringSpacing;
      if (Levels.Values.Any(l => l.Layer == 1 && l.LevelSection == section && l.Unlocked))
        center.Y -= _buttonHeight / 2.0f + 16;
      label.Position = center - label.GetCombinedMinimumSize() / 2;
    }
  }

  // Map backdrop: a tinted wedge per section, and a ring per layer
  private void DrawWorldMap()
  {
    float radius = (_numLayers - 0.5f) * _ringSpacing;
    float half = Mathf.Tau / _numLevelSections / 2.0f;

    for (int section = 0; section < _numLevelSections; section++)
    {
      float angle = section * (Mathf.Tau / _numLevelSections) - Mathf.Pi / 2.0f;
      List<Vector2> wedge = [_worldCenter];
      for (int i = 0; i <= 24; i++)
        wedge.Add(_worldCenter + Vector2.FromAngle(angle - half + 2 * half * i / 24.0f) * radius);
      _worldUi.DrawColoredPolygon(wedge.ToArray(), new Color(GamePalette.Section(section), 0.06f));
    }

    for (int layer = 1; layer < _numLayers; layer++)
      _worldUi.DrawArc(_worldCenter, layer * _ringSpacing, 0, Mathf.Tau, 128, new Color(GamePalette.Border, 0.35f), 1.0f, true);
  }

  // Drag anywhere on the map to pan it
  private void OnWorldMapInput(InputEvent @event)
  {
    if (@event is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Left))
    {
      ScrollContainer scroll = _worldUi.GetParent<ScrollContainer>();
      scroll.ScrollHorizontal -= Mathf.RoundToInt(motion.Relative.X);
      scroll.ScrollVertical -= Mathf.RoundToInt(motion.Relative.Y);
    }
  }

  Vector2 GetLevelButtonPosition(int layer, int layerIndex, int nodesInLayer, int section)
  {
    if (layer == 0)
      return _worldCenter;

    float radius = layer * _ringSpacing;
    float sectionAngle = section * (Mathf.Tau / _numLevelSections) - Mathf.Pi / 2.0f;

    // Shrink the cone based on how many nodes are in this layer
    float maxConeWidth = Mathf.Tau / _numLevelSections * 0.95f; // 95% of available space per branch
    float coneWidth = Mathf.Min(maxConeWidth, (nodesInLayer + 3) * 0.15f);

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
    LevelInfo? previous = _selectedLevel;
    _selectedLevel = levelInfo;
    if (previous != null && _levelButtons.ContainsKey(previous.Id))
      StyleLevelButton(previous);
    StyleLevelButton(levelInfo);
    _levelInfoContainer.DisplayInfo(levelInfo);
  }

  private void OnStartLevelPressed(LevelInfo levelInfo)
  {
    if (!_decksHandler.HasSelectedDeck)
    {
      ClearMessagePanel();
      _message.Text = "Create or select a deck first, so you have units to fight with.";
      Button okayBtn = UiHelpers.MakeButton("Okay", primary: true);
      okayBtn.Pressed += () =>
      {
        ClearMessagePanel();
        _messagePanel.Hide();
      };
      _messageResponses.AddChild(okayBtn);
      _messagePanel.Show();
      return;
    }

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
        Button yesBtn = UiHelpers.MakeButton("Yes", primary: true);
        yesBtn.Pressed += () => OnRedoConfirmed(levelInfo);
        _messageResponses.AddChild(yesBtn);

        Button noBtn = UiHelpers.MakeButton("No");
        noBtn.Pressed += OnRedoCanceled;
        _messageResponses.AddChild(noBtn);
      }
      else
      {
        _message.Text = $"You have already completed this level. You do not have the required {levelRedoCost} coins to repeat this level.";
        Button btn = UiHelpers.MakeButton("Okay", primary: true);
        btn.Pressed += () => OnRedoCanceled();
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
    _decksHandler.Hide();
    _levelInfoContainer.Show();
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
      if (_selectedLevel != null)
        _levelInfoContainer.DisplayInfo(_selectedLevel);
    }

    if (!_openedWorldOnce)
    {
      _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_vertical", Mathf.FloorToInt(_numLayers * _ringSpacing - 134));
      _worldUi.GetParent<ScrollContainer>().SetDeferred("scroll_horizontal", Mathf.FloorToInt(_numLayers * _ringSpacing - 134));
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

    _selectedLevel = null;
    _levelInfoContainer.ClearInfo();
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
      // Only clicks on the board itself change the selection; UI above it handles its own clicks
      if (hovered != null && hovered != _gridOverlay)
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
      _playPauseButton.Text = "Resume";
    }
  }

  private void OnSurrenderButtonPressed()
  {
    if (!_playing || _gameEnd)
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
          Button btn = UiHelpers.MakeButton("Okay", primary: true);
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
        unitInstance!.SetPlayData(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits, _activeUnitLayer, _targetedCellsLayer);
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
    if (_unitToAct != null && units.Contains(_unitToAct))
      currentIndex = units.IndexOf(_unitToAct);

    // Turn order for units that act on the same tick: leftmost first, then frontmost
    List<Unit> sorted = units
    .Skip(currentIndex)  // Skip units that have already played their turn
    .OrderBy(u => u.GlobalPosition.X)  // Leftmost first
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
    if (_turn > 50)
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
    }

    // Call turn end function of every unit
    // Use shallow copy to prevent spawned units having their turn end called as well
    foreach (Unit unit in _units.ToList())  
      unit.TurnEnd(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);

    // The world acts: dead zombies revive, and every prop's turn end function is called
    // Use shallow copy to prevent spawned props having their turn end called as well
    if (_turn % GlobalConstants.TurnsPerBoardUpdate == 0)
    {
      _worldBar.PlayAction();
      RespawnZombies();
      foreach (Prop prop in _propsGrid.Cast<Prop>().Where(p => p != null && IsInstanceValid(p)).ToList())
        prop.TurnEnd(_unitsGrid, _terrainGrid, _propsGrid, _units, _removedUnits);
    }

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

  private void AddStatRow(Control container, Unit unit, int value, int maxValue)
  {
    HBoxContainer row = new();
    row.AddThemeConstantOverride("separation", 8);
    row.CustomMinimumSize = new Vector2(0, 26);

    TextureRect icon = new TextureRect
    {
      Texture = unit.Texture,
      CustomMinimumSize = new Vector2(24, 24),
      ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
      StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
    };
    row.AddChild(icon);

    Label nameLabel = new Label { Text = unit.DisplayName, CustomMinimumSize = new Vector2(130, 0) };
    nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
    row.AddChild(nameLabel);

    ProgressBar bar = new ProgressBar
    {
      MaxValue = Mathf.Max(1, maxValue),
      Value = value,
      ShowPercentage = false,
      CustomMinimumSize = new Vector2(0, 12),
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter,
    };
    bar.AddThemeStyleboxOverride("background", GamePalette.Box(GamePalette.Background, GamePalette.Background, 0, 4, 0, 0));
    bar.AddThemeStyleboxOverride("fill", GamePalette.Box(GamePalette.Side(unit.Side), GamePalette.Panel, 0, 4, 0, 0));
    row.AddChild(bar);

    Label valueLabel = new Label
    {
      Text = value.ToString(),
      CustomMinimumSize = new Vector2(44, 0),
      HorizontalAlignment = HorizontalAlignment.Right,
      ThemeTypeVariation = "ValueLabel",
    };
    row.AddChild(valueLabel);

    row.AddChild(new Control { CustomMinimumSize = new Vector2(6, 0) }); // Keeps the value clear of the scrollbar
    container.AddChild(row);
  }

  private void ShowGameStats()
  {
    var sortedByDamageDealt = _unitsActivity
        .OrderByDescending(x => x.Value.DamageDealth)
        .ToList();
    int maxDamageDealt = sortedByDamageDealt.FirstOrDefault().Value?.DamageDealth ?? 1;

    foreach (var entry in sortedByDamageDealt)
      AddStatRow(_damageDealtStats, entry.Key, entry.Value.DamageDealth, maxDamageDealt);

    var sortedByDamageTaken = _unitsActivity
        .OrderByDescending(x => x.Value.DamageTaken)
        .ToList();
    int maxDamageTaken = sortedByDamageTaken.FirstOrDefault().Value?.DamageTaken ?? 1;

    foreach (var entry in sortedByDamageTaken)
      AddStatRow(_damageTakenStats, entry.Key, entry.Value.DamageTaken, maxDamageTaken);

    var sortedByHealingDone = _unitsActivity
        .OrderByDescending(x => x.Value.HealingDone)
        .ToList();
    int maxHealingDone = sortedByHealingDone.FirstOrDefault().Value?.HealingDone ?? 1;

    foreach (var entry in sortedByHealingDone)
      AddStatRow(_healingDoneStats, entry.Key, entry.Value.HealingDone, maxHealingDone);

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
