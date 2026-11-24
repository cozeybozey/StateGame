using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// The map of all levels. Sits inside a ScrollContainer, which is shown and hidden as a whole.
public partial class WorldMap : Panel
{
  private const float RingSpacing = 150f; // pixels between rings
  private const int ButtonWidth = 84;
  private const int ButtonHeight = 48;

  public Godot.Collections.Dictionary<string, LevelInfo> Levels = new();

  // Number of levels per [layer, section]
  public int[,] NodesPerLayer = new int[WorldGenerator.NumLayers, WorldGenerator.NumSections];

  public LevelInfo? SelectedLevel { get; private set; } = null;

  public event Action<LevelInfo>? LevelSelected;

  private readonly Dictionary<string, Button> _buttons = new();
  private readonly List<Label> _sectionLabels = new();
  private ScrollContainer _scroll = null!;
  private Vector2 _center;
  private bool _openedOnce = false;

  public override void _Ready()
  {
    _scroll = GetParent<ScrollContainer>();
  }

  public bool IsOpen => _scroll.Visible;

  public void Open()
  {
    _scroll.Show();

    if (!_openedOnce)
    {
      int scroll = Mathf.FloorToInt(WorldGenerator.NumLayers * RingSpacing - 134);
      _scroll.SetDeferred("scroll_vertical", scroll);
      _scroll.SetDeferred("scroll_horizontal", scroll);
      _openedOnce = true;
    }
  }

  public void Close()
  {
    _scroll.Hide();
  }

  // Create the UI for the current contents of Levels
  public void Build()
  {
    SelectedLevel = null;
    foreach (Node child in GetChildren())
      child.QueueFree();

    float diameter = WorldGenerator.NumLayers * RingSpacing * 2 + 300;
    CustomMinimumSize = new Vector2(diameter, diameter);
    _center = CustomMinimumSize / 2.0f;

    foreach (LevelInfo level in Levels.Values.OrderBy(l => l.Layer).ThenBy(l => l.LayerIndex))
    {
      AddButton(level);
      if (level.Completed)
        ConnectToNextLevels(level);
    }
    AddSectionLabels();
  }

  // Update the map after a level was completed, and unlock the levels behind it
  public void ShowCompleted(LevelInfo level)
  {
    StyleButton(level);
    foreach (string nextNodeId in level.NextNodes)
    {
      if (!Levels[nextNodeId].Unlocked)
      {
        Levels[nextNodeId].Unlocked = true;
        _buttons[nextNodeId].Visible = true;
      }
    }
    ConnectToNextLevels(level);
    PlaceSectionLabels();
  }

  private void AddButton(LevelInfo level)
  {
    Vector2 size = new Vector2(ButtonWidth, ButtonHeight);
    Button button = new();
    button.Text = GetButtonText(level);
    button.Visible = level.Unlocked;
    button.CustomMinimumSize = size;
    button.Position = GetLevelPosition(level) - size / 2.0f; // center the button on the position
    button.AddThemeFontSizeOverride("font_size", 13);
    button.Pressed += () => Select(level);

    _buttons[level.Id] = button;
    StyleButton(level);
    AddChild(button);
  }

  private void Select(LevelInfo level)
  {
    LevelInfo? previous = SelectedLevel;
    SelectedLevel = level;
    if (previous != null && _buttons.ContainsKey(previous.Id))
      StyleButton(previous);
    StyleButton(level);
    LevelSelected?.Invoke(level);
  }

  // Map nodes only have room for a short name; the full one is in the level panel
  private static string GetButtonText(LevelInfo level)
  {
    if (level.Layer == 0)
      return "Start";
    if (level.Boss)
      return $"BOSS\n{level.Name}";
    string number = $"{level.Layer + 1}-{level.LayerIndex + 1}";
    return level.Gauntlet ? $"Gauntlet\n{number}" : number;
  }

  // Filled = completed, outlined in the section's color = open. Bosses are red, gauntlets have a thick border.
  private void StyleButton(LevelInfo level)
  {
    Button button = _buttons[level.Id];
    Color color = level.Layer == 0 ? GamePalette.Gold : level.Boss ? GamePalette.Enemy : GamePalette.Section(level.LevelSection);
    bool selected = level == SelectedLevel;
    int borderWidth = level.Boss || level.Gauntlet ? 4 : 2;
    Color background = level.Completed ? GamePalette.Panel.Lerp(color, 0.45f) : GamePalette.Panel.Lerp(color, 0.1f);
    Color border = selected ? Colors.White : level.Completed ? new Color(color, 0.6f) : color;

    button.AddThemeColorOverride("font_color", level.Completed ? GamePalette.Text : color.Lightened(0.3f));
    button.AddThemeStyleboxOverride("normal", GamePalette.Box(background, border, borderWidth, 14));
    button.AddThemeStyleboxOverride("hover", GamePalette.Box(background.Lightened(0.12f), Colors.White, borderWidth, 14));
    button.AddThemeStyleboxOverride("pressed", GamePalette.Box(background.Darkened(0.2f), Colors.White, borderWidth, 14));
  }

