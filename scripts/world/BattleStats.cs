using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Tracks damage and healing per unit, and shows it after a battle
public partial class BattleStats : PanelContainer
{
  // A C# dictionary keeps the unit reference after the unit's node is freed
  private readonly Dictionary<Unit, UnitActivity> _activity = new();

  private MenuButton _statisticButton = null!;
  private VBoxContainer[] _lists = null!;

  public override void _Ready()
  {
    _statisticButton = GetNode<MenuButton>("VBoxContainer/Header/StatisticsButton");
    _statisticButton.GetPopup().IdPressed += OnStatisticSelected;

    // In the order of the statistic button's menu
    _lists =
    [
      GetNode<VBoxContainer>("VBoxContainer/DamageDealt/VBoxContainer"),
      GetNode<VBoxContainer>("VBoxContainer/DamageTaken/VBoxContainer"),
      GetNode<VBoxContainer>("VBoxContainer/HealingDone/VBoxContainer"),
    ];

    GlobalSignals signals = GetNode<GlobalSignals>("/root/GlobalSignals");
    signals.DamageDealt += (entity, amount) => Record(entity, a => a.DamageDealth += amount);
    signals.DamageTaken += (entity, amount) => Record(entity, a => a.DamageTaken += amount);
    signals.HealingDone += (entity, amount) => Record(entity, a => a.HealingDone += amount);
    signals.HealingReceived += (entity, amount) => Record(entity, a => a.HealingReceived += amount);
  }

  public void Track(Unit unit)
  {
    _activity[unit] = new UnitActivity(0, 0, 0, 0);
  }

  public void Forget(Unit unit)
  {
    _activity.Remove(unit);
  }

  private void Record(GridEntity entity, Action<UnitActivity> change)
  {
    if (entity is Unit unit && _activity.TryGetValue(unit, out UnitActivity? activity))
      change(activity);
  }

  public void ShowStats()
  {
    Fill(_lists[0], a => a.DamageDealth);
    Fill(_lists[1], a => a.DamageTaken);
    Fill(_lists[2], a => a.HealingDone);
    Show();
  }

  public void Reset()
  {
    _activity.Clear();
    Hide();
    foreach (VBoxContainer list in _lists)
      foreach (Node child in list.GetChildren())
        child.QueueFree();
  }

  private void Fill(VBoxContainer list, Func<UnitActivity, int> getValue)
  {
    List<(Unit Unit, int Value)> rows = _activity
        .Select(entry => (entry.Key, getValue(entry.Value)))
        .OrderByDescending(row => row.Item2)
        .ToList();
    foreach ((Unit unit, int value) in rows)
      list.AddChild(MakeRow(unit, value, rows[0].Value));
  }

  private static HBoxContainer MakeRow(Unit unit, int value, int maxValue)
  {
    HBoxContainer row = new();
    row.AddThemeConstantOverride("separation", 8);
    row.CustomMinimumSize = new Vector2(0, 26);

    row.AddChild(new TextureRect
    {
      Texture = unit.Texture,
      CustomMinimumSize = new Vector2(24, 24),
      ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
      StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
    });

    row.AddChild(new Label
    {
      Text = unit.DisplayName,
      CustomMinimumSize = new Vector2(130, 0),
      TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
    });

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

    row.AddChild(new Label
    {
      Text = value.ToString(),
      CustomMinimumSize = new Vector2(44, 0),
      HorizontalAlignment = HorizontalAlignment.Right,
      ThemeTypeVariation = "ValueLabel",
    });

    row.AddChild(new Control { CustomMinimumSize = new Vector2(6, 0) }); // Keeps the value clear of the scrollbar
    return row;
  }

  private void OnStatisticSelected(long id)
  {
    for (int i = 0; i < _lists.Length; i++)
      _lists[i].GetParent<ScrollContainer>().Visible = i == id;
    _statisticButton.Text = _statisticButton.GetPopup().GetItemText((int)id);
  }
}
