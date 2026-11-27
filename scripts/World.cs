using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Connects the parts of the game and controls its flow: choosing a level, placing units, the battle, and its result
public partial class World : Node2D
{
  private const int LevelRedoCost = 100;
  private const float GauntletRedoCostFactor = 2.5f;
  private const int BossesToWin = 5;

  // In the order of the speed button's menu
  private static readonly float[] Speeds = [0.5f, 1, 2, 4, 8, 16, 32];

  public WorldMap Map { get; private set; } = null!;
  public int Coins { get; private set; } = 0;

  private readonly Random _rng = new Random();
  private Board _board = null!;
  private Battle _battle = null!;

  private LevelInfo _activeLevel = null!;
  private int _gauntletIndex = 0;

  // Units that fought in earlier rounds of the current tournament
  private readonly List<UnitInfo> _tournamentUsed = new();

  // Fighting a level from the editor, without rewards or progress
  private bool _testing = false;

  // The player's placement right before the battle, to retry the level with
  private UnitInfo[,] _playerUnitsBeforeBattle = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

  // Board visuals
  private GridOverlay _gridOverlay = null!;
  private CombatFx _combatFx = null!;
  private TurnTimeline _turnTimeline = null!;
  private WorldBar _worldBar = null!;
  private UnitOutlines _unitOutlines = null!;
  private OverlayLayer _activeUnitLayer = null!;
  private OverlayLayer _targetedCellsLayer = null!;

  // UI
  private MessagePopup _popup = null!;
  private BattleStats _stats = null!;
  private ColorRect _dimmer = null!;
  private DecksHandler _decksHandler = null!;
  private LevelInfoContainer _levelInfo = null!;
  private LevelEditor _levelEditor = null!;
  private Label _turnCounter = null!;
  private Label _coinsCounter = null!;
  private Label _gatekeepersBeaten = null!;
  private MenuButton _speedButton = null!;
  private Button _worldMapButton = null!;
  private Button _pauseButton = null!;
  private Button _surrenderButton = null!;
  private Button _startButton = null!;
  private Button _quitButton = null!;

  public override void _Ready()
  {
    GetNodes();
    CreateBoardAndBattle();
    ConnectButtons();

    GameData.Load();
    GameCursor.Apply();

    // Start with 2 archers
    _decksHandler.AddUnit(GlobalConstants.UnitsData["archer"]);
    _decksHandler.AddUnit(GlobalConstants.UnitsData["archer"]);

    new WorldGenerator(_rng).Generate(Map.Levels, Map.NodesPerLayer);
    BuildMap();
  }

  private void GetNodes()
  {
    _gridOverlay = GetNode<GridOverlay>("GridOverlay");
    _unitOutlines = GetNode<UnitOutlines>("UnitOutlines");
    _activeUnitLayer = GetNode<OverlayLayer>("ActiveUnitLayer");
    _targetedCellsLayer = GetNode<OverlayLayer>("TargetedCellsLayer");

    Map = GetNode<WorldMap>("CanvasLayer/ScrollContainer/WorldUi");
    _popup = GetNode<MessagePopup>("CanvasLayer/Results/MessagePanel");
    _stats = GetNode<BattleStats>("CanvasLayer/Results/GameStats");
    _dimmer = GetNode<ColorRect>("CanvasLayer/Dimmer");
    _decksHandler = GetNode<DecksHandler>("CanvasLayer/SelectionUi/HBoxContainer/DecksHandler");
    _levelInfo = GetNode<LevelInfoContainer>("CanvasLayer/SelectionUi/HBoxContainer/LevelInfoContainer");
    _gatekeepersBeaten = _levelInfo.GetNode<Label>("Content/UnlockInfo/Value");
    _levelEditor = GetNode<LevelEditor>("CanvasLayer/SelectionUi/HBoxContainer/LevelEditor");

    Node bottomUi = GetNode("CanvasLayer/BottomUi");
    _turnCounter = bottomUi.GetNode<Label>("TurnCounter/Counter");
    _coinsCounter = bottomUi.GetNode<Label>("CoinsCounter/Counter");
    _speedButton = bottomUi.GetNode<MenuButton>("SpeedButton");
    _worldMapButton = bottomUi.GetNode<Button>("WorldMapButton");
    _pauseButton = bottomUi.GetNode<Button>("PlayPauseButton");
    _surrenderButton = bottomUi.GetNode<Button>("SurrenderButton");
    _startButton = bottomUi.GetNode<Button>("StartButton");
    _quitButton = bottomUi.GetNode<Button>("QuitButton");
  }

