using Godot;
using Godot.NativeInterop;
using System;

public partial class LevelInfoContainer : VBoxContainer
{
  private Label _name;
  private Label _type;
  private Label _unlocked;
  private Label _completed;
  private VBoxContainer _units;
  private VBoxContainer _rewards;
  private Button _startLevelButton;
  private string _unitsSelectionScenePath = "res://scenes/units/unit_selection.tscn";
  private LevelInfo _activeLevel;

  [Signal]
  public delegate void StartLevelPressedEventHandler(LevelInfo levelInfo);

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _name = GetNode<Label>("Id/Value");
    _type = GetNode<Label>("Type/Value");
    _unlocked = GetNode<Label>("Unlocked/Value");
    _completed = GetNode<Label>("Completed/Value");
    _units = GetNode<VBoxContainer>("Units/VBoxContainer");
    _rewards = GetNode<VBoxContainer>("Rewards/VBoxContainer");
    _startLevelButton = GetNode<Button>("StartLevelButton");

    _startLevelButton.Pressed += OnStartLevelButtonPressed;
  }

  private void DisplayUnits(LevelInfo levelInfo)
  {
    // Clear previous units
    foreach (Node child in _units.GetChildren())
      child.Free();

    // Show separate list of units for every level in the gauntlet
    if (levelInfo.Gauntlet)
    {
      int levelIndex = 1;
      foreach (UnitInfo[,] grid in levelInfo.Units)
      {
        Label levelLabel = new();
        levelLabel.Text = $"Level {levelIndex++}";
        _units.AddChild(levelLabel);

        System.Collections.Generic.Dictionary<string, (UnitInfo info, int count)> unitCounts = new();
        foreach (UnitInfo unit in grid)
        {
          if (unit == null) continue;
          if (unitCounts.ContainsKey(unit.Id))
            unitCounts[unit.Id] = (unit, unitCounts[unit.Id].count + 1);
          else
            unitCounts[unit.Id] = (unit, 1);
        }

        foreach (var entry in unitCounts.Values)
        {
          PackedScene unitGuiScene = GD.Load<PackedScene>(_unitsSelectionScenePath);
          UnitGui unitGui = unitGuiScene.Instantiate<UnitGui>();
          unitGui.Info = entry.info;
          unitGui.Amount = entry.count;
          unitGui.Draggable = false;
          _units.AddChild(unitGui);
        }
      }
    }
    else
    {
      System.Collections.Generic.Dictionary<string, (UnitInfo info, int count)> unitCounts = new();
      foreach (UnitInfo[,] grid in levelInfo.Units)
      {
        foreach (UnitInfo unit in grid)
        {
          if (unit == null) continue;
          if (unitCounts.ContainsKey(unit.Id))
            unitCounts[unit.Id] = (unit, unitCounts[unit.Id].count + 1);
          else
            unitCounts[unit.Id] = (unit, 1);
        }
      }

      foreach (var entry in unitCounts.Values)
      {
        PackedScene unitGuiScene = GD.Load<PackedScene>(_unitsSelectionScenePath);
        UnitGui unitGui = unitGuiScene.Instantiate<UnitGui>();
        unitGui.Info = entry.info;
        unitGui.Amount = entry.count;
        unitGui.Draggable = false;
        _units.AddChild(unitGui);
      }
    }
  }

  private void DisplayRewards(LevelInfo levelInfo)
  {
    // Clear previous rewards
    foreach (Node child in _rewards.GetChildren())
      child.Free();

    Label coinsRewardLabel = new();
    coinsRewardLabel.Text = $"{levelInfo.CoinsReward} coins,";
    coinsRewardLabel.AutowrapMode = TextServer.AutowrapMode.Word;
    _rewards.AddChild(coinsRewardLabel);

    if (levelInfo.Rewards.Count == 0)
    {
      Label unitRewardsLabel = new();
      unitRewardsLabel.Text = "1 of 3 random units from this level";
      unitRewardsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
      _rewards.AddChild(unitRewardsLabel);
    }
    else
    {
      Label unitRewardsLabel = new();
      unitRewardsLabel.Text = $"1 of the following units:";
      unitRewardsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
      _rewards.AddChild(unitRewardsLabel);

      foreach (UnitInfo reward in levelInfo.Rewards)
      {
        HBoxContainer row = new();

        PackedScene unitGuiScene = GD.Load<PackedScene>(_unitsSelectionScenePath);
        UnitGui unitGui = unitGuiScene.Instantiate<UnitGui>();
        unitGui.Info = reward;
        unitGui.Amount = 1;
        unitGui.Draggable = false;
        row.AddChild(unitGui);

        _rewards.AddChild(row);
      }
    }
  }

  public void DisplayInfo(LevelInfo levelInfo)
  {
    _activeLevel = levelInfo;
    _name.Text = levelInfo.Name;
    if (levelInfo.Boss)
      _type.Text = "Boss";
    else if (levelInfo.Gauntlet)
      _type.Text = $"Gauntlet, {levelInfo.Units.Count} levels";
    else
      _type.Text = "Regular";
    _unlocked.Text = levelInfo.Unlocked ? "Yes" : "No";
    _completed.Text = levelInfo.Completed ? "Yes" : "No";

    DisplayUnits(levelInfo);
    DisplayRewards(levelInfo);
  }

  private void OnStartLevelButtonPressed()
  {
    EmitSignal(SignalName.StartLevelPressed, _activeLevel);
  }
}
