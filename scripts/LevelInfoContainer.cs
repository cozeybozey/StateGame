using Godot;
using Godot.NativeInterop;
using System;

public partial class LevelInfoContainer : PanelContainer
{
  private Label _name;
  private Label _hint;
  private VBoxContainer _details;
  private HFlowContainer _tags;
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
    _name = GetNode<Label>("Content/Name");
    _hint = GetNode<Label>("Content/Hint");
    _details = GetNode<VBoxContainer>("Content/Details");
    _tags = GetNode<HFlowContainer>("Content/Details/Tags");
    _units = GetNode<VBoxContainer>("Content/Details/UnitsPanel/Units/VBoxContainer");
    _rewards = GetNode<VBoxContainer>("Content/Details/RewardsPanel/Rewards/VBoxContainer");
    _startLevelButton = GetNode<Button>("Content/StartLevelButton");

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
    coinsRewardLabel.Text = $"{levelInfo.CoinsReward} coins";
    coinsRewardLabel.AutowrapMode = TextServer.AutowrapMode.Word;
    _rewards.AddChild(coinsRewardLabel);

    if (levelInfo.Rewards.Count == 0)
    {
      Label unitRewardsLabel = new();
      unitRewardsLabel.Text = "Plus one of 3 random units from this level";
      unitRewardsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
      _rewards.AddChild(unitRewardsLabel);
    }
    else
    {
      Label unitRewardsLabel = new();
      unitRewardsLabel.Text = "One of these units:";
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

  public void ClearInfo()
  {
    _activeLevel = null!;
    _name.Text = "World map";
    _hint.Show();
    _details.Hide();
    _startLevelButton.Disabled = true;
  }

  public void DisplayInfo(LevelInfo levelInfo)
  {
    _activeLevel = levelInfo;
    _name.Text = levelInfo.Name.Replace("\n", " - ");
    _hint.Hide();
    _details.Show();
    _startLevelButton.Disabled = false;
    _startLevelButton.Text = levelInfo.Completed ? "Replay level" : "Start level";

    string type = levelInfo.Boss ? "Boss" : levelInfo.Gauntlet ? $"Gauntlet, {levelInfo.Units.Count} levels" : "Regular";
    UiHelpers.ShowTags(_tags, levelInfo.Completed ? [type, "Completed"] : [type]);

    DisplayUnits(levelInfo);
    DisplayRewards(levelInfo);
  }

  private void OnStartLevelButtonPressed()
  {
    EmitSignal(SignalName.StartLevelPressed, _activeLevel);
  }
}