  private void CreateBoardAndBattle()
  {
    _activeUnitLayer.OutlineColor = Colors.Yellow;
    _activeUnitLayer.HighlightColor = new Color(1, 1, 1, 0.15f);
    _targetedCellsLayer.OutlineColor = Colors.Red;

    _combatFx = new CombatFx { Name = "CombatFx" };
    AddChild(_combatFx);
    _turnTimeline = new TurnTimeline { Name = "TurnTimeline" };
    AddChild(_turnTimeline);
    _worldBar = new WorldBar { Name = "WorldBar" };
    AddChild(_worldBar);
    AddChild(new UnitDragPreview { Name = "UnitDragPreview" });

    _board = new Board(this, GetNode<GlobalSignals>("/root/GlobalSignals"));
    _board.UnitAdded += _stats.Track;
    _board.UnitRemoved += OnUnitRemoved;
    _board.UnitDied += OnUnitDied;

    BoardSelection selection = new BoardSelection { Name = "BoardSelection" };
    selection.Initialize(_board, _gridOverlay, GetNode<OverlayLayer>("SelectedUnitLayer"),
      GetNode<InfoGui>("CanvasLayer/SelectionUi/HBoxContainer/InfoContainer"));
    AddChild(selection);

    SightPreview sightPreview = new SightPreview { Name = "SightPreview" };
    sightPreview.Initialize(_board, _gridOverlay);
    AddChild(sightPreview);

    _levelEditor.Initialize(_board, selection, _gridOverlay, _decksHandler, _popup);

    _battle = new Battle(_board, _activeUnitLayer, _targetedCellsLayer);
    _battle.TurnChanged += turn => _turnCounter.Text = turn.ToString();
    _battle.WorldActing += _worldBar.PlayAction;
  }

  private void ConnectButtons()
  {
    _worldMapButton.Pressed += OnWorldMapPressed;
    _startButton.Pressed += OnStartBattlePressed;
    _quitButton.Pressed += OnQuitLevelPressed;
    _pauseButton.Pressed += OnPausePressed;
    _surrenderButton.Pressed += OnSurrenderPressed;
    _speedButton.GetPopup().IdPressed += OnSpeedSelected;
    _levelInfo.StartLevelPressed += OnStartLevelPressed;
    Map.LevelSelected += ShowLevelInfo;
    _levelEditor.Closed += CloseEditor;
    _levelEditor.TestRequested += TestLevel;
  }

  public override void _Process(double delta)
  {
    _board.UpdateTurnOrderPreview();
    UpdateVisuals();
    _battle.Advance(delta);
  }

  private void UpdateVisuals()
  {
    int worldTicksLeft = _battle.WorldTicksLeft;
    // While the queue runs, the tick that started it is still unfinished, so the world is one tick closer
    _turnTimeline.SetData(_board.Units, _board.UnitsGrid, _battle.ActionQueue, _battle.ActionQueue.Count > 0 ? worldTicksLeft - 1 : worldTicksLeft);
    _turnTimeline.Visible = _board.Units.Count > 0 && !Map.IsShown;
    _worldBar.SetData(worldTicksLeft, _battle.Running, _turnTimeline.WorldHovered, _board.Props);
    _worldBar.Visible = _turnTimeline.Visible;
    _unitOutlines.SetData(_board.Units);
    _unitOutlines.Visible = _battle.Playing;
    _dimmer.Visible = _popup.Visible || _stats.Visible;
  }

  // ---- World map

  private void BuildMap()
  {
    Map.Build();
    UpdateProgress();
  }

  // Progress follows from which levels are completed, so it is also right after loading a save
  private void UpdateProgress()
  {
    int gatekeepersBeaten = Map.CountGatekeepersBeaten();
    _gatekeepersBeaten.Text = $"{gatekeepersBeaten}/{WorldGenerator.NumTiers * WorldGenerator.NumSections}";
    _gridOverlay.SetExtraUnitSlots(gatekeepersBeaten);
  }

  // Called after the map's levels were replaced by those of a save file
  public void LoadLevels()
  {
    _levelInfo.ClearInfo();
    BuildMap();
  }

  private void ShowLevelInfo(LevelInfo level)
  {
    int tier = WorldGenerator.GetTier(level.Layer);
    string lockedText = Map.IsOpen(level)
      ? ""
      : $"Beat the tier {tier} gatekeepers of {WorldMap.GatekeepersPerTier} sections first ({Map.CountGatekeepersBeaten(tier - 1)}/{WorldMap.GatekeepersPerTier}).";
    _levelInfo.DisplayInfo(level, lockedText);
  }

  private void OnWorldMapPressed()
  {
    if (Map.IsShown)
      CloseMap();
    else
    {
      OpenMap();
      if (Map.SelectedLevel != null)
        ShowLevelInfo(Map.SelectedLevel);
    }
  }

  private void OpenMap()
  {
    Map.Open();
    _decksHandler.Hide();
    _levelInfo.Show();
  }

