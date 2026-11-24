using Godot;
using System;

// The clicked cell: outlines it on the board and shows what is on it in the info panel
public partial class BoardSelection : Node
{
  private Board _board = null!;
  private GridOverlay _gridOverlay = null!;
  private OverlayLayer _layer = null!;
  private InfoGui _infoGui = null!;

  public void Initialize(Board board, GridOverlay gridOverlay, OverlayLayer layer, InfoGui infoGui)
  {
    _board = board;
    _gridOverlay = gridOverlay;
    _layer = layer;
    _infoGui = infoGui;

    _layer.OutlineColor = GamePalette.Gold;
    _layer.HighlightColor = new Color(1, 1, 1, 0.15f);

    _board.UnitRemoved += OnUnitRemoved;
    _board.UnitChanged += OnUnitChanged;
    _board.CellChanged += OnCellChanged;
  }

  public override void _Input(InputEvent @event)
  {
    if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse)
      return;

    // Only clicks on the board itself change the selection; UI above it handles its own clicks
    Control hovered = GetViewport().GuiGetHoveredControl();
    if (hovered != null && hovered != _gridOverlay)
      return;

    Vector2I cell = _gridOverlay.GetCellUnderMouse(mouse.Position) - new Vector2I(1, 1);
    if (GlobalFunctions.IsCellInsideGrid(cell))
      Select(cell);
    else
      Deselect();
  }

  private void Select(Vector2I cell)
  {
    ShowInfo(cell);
    Unit unit = _board.UnitsGrid[cell.X, cell.Y];
    _layer.Clear();
    _layer.ShowCells(unit != null ? unit.GetOccupiedCells() : [cell]);
  }

  private void Deselect()
  {
    _infoGui.Reset();
    _layer.Clear();
  }

  private void ShowInfo(Vector2I cell)
  {
    _infoGui.SetSelectedInfo(_board.UnitsGrid[cell.X, cell.Y], _board.TerrainGrid[cell.X, cell.Y], _board.PropsGrid[cell.X, cell.Y], cell);
  }

  private void OnUnitRemoved(Unit unit)
  {
    if (_infoGui.GetSelectedUnit() == unit)
      Deselect();
  }

  // The selection follows a unit that moves or grows
  private void OnUnitChanged(Unit unit)
  {
    if (_infoGui.GetSelectedUnit() != unit)
      return;

    Vector2I cell = unit.OccupiedMainCell;
    _layer.Clear();
    _layer.ShowCells(unit.GetOccupiedCells());
    _infoGui.SetSelectedInfo(unit, _board.TerrainGrid[cell.X, cell.Y], _board.PropsGrid[cell.X, cell.Y], cell);
  }

  private void OnCellChanged(Vector2I cell)
  {
    if (_infoGui.GetSelectedCell() == cell)
      ShowInfo(cell);
  }
}
