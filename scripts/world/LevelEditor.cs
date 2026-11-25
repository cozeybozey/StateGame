using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Panel to design levels. The board shows the level as it will be in the game: units are dragged onto the enemy half,
// terrain and props are drawn anywhere.
public partial class LevelEditor : PanelContainer
{
  private enum Tool { Units, Terrain, Props }

  private const string UnitRowScenePath = "res://scenes/units/unit_selection.tscn";
  private static readonly int[] BrushSizes = [1, 3, 5];

  // Layers where designed levels can replace random ones: all except the start and the boss
  private const int FirstLayer = 1;
  private const int LastLayer = WorldGenerator.NumLayers - 2;

  public event Action? Closed;
  public event Action<LevelInfo>? TestRequested;

  private Board _board = null!;
  private BoardSelection _selection = null!;
  private GridOverlay _gridOverlay = null!;
  private DecksHandler _decksHandler = null!;
  private MessagePopup _popup = null!;

  private OptionButton _levelButton = null!;
  private LineEdit _nameEdit = null!;
  private OptionButton _sectionButton = null!;
  private OptionButton _layerButton = null!;
  private LineEdit _search = null!;
  private Control _brushRow = null!;
  private OptionButton _brushSizeButton = null!;
  private VBoxContainer _unitList = null!;
  private VBoxContainer _terrainList = null!;
  private VBoxContainer _propList = null!;
  private Label _status = null!;
  private OptionButton _deckButton = null!;
  private Button _testButton = null!;
  private Button _deleteButton = null!;
  private Dictionary<Tool, Button> _tabs = null!;

  private LevelStore _store = null!;

  // Empty for a level that was never saved
  private string _levelId = "";
  private string _savedState = "";
  private DesignedLevel _testedLevel = new();
  private string _baseTerrain = "";

  private Tool _tool = Tool.Units;
  private TerrainInfo? _terrainBrush = null;
  private PropInfo? _propBrush = null;
  private Vector2I? _lastPaintedCell = null;
  private Node2D _brushPreview = null!;

  public override void _Ready()
  {
    _levelButton = GetNode<OptionButton>("Content/Level");
    _nameEdit = GetNode<LineEdit>("Content/LevelName");
    _sectionButton = GetNode<OptionButton>("Content/Place/Section");
    _layerButton = GetNode<OptionButton>("Content/Place/Layer");
    _search = GetNode<LineEdit>("Content/Search");
    _brushRow = GetNode<Control>("Content/BrushRow");
    _brushSizeButton = GetNode<OptionButton>("Content/BrushRow/Size");
    _unitList = GetNode<VBoxContainer>("Content/Scroll/Lists/Units");
    _terrainList = GetNode<VBoxContainer>("Content/Scroll/Lists/Terrain");
    _propList = GetNode<VBoxContainer>("Content/Scroll/Lists/Props");
    _status = GetNode<Label>("Content/Status");
    _deckButton = GetNode<OptionButton>("Content/TestRow/Deck");
    _testButton = GetNode<Button>("Content/TestRow/Test");
    _deleteButton = GetNode<Button>("Content/SaveRow/Delete");

    _levelButton.ItemSelected += _ => OnLevelChosen();
    _sectionButton.ItemSelected += _ => OnSectionChosen();
    _layerButton.ItemSelected += _ => ShowPlaceHint();
    _search.TextChanged += _ => FilterUnits();
    _testButton.Pressed += Test;
    _deleteButton.Pressed += OnDeletePressed;
    GetNode<Button>("Content/SaveRow/Clear").Pressed += ClearTool;
    GetNode<Button>("Content/SaveRow/Save").Pressed += Save;
    GetNode<Button>("Content/Header/Close").Pressed += () => ConfirmDiscard(() => Closed?.Invoke(), () => { });

    _tabs = new Dictionary<Tool, Button>();
    ButtonGroup group = new();
    foreach (Tool tool in Enum.GetValues<Tool>())
    {
      Button tab = GetNode<Button>($"Content/Tabs/{tool}");
      tab.ButtonGroup = group;
      tab.Pressed += () => SelectTool(tool);
      _tabs[tool] = tab;
    }
    foreach (int size in BrushSizes)
      _brushSizeButton.AddItem($"{size} x {size}", size);
  }