  private void CloseMap()
  {
    Map.Close();
    _decksHandler.Show();
    _levelInfo.Hide();
  }

  private void OnStartLevelPressed(LevelInfo level)
  {
    if (!_decksHandler.HasSelectedDeck)
      _popup.ShowMessage("Create or select a deck first, so you have units to fight with.", ("Okay", _popup.Close));
    else if (level.Completed)
      AskToRedoLevel(level);
    else
      EnterLevel(level);
  }

  private void AskToRedoLevel(LevelInfo level)
  {
    CloseMap();

    int cost = level.Gauntlet || level.Tournament ? Mathf.FloorToInt(LevelRedoCost * GauntletRedoCostFactor) : LevelRedoCost;
    if (Coins < cost)
    {
      _popup.ShowMessage($"You have already completed this level. You do not have the required {cost} coins to repeat this level.", ("Okay", Cancel));
      return;
    }

    _popup.ShowMessage($"You have already completed this level. Do you want to repeat the level for {cost} coins?",
      ("Yes", Confirm), ("No", Cancel));

    void Confirm()
    {
      _popup.Close();
      UpdateCoins(-cost);
      EnterLevel(level);
    }

    void Cancel()
    {
      _popup.Close();
      OpenMap();
    }
  }

  // ---- Level

  private void EnterLevel(LevelInfo level)
  {
    CloseMap();
    _activeLevel = level;
    _gauntletIndex = 0;
    _tournamentUsed.Clear();
    SetUpLevel();
  }

  // With keepPlacement, the player's units from before the last battle are already on the board
  private void SetUpLevel(bool keepPlacement = false)
  {
    if (!keepPlacement)
      _decksHandler.LoadBeforeLevel(_activeLevel.Tournament ? _tournamentUsed : null);
    _board.SpawnLevel(_activeLevel.Terrains[_gauntletIndex], _activeLevel.Props[_gauntletIndex], _activeLevel.Units[_gauntletIndex]);
    ShowButtons(_startButton, _quitButton);
  }

  // Clear the board and go back to the deck, or to the player's placement from before the last battle
  private void LeaveLevel(bool keepPlacement = false)
  {
    _battle.Stop();
    _board.Clear();
    _combatFx.ClearAll();
    _popup.Close();
    _stats.Reset();
    _activeUnitLayer.Clear();
    _targetedCellsLayer.Clear();

    if (keepPlacement)
      _decksHandler.ReloadAfterLevel(_playerUnitsBeforeBattle);
    else
      _decksHandler.LoadAfterLevel();

    _gridOverlay.SetInteractionLocked(false);
    ShowButtons(_worldMapButton);
  }

  private void ShowButtons(params Button[] visible)
  {
    foreach (Button button in new[] { _worldMapButton, _startButton, _quitButton, _pauseButton, _surrenderButton })
      button.Visible = visible.Contains(button);
  }

  private void OnQuitLevelPressed()
  {
    if (_testing)
      EndTest();
    else
      LeaveLevel();
  }

  // ---- Level editor

  // The editor uses the board, so it can only be opened from the deck screen or the map
  public bool CanOpenEditor => _worldMapButton.Visible;
  public bool IsEditing => _levelEditor.Visible || _testing;

  public void OpenEditor()
  {
    CloseMap();
    _decksHandler.Suspend();
    ShowButtons();
    _levelEditor.Open();
  }

  private void CloseEditor()
  {
    LeaveEditor();
    ShowButtons(_worldMapButton);
  }

  private void LeaveEditor()
  {
    _levelEditor.Close();
    UpdateProgress();
    _decksHandler.Resume();
  }

  private void TestLevel(LevelInfo level)
  {
    _testing = true;
    LeaveEditor();
    EnterLevel(level);
  }

  private void EndTest()
  {
    _testing = false;
    LeaveLevel();
    _decksHandler.Suspend();
    ShowButtons();
    _levelEditor.Resume();
  }

  // ---- Battle

  private void OnStartBattlePressed()
  {
    if (_board.GetBlockedUnitCells().Any())
    {
      _popup.ShowMessage("You have units placed on blocking terrain or blocking props. Please move those units before starting the level.", ("Okay", _popup.Close));
      return;
    }

    _playerUnitsBeforeBattle = _board.GetUnits(playerSide: true);
    _gridOverlay.SetInteractionLocked(true);
    ShowButtons(_pauseButton, _surrenderButton);
    _battle.Start();

    // Immediately win or lose when you or the enemy has no units
    if (_board.PlayerUnitsCount == 0)
      Lose();
    else if (_board.EnemyUnitsCount == 0)
      Win();
  }

  private void OnUnitRemoved(Unit unit)
  {
    if (!_battle.Playing)
      _stats.Forget(unit);
  }

