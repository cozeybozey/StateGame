using Godot;
using System;

public partial class InfoGui : PanelContainer
{
  private Button _unitTab = null!;
  private Button _terrainTab = null!;
  private Button _propTab = null!;
  private HBoxContainer _tabs = null!;
  private Label _placeholder = null!;
  private ScrollContainer _scroll = null!;
  private UnitsInfoGui _unitsInfoContainer = null!;
  private TerrainsInfoGui _terrainsInfoContainer = null!;
  private PropsInfoGui _propsInfoContainer = null!;
  private GlobalSignals _globalSignals = null!;
  private Vector2I _selectedCell = new Vector2I(0, 0);

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _tabs = GetNode<HBoxContainer>("Content/Tabs");
    _unitTab = GetNode<Button>("Content/Tabs/UnitTab");
    _terrainTab = GetNode<Button>("Content/Tabs/TerrainTab");
    _propTab = GetNode<Button>("Content/Tabs/PropTab");
    _placeholder = GetNode<Label>("Content/Placeholder");
    _scroll = GetNode<ScrollContainer>("Content/Scroll");
    _unitsInfoContainer = GetNode<UnitsInfoGui>("Content/Scroll/VBoxContainer/UnitsInfoContainer");
    _terrainsInfoContainer = GetNode<TerrainsInfoGui>("Content/Scroll/VBoxContainer/TerrainsInfoContainer");
    _propsInfoContainer = GetNode<PropsInfoGui>("Content/Scroll/VBoxContainer/PropsInfoContainer");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    ButtonGroup group = new ButtonGroup();
    foreach (Button tab in new[] { _unitTab, _terrainTab, _propTab })
      tab.ButtonGroup = group;
    _unitTab.Pressed += () => ShowTab(_unitTab);
    _terrainTab.Pressed += () => ShowTab(_terrainTab);
    _propTab.Pressed += () => ShowTab(_propTab);

    _globalSignals.UnitInfoSelected += OnUnitInfoSelected;
    ShowContent(false);
  }

  private void OnUnitInfoSelected(UnitInfo unitInfo)
  {
    _unitsInfoContainer.ResetSelectedUnit();
    _unitsInfoContainer.DisplayInfo(unitInfo);
    _unitsInfoContainer.ShowInfo();
    _terrainsInfoContainer.ResetSelectedTerrain();
    _propsInfoContainer.ResetSelectedProp();
    _unitTab.Disabled = false;
    _terrainTab.Disabled = true;
    _propTab.Disabled = true;
    ShowTab(_unitTab);
    ShowContent(true);
  }

  private void ShowTab(Button tab)
  {
    tab.ButtonPressed = true;
    _unitsInfoContainer.Visible = tab == _unitTab;
    _terrainsInfoContainer.Visible = tab == _terrainTab;
    _propsInfoContainer.Visible = tab == _propTab;
    _scroll.ScrollVertical = 0;
  }

  // Without a selection the panel stays in place and shows a hint, so the layout never jumps
  private void ShowContent(bool show)
  {
    _tabs.Visible = show;
    _scroll.Visible = show;
    _placeholder.Visible = !show;
  }

  public void Reset()
  {
    _unitsInfoContainer.ResetSelectedUnit();
    _terrainsInfoContainer.ResetSelectedTerrain();
    _propsInfoContainer.ResetSelectedProp();
    ShowContent(false);
  }

  public void SetSelectedInfo(Unit? unit, Terrain? terrain, Prop? prop, Vector2I cell)
  {
    _unitsInfoContainer.SetSelectedUnit(unit);
    _terrainsInfoContainer.SetSelectedTerrain(terrain);
    _propsInfoContainer.SetSelectedProp(prop);
    _selectedCell = cell;

    _unitTab.Disabled = unit == null;
    _terrainTab.Disabled = terrain == null;
    _propTab.Disabled = prop == null;

    // Show the most specific thing on the cell
    if (unit != null)
      ShowTab(_unitTab);
    else if (prop != null)
      ShowTab(_propTab);
    else
      ShowTab(_terrainTab);

    ShowContent(unit != null || prop != null || terrain != null);
  }

  public Unit? GetSelectedUnit()
  {
    return _unitsInfoContainer.SelectedUnit;
  }

  public Vector2I GetSelectedCell()
  {
    return _selectedCell;
  }
}