  public void Initialize(Board board, BoardSelection selection, GridOverlay gridOverlay, DecksHandler decksHandler, MessagePopup popup)
  {
    _board = board;
    _selection = selection;
    _gridOverlay = gridOverlay;
    _decksHandler = decksHandler;
    _popup = popup;

    _gridOverlay.GuiInput += OnBoardInput;

    // On the board's own canvas, above units
    _brushPreview = new Node2D { Name = "BrushPreview", ZIndex = 45 };
    _brushPreview.Draw += DrawBrushPreview;
    _gridOverlay.GetParent().CallDeferred(Node.MethodName.AddChild, _brushPreview);
  }

  public override void _Process(double delta)
  {
    _brushPreview.Visible = Visible && _tool != Tool.Units;
    if (_brushPreview.Visible)
      _brushPreview.QueueRedraw();
  }

  public void Open()
  {
    _store = new LevelStore();
    if (_unitList.GetChildCount() == 0)
    {
      FillPlaceButtons();
      FillUnits();
      FillBrushes();
    }
    FillLevels();
    ShowEditor();
    ShowLevel(new DesignedLevel());
  }

  // Back from a test: the level is exactly as it was left
  public void Resume()
  {
    ShowEditor();
    ShowOnBoard(_testedLevel);
  }

  private void ShowEditor()
  {
    _gridOverlay.EditsEnemySide = true;
    _gridOverlay.RemoveUnitSlotLimit();
    ShowDecks();
    SelectTool(_tool);
    Show();
  }

  public void Close()
  {
    Hide();
    _gridOverlay.EditsEnemySide = false;
    _gridOverlay.SetInteractionLocked(false);
    _selection.Enabled = true;
    _board.Clear();
  }

  // ---- Choosing a level

  private void FillPlaceButtons()
  {
    _sectionButton.AddItem("Not in the world");
    foreach (string section in WorldGenerator.SectionNames)
      _sectionButton.AddItem(section);
    for (int layer = FirstLayer; layer <= LastLayer; layer++)
      _layerButton.AddItem($"Layer {layer + 1}", layer);
  }

  private void FillLevels()
  {
    _levelButton.Clear();
    _levelButton.AddItem("New level");
    _levelButton.SetItemMetadata(0, "");
    foreach (DesignedLevel level in _store.Levels)
    {
      _levelButton.AddItem(level.Name);
      _levelButton.SetItemMetadata(_levelButton.ItemCount - 1, level.Id);
    }
    SelectCurrentLevel();
  }

  private void SelectCurrentLevel()
  {
    for (int i = 0; i < _levelButton.ItemCount; i++)
    {
      if (_levelButton.GetItemMetadata(i).AsString() == _levelId)
        _levelButton.Select(i);
    }
  }

  private void OnLevelChosen()
  {
    string id = _levelButton.GetSelectedMetadata().AsString();
    ConfirmDiscard(() => ShowLevel(id == "" ? new DesignedLevel() : _store.Get(id)), SelectCurrentLevel);
  }

  private void ShowLevel(DesignedLevel level)
  {
    _levelId = level.Id;
    _nameEdit.Text = level.Name;
    _sectionButton.Select(level.Section + 1);
    _layerButton.Select(_layerButton.GetItemIndex(level.Layer));
    _deleteButton.Disabled = _levelId == "" || WorldGenerator.IsBossLevel(_levelId);
    SelectCurrentLevel();

    bool overlaps = level.RemoveOverlaps();
    ShowOnBoard(level);
    _savedState = GetState();
    ShowPlaceHint();
    if (overlaps)
      _status.Text = "Some units stood on top of each other and were left out.";
  }

  private void ShowOnBoard(DesignedLevel level)
  {
    _board.Clear();
    _baseTerrain = LevelGenerator.GetBaseTerrain(GetSection());
    foreach (Vector2I cell in Board.Cells)
    {
      _board.SetTerrain(cell, level.Terrain[cell.X, cell.Y] ?? GlobalConstants.TerrainsData[_baseTerrain]);
      _board.SetProp(cell, level.Props[cell.X, cell.Y]);
    }
    _gridOverlay.LoadDeck(level.Units);
  }

  // Levels outside the world are shown on the first section's terrain
  private int GetSection()
  {
    return WorldGenerator.GetBossSection(_levelId) ?? Mathf.Max(0, _sectionButton.Selected - 1);
  }