  private void OnUnitDied()
  {
    if (_board.EnemyUnitsCount == 0)
      Win();
    else if (_board.PlayerUnitsCount == 0)
      Lose();
  }

  private void OnSurrenderPressed()
  {
    Lose();
  }

  private void OnPausePressed()
  {
    _battle.Paused = !_battle.Paused;
    _pauseButton.Text = _battle.Paused ? "Resume" : "Pause";
  }

  private void OnSpeedSelected(long id)
  {
    float speed = Speeds[id];
    _battle.Speed = speed;
    _speedButton.Text = "Speed: " + _speedButton.GetPopup().GetItemText((int)id);
    // Visual effects shrink with game speed, but not fully, so they stay visible at high speeds
    _combatFx.GameSpeed = speed;
    _combatFx.TimeScale = (float)Mathf.Clamp(1.0 / Mathf.Sqrt(speed), 0.2, 1.4);
  }

  private void EndBattle()
  {
    _battle.End();
    ShowButtons();
    _stats.ShowStats();
  }

  private void Lose()
  {
    if (!_battle.Running)
      return;

    EndBattle();
    if (_testing)
      _popup.ShowMessage("Defeat", ("Try again", Retry), ("Back to editor", EndTest));
    else
      _popup.ShowMessage("Defeat", ("Try again", Retry));
  }

  private void Retry()
  {
    LeaveLevel(keepPlacement: true);
    // A tournament lists the units that may still fight again
    SetUpLevel(keepPlacement: !_activeLevel.Tournament);
  }

  private void Win()
  {
    if (!_battle.Running)
      return;

    EndBattle();

    if (_testing)
    {
      _popup.ShowMessage("Victory", ("Back to editor", EndTest), ("Fight again", Retry));
      return;
    }

    if ((_activeLevel.Gauntlet || _activeLevel.Tournament) && _gauntletIndex + 1 < _activeLevel.Units.Count)
    {
      ShowNextGauntletLevel();
      return;
    }

    int otherBossesDefeated = Map.Levels.Values.Count(l => l.Boss && l.Completed && l != _activeLevel);
    if (_activeLevel.Boss && !_activeLevel.Completed && otherBossesDefeated + 1 == BossesToWin)
    {
      _popup.ShowMessage("You win the demo!\nThanks for playing!");
      return;
    }

    bool gainsSlot = WorldGenerator.IsGatekeeper(_activeLevel) && !_activeLevel.Completed;
    string title = gainsSlot ? "Gatekeeper beaten! You gain a unit slot. Choose your reward" : "Victory! Choose your reward";
    _popup.ShowRewards(title, GetRewards(), reward =>
    {
      _decksHandler.AddUnit(reward);
      LeaveLevel();
    });
    UpdateCoins(_activeLevel.CoinsReward);
    CompleteLevel();
  }

  private void ShowNextGauntletLevel()
  {
    if (_activeLevel.Tournament)
    {
      ShowNextTournamentRound();
      return;
    }

    _gauntletIndex++;
    int levelsLeft = _activeLevel.Units.Count - _gauntletIndex;
    string levelText = levelsLeft > 1 ? "levels" : "level";
    _popup.ShowMessage($"You won level {_gauntletIndex} of the gauntlet.\n{levelsLeft} more {levelText} to go.", ("Next level", () =>
    {
      LeaveLevel();
      SetUpLevel();
    }));
  }

  // The units that fought rest for the rest of the tournament
  private void ShowNextTournamentRound()
  {
    foreach (UnitInfo unit in _playerUnitsBeforeBattle)
    {
      if (unit != null)
        _tournamentUsed.Add(GlobalConstants.UnitsData[unit.Id]);
    }

    _gauntletIndex++;
    int rounds = _activeLevel.Units.Count;
    _popup.ShowMessage($"You won round {_gauntletIndex} of {rounds}.\nThe units that fought cannot fight again in this tournament.", ("Next round", () =>
    {
      LeaveLevel();
      SetUpLevel();
    }));
  }

  // Levels without fixed rewards offer 3 random units of the level
  private List<UnitInfo> GetRewards()
  {
    if (_activeLevel.Rewards.Count > 0)
      return _activeLevel.Rewards;

    List<UnitInfo> levelUnits = _board.LevelUnits;
    List<int> indices = new();
    while (indices.Count < Mathf.Min(3, levelUnits.Count))
    {
      int index = _rng.Next(0, levelUnits.Count);
      if (!indices.Contains(index))
        indices.Add(index);
    }
    return indices.Select(index => levelUnits[index]).ToList();
  }

  private void CompleteLevel()
  {
    _activeLevel.Completed = true;
    UpdateProgress();
    Map.ShowCompleted(_activeLevel);
  }

  public void UpdateCoins(int amount)
  {
    Coins += amount;
    _coinsCounter.Text = Coins.ToString();
  }
}
