using Godot;
using System;

public partial class InfoGui : VBoxContainer
{
  MenuButton _menuButton = null!;
  PopupMenu _popupMenu = null!;
  UnitsInfoGui _unitsInfoContainer = null!;
  TerrainsInfoGui _terrainsInfoContainer = null!;
  private GlobalSignals _globalSignals = null!;
  PropsInfoGui _propsInfoContainer = null!;
  Vector2I _selectedCell = new Vector2I(0, 0);

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _menuButton = GetNode<MenuButton>("InfoTypeButton");
    _popupMenu = _menuButton.GetPopup();
    _popupMenu.IdPressed += OnInfoTypeSelected;
    _unitsInfoContainer = GetNode<UnitsInfoGui>("UnitsInfoContainer");
    _terrainsInfoContainer = GetNode<TerrainsInfoGui>("TerrainsInfoContainer");
    _propsInfoContainer = GetNode<PropsInfoGui>("PropsInfoContainer"); 
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    _globalSignals.UnitInfoSelected += OnUnitInfoSelected;
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
  }

  private void OnUnitInfoSelected(UnitInfo unitInfo)
  {
    _unitsInfoContainer.ResetSelectedUnit();
    _unitsInfoContainer.DisplayInfo(unitInfo);
    _unitsInfoContainer.ShowInfo();
    Show();
  }

  private void ShowUnitInfo()
  {
    _unitsInfoContainer.Show();
    _terrainsInfoContainer.Hide();
    _propsInfoContainer.Hide();
    _menuButton.Text = "Unit Info";
  }

  private void ShowTerrainInfo()
  {
    _unitsInfoContainer.Hide();
    _terrainsInfoContainer.Show();
    _propsInfoContainer.Hide();
    _menuButton.Text = "Terrain Info";
  }

  private void ShowPropInfo()
  {
    _unitsInfoContainer.Hide();
    _terrainsInfoContainer.Hide();
    _propsInfoContainer.Show();
    _menuButton.Text = "Prop Info";

  }

  private void OnInfoTypeSelected(long id)
  {
    switch (id)
    {
      case 0:
        ShowUnitInfo();
        break;
      case 1:
        ShowTerrainInfo();
        break;
      case 2:
        ShowPropInfo();
        break;
    }
  }

  public void Reset()
  {
    _unitsInfoContainer.ResetSelectedUnit();
    _terrainsInfoContainer.ResetSelectedTerrain();
    _propsInfoContainer.ResetSelectedProp();
    Hide();
  }

  public void SetSelectedInfo(Unit? unit, Terrain? terrain, Prop? prop, Vector2I cell)
  {
    _unitsInfoContainer.SetSelectedUnit(unit);
    _terrainsInfoContainer.SetSelectedTerrain(terrain);
    _propsInfoContainer.SetSelectedProp(prop);
    _selectedCell = cell;

    // Show unit with highest priority
    if (unit != null)
      ShowUnitInfo();
    else if (prop != null)
      ShowPropInfo();
    else
      ShowTerrainInfo();

    Show();
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