  private void OnSectionChosen()
  {
    string oldBase = _baseTerrain;
    _baseTerrain = LevelGenerator.GetBaseTerrain(GetSection());
    foreach (Vector2I cell in Board.Cells)
    {
      if (_board.TerrainGrid[cell.X, cell.Y].Id == oldBase)
        _board.SetTerrain(cell, GlobalConstants.TerrainsData[_baseTerrain]);
    }
    ShowPlaceHint();
  }

  private DesignedLevel ReadLevel()
  {
    DesignedLevel level = new DesignedLevel
    {
      Id = _levelId,
      Name = _nameEdit.Text.Trim(),
      Section = _sectionButton.Selected - 1,
      Layer = _layerButton.GetSelectedId(),
      Units = _board.GetUnits(playerSide: false),
    };
    foreach (Vector2I cell in Board.Cells)
    {
      string terrain = _board.TerrainGrid[cell.X, cell.Y].Id;
      if (terrain != _baseTerrain)
        level.Terrain[cell.X, cell.Y] = GlobalConstants.TerrainsData[terrain];
      Prop prop = _board.PropsGrid[cell.X, cell.Y];
      if (prop != null)
        level.Props[cell.X, cell.Y] = GlobalConstants.PropsData[prop.Id];
    }
    return level;
  }

  // Everything that Save would store, to detect unsaved changes
  private string GetState()
  {
    DesignedLevel level = ReadLevel();
    IEnumerable<string> cells = Board.Cells
      .Select(c => $"{level.Units[c.X, c.Y]?.Id},{level.Terrain[c.X, c.Y]?.Id},{level.Props[c.X, c.Y]?.Id}");
    return $"{level.Name}|{level.Section}|{level.Layer}|{string.Join(";", cells)}";
  }

  private void ConfirmDiscard(Action onConfirmed, Action onCanceled)
  {
    if (GetState() == _savedState)
    {
      onConfirmed();
      return;
    }
    _popup.ShowMessage("This level has unsaved changes.",
      ("Keep editing", () => { _popup.Close(); onCanceled(); }),
      ("Discard changes", () => { _popup.Close(); onConfirmed(); }));
  }

  // ---- Saving

  private bool IsComplete(DesignedLevel level)
  {
    if (level.Name == "")
      _status.Text = "Give the level a name first.";
    else if (level.Units.Cast<UnitInfo>().All(u => u == null))
      _status.Text = "Place at least one unit first.";
    else
      return true;
    return false;
  }

  private void Save()
  {
    DesignedLevel level = ReadLevel();
    if (!IsComplete(level))
      return;

    if (level.Id == "")
      level.Id = MakeId(level.Name);
    if (!_store.Save(level))
    {
      _status.Text = "Could not write levels.json.";
      return;
    }

    _levelId = level.Id;
    _savedState = GetState();
    _deleteButton.Disabled = WorldGenerator.IsBossLevel(_levelId);
    FillLevels();
    ShowPlaceHint();
    _status.Text = "Saved. " + _status.Text;
  }