  private void ConnectToNextLevels(LevelInfo level)
  {
    foreach (string nextNodeId in level.NextNodes)
    {
      LevelInfo next = Levels[nextNodeId];
      Line2D line = new();
      line.Points = [GetLevelPosition(level), GetLevelPosition(next)];
      line.Width = 3.0f;
      line.Antialiased = true;
      line.DefaultColor = new Color(GamePalette.Section(next.LevelSection), 0.6f);
      AddChild(line);
      MoveChild(line, 0); // Make sure lines appear behind buttons
    }
  }

  private void AddSectionLabels()
  {
    _sectionLabels.Clear();
    for (int section = 0; section < WorldGenerator.NumSections; section++)
    {
      Label label = new Label
      {
        Text = WorldGenerator.SectionNames[section].ToUpper(),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
        ThemeTypeVariation = "ValueLabel",
      };
      label.AddThemeColorOverride("font_color", GamePalette.Section(section));
      label.AddThemeFontSizeOverride("font_size", 13);
      label.AddThemeStyleboxOverride("normal", GamePalette.Box(GamePalette.Background, new Color(GamePalette.Section(section), 0.35f), 1, 10, 10, 2));
      AddChild(label);
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
      Vector2 center = _center + Vector2.FromAngle(GetSectionAngle(section)) * RingSpacing;
      if (Levels.Values.Any(l => l.Layer == 1 && l.LevelSection == section && l.Unlocked))
        center.Y -= ButtonHeight / 2.0f + 16;
      label.Position = center - label.GetCombinedMinimumSize() / 2;
    }
  }

  private static float GetSectionAngle(int section)
  {
    return section * (Mathf.Tau / WorldGenerator.NumSections) - Mathf.Pi / 2.0f;
  }

  private Vector2 GetLevelPosition(LevelInfo level)
  {
    if (level.Layer == 0)
      return _center;

    int nodesInLayer = NodesPerLayer[level.Layer, level.LevelSection];
    float radius = level.Layer * RingSpacing;
    float nodeAngle = GetSectionAngle(level.LevelSection);

    if (nodesInLayer > 1)
    {
      // Shrink the cone based on how many nodes are in this layer
      float maxConeWidth = Mathf.Tau / WorldGenerator.NumSections * 0.95f; // 95% of available space per branch
      float coneWidth = Mathf.Min(maxConeWidth, (nodesInLayer + 3) * 0.15f);
      float margin = coneWidth * 0.1f;
      nodeAngle = nodeAngle - coneWidth / 2.0f + margin +
                  (float)level.LayerIndex / (nodesInLayer - 1) * (coneWidth - margin * 2);
    }

    return _center + new Vector2(Mathf.Cos(nodeAngle) * radius, Mathf.Sin(nodeAngle) * radius);
  }

  // Backdrop: a tinted wedge per section, and a ring per layer
  public override void _Draw()
  {
    float radius = (WorldGenerator.NumLayers - 0.5f) * RingSpacing;
    float half = Mathf.Tau / WorldGenerator.NumSections / 2.0f;

    for (int section = 0; section < WorldGenerator.NumSections; section++)
    {
      float angle = GetSectionAngle(section);
      List<Vector2> wedge = [_center];
      for (int i = 0; i <= 24; i++)
        wedge.Add(_center + Vector2.FromAngle(angle - half + 2 * half * i / 24.0f) * radius);
      DrawColoredPolygon(wedge.ToArray(), new Color(GamePalette.Section(section), 0.06f));
    }

    for (int layer = 1; layer < WorldGenerator.NumLayers; layer++)
      DrawArc(_center, layer * RingSpacing, 0, Mathf.Tau, 128, new Color(GamePalette.Border, 0.35f), 1.0f, true);
  }

  // Drag anywhere on the map to pan it
  public override void _GuiInput(InputEvent @event)
  {
    if (@event is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Left))
    {
      _scroll.ScrollHorizontal -= Mathf.RoundToInt(motion.Relative.X);
      _scroll.ScrollVertical -= Mathf.RoundToInt(motion.Relative.Y);
    }
  }
}