  private string MakeId(string name)
  {
    string slug = new string(name.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
    if (slug == "")
      slug = "level";
    string id = slug;
    for (int i = 2; _store.Contains(id); i++)
      id = $"{slug}_{i}";
    return id;
  }

  private void OnDeletePressed()
  {
    _popup.ShowMessage($"Delete \"{_store.Get(_levelId).Name}\" from levels.json?",
      ("Keep", _popup.Close),
      ("Delete", () =>
      {
        _popup.Close();
        _store.Delete(_levelId);
        ShowLevel(new DesignedLevel());
        FillLevels();
        _status.Text = "Deleted.";
      }));
  }

  // A layer only has a fixed number of levels, so designed levels beyond that are not used
  private void ShowPlaceHint()
  {
    _layerButton.Disabled = _sectionButton.Selected == 0;
    if (WorldGenerator.IsBossLevel(_levelId))
      _status.Text = "This is a boss. It always appears at the end of its section.";
    else if (_sectionButton.Selected == 0)
      _status.Text = "Choose a section to make this level appear in the world.";
    else
    {
      int layer = _layerButton.GetSelectedId();
      int room = WorldGenerator.GetNodesInLayer(layer);
      int used = _store.Levels.Count(l => l.Id != _levelId && l.Section == _sectionButton.Selected - 1 && l.Layer == layer);
      _status.Text = used < room
        ? $"Uses place {used + 1} of {room} in new worlds (restart the game)."
        : $"All {room} places in this layer are already taken.";
    }
  }

  // ---- Testing

  private void ShowDecks()
  {
    _deckButton.Clear();
    foreach (string deck in _decksHandler.Decks.Keys)
    {
      _deckButton.AddItem(deck);
      if (deck == _decksHandler.SelectedDeck)
        _deckButton.Select(_deckButton.ItemCount - 1);
    }
    _testButton.Disabled = _deckButton.ItemCount == 0;
    _testButton.TooltipText = _testButton.Disabled ? "Create a deck first" : "";
  }

  private void Test()
  {
    DesignedLevel level = ReadLevel();
    if (!IsComplete(level))
      return;

    _testedLevel = level;
    _decksHandler.SelectDeck(_deckButton.GetItemText(_deckButton.Selected));
    TestRequested?.Invoke(WorldGenerator.CreateTestLevel(level, GetSection()));
  }

  // ---- Tools

  private void SelectTool(Tool tool)
  {
    _tool = tool;
    _tabs[tool].ButtonPressed = true;
    _unitList.Visible = tool == Tool.Units;
    _terrainList.Visible = tool == Tool.Terrain;
    _propList.Visible = tool == Tool.Props;
    _search.Visible = tool == Tool.Units;
    _brushRow.Visible = tool != Tool.Units;

    // While drawing, the mouse neither drags units nor selects cells
    _gridOverlay.SetInteractionLocked(tool != Tool.Units);
    _selection.Enabled = tool == Tool.Units;

    _status.Text = tool == Tool.Units
      ? "Drag units onto the enemy half. Right-click removes a unit."
      : "Left mouse button draws, right mouse button erases.";
  }

  private void ClearTool()
  {
    if (_tool == Tool.Units)
    {
      _gridOverlay.ClearUnits();
      return;
    }
    foreach (Vector2I cell in Board.Cells)
      Erase(cell);
  }

  private void FillUnits()
  {
    PackedScene rowScene = GD.Load<PackedScene>(UnitRowScenePath);
    foreach (UnitInfo unit in GlobalConstants.UnitsData.Values.OrderBy(u => u.Types.FirstOrDefault() ?? "").ThenBy(u => u.Name))
    {
      // Unfinished units without a sprite or scene can't be placed
      if (unit.Texture == null || !ResourceLoader.Exists(unit.ScenePath))
        continue;
      UnitGui row = rowScene.Instantiate<UnitGui>();
      row.Info = unit;
      row.Infinite = true;
      _unitList.AddChild(row);
    }
  }

  private void FilterUnits()
  {
    string text = _search.Text.Trim();
    foreach (UnitGui row in _unitList.GetChildren().Cast<UnitGui>())
    {
      UnitInfo unit = row.Info;
      row.Visible = new[] { unit.Name, unit.Description, unit.Rarity }.Concat(unit.Types)
        .Any(t => t.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
  }

  private void FillBrushes()
  {
    ButtonGroup terrainGroup = new();
    foreach (TerrainInfo terrain in GlobalConstants.TerrainsData.Values.Where(t => t.Texture != null))
      AddBrush(_terrainList, terrainGroup, terrain.Name, terrain.Texture, terrain.Description, () => _terrainBrush = terrain);

    ButtonGroup propGroup = new();
    foreach (PropInfo prop in GlobalConstants.PropsData.Values.Where(p => p.Texture != null))
      AddBrush(_propList, propGroup, prop.Name, prop.Texture, prop.Description, () => _propBrush = prop);

    // The first of each list is selected
    _terrainBrush = GlobalConstants.TerrainsData.Values.First(t => t.Texture != null);
    _propBrush = GlobalConstants.PropsData.Values.First(p => p.Texture != null);
  }

  private static void AddBrush(VBoxContainer list, ButtonGroup group, string name, Texture2D icon, string description, Action onSelected)
  {
    Button button = new Button
    {
      Text = name,
      Icon = icon,
      TooltipText = description,
      Alignment = HorizontalAlignment.Left,
      ToggleMode = true,
      ButtonGroup = group,
      ButtonPressed = list.GetChildCount() == 0,
      CustomMinimumSize = new Vector2(0, 38),
    };
    button.Pressed += onSelected;
    list.AddChild(button);
  }

  // ---- Drawing on the board

  private void OnBoardInput(InputEvent @event)
  {
    if (!Visible || @event is not InputEventMouse mouse)
      return;

    Vector2I cell = _gridOverlay.GetCellUnderMouse(mouse.Position);
    if (_tool == Tool.Units)
    {
      if (mouse is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } && GlobalFunctions.IsCellInsideGrid(cell))
        RemoveUnit(cell);
      return;
    }

    bool draw = mouse.ButtonMask.HasFlag(MouseButtonMask.Left);
    bool erase = mouse.ButtonMask.HasFlag(MouseButtonMask.Right);
    if (!draw && !erase)
    {
      _lastPaintedCell = null;
      return;
    }

    // Mouse events can skip cells in a fast stroke, so everything on the line since the last one is painted
    Vector2I from = _lastPaintedCell ?? cell;
    int steps = Mathf.Max(1, Mathf.Max(Mathf.Abs(cell.X - from.X), Mathf.Abs(cell.Y - from.Y)));
    for (int i = 0; i <= steps; i++)
    {
      foreach (Vector2I target in GetBrushCells((Vector2I)((Vector2)from).Lerp(cell, (float)i / steps).Round()))
      {
        if (draw)
          Draw(target);
        else
          Erase(target);
      }
    }
    _lastPaintedCell = cell;
  }

  private void RemoveUnit(Vector2I cell)
  {
    Unit unit = _board.UnitsGrid[cell.X, cell.Y];
    if (unit != null)
      _gridOverlay.OnUnitPlacedSuccessfully(new DragPayload(unit.GetStartInfo(), _gridOverlay, unit.OccupiedMainCell));
  }

  private IEnumerable<Vector2I> GetBrushCells(Vector2I center)
  {
    int radius = BrushSizes[_brushSizeButton.Selected] / 2;
    for (int x = -radius; x <= radius; x++)
    {
      for (int y = -radius; y <= radius; y++)
      {
        Vector2I cell = center + new Vector2I(x, y);
        if (GlobalFunctions.IsCellInsideGrid(cell))
          yield return cell;
      }
    }
  }

  // Cells where the brush does not fit the game's own placement rules stay as they are
  private void Draw(Vector2I cell)
  {
    Unit unit = _board.UnitsGrid[cell.X, cell.Y];
    Prop prop = _board.PropsGrid[cell.X, cell.Y];

    if (_tool == Tool.Terrain && _terrainBrush != null)
    {
      if (_terrainBrush.Blocking && unit != null)
      {
        _status.Text = $"{unit.DisplayName} can't stand on {_terrainBrush.Name}.";
        return;
      }
      _board.SetTerrain(cell, _terrainBrush);
      if (prop != null && !GlobalFunctions.AreTypesCompatible(prop.GetInfo(), _terrainBrush))
        _board.SetProp(cell, null);
    }
    else if (_tool == Tool.Props && _propBrush != null)
    {
      TerrainInfo terrain = _board.TerrainGrid[cell.X, cell.Y].GetInfo();
      if (_propBrush.Blocking && unit != null)
        _status.Text = $"{_propBrush.Name} can't share a tile with {unit.DisplayName}.";
      else if (!GlobalFunctions.AreTypesCompatible(_propBrush, terrain))
        _status.Text = $"{_propBrush.Name} can't be placed on {terrain.Name}.";
      else
        _board.SetProp(cell, _propBrush);
    }
  }

  private void Erase(Vector2I cell)
  {
    if (_tool == Tool.Terrain)
      _board.SetTerrain(cell, GlobalConstants.TerrainsData[_baseTerrain]);
    else
      _board.SetProp(cell, null);
  }

  private void DrawBrushPreview()
  {
    Vector2I cell = _gridOverlay.GetCellUnderMouse(_brushPreview.GetGlobalMousePosition());
    if (!GlobalFunctions.IsCellInsideGrid(cell))
      return;

    Vector2 tileSize = new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize);
    foreach (var edge in OverlayLayer.GetOutlineEdges(GetBrushCells(cell), CombatFx.CellCenter, tileSize))
      _brushPreview.DrawLine(edge.Item1, edge.Item2, GamePalette.Gold, 2f);
  }

  // Dropping a placed unit anywhere on the panel takes it off the board
  public override bool _CanDropData(Vector2 atPosition, Variant data)
  {
    return data.Obj is DragPayload { Source: GridOverlay };
  }

  public override void _DropData(Vector2 atPosition, Variant data)
  {
    if (data.Obj is DragPayload { Source: GridOverlay grid } payload)
      grid.OnUnitPlacedSuccessfully(payload);
  }
}
