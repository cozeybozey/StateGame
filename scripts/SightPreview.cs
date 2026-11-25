using Godot;
using System.Collections.Generic;

// While placing units: darkens the cells on the other half that the dragged or hovered unit cannot see,
// and outlines what it would attack first.
public partial class SightPreview : Node2D
{
  private Board _board = null!;
  private GridOverlay _grid = null!;
  private UnitInfo? _unit = null;
  private Vector2I _cell;
  private bool _side;

  public void Initialize(Board board, GridOverlay grid)
  {
    _board = board;
    _grid = grid;
    ZIndex = 15;
  }

  public override void _Process(double delta)
  {
    FindUnit();
    QueueRedraw();
  }

  private void FindUnit()
  {
    _unit = null;
    if (_board.InBattle || !_grid.IsVisibleInTree())
      return;

    Control hovered = GetViewport().GuiGetHoveredControl();
    Viewport viewport = GetViewport();
    Vector2I cell = _grid.GetCellUnderMouse(_grid.GetLocalMousePosition());

    if (viewport.GuiIsDragging())
    {
      if (viewport.GuiGetDragData().Obj is not DragPayload payload || !GlobalFunctions.IsCellInsideGrid(cell) || !_grid.CanPlace(payload, cell))
        return;
      _unit = payload.Unit;
      _cell = cell;
      _side = !_grid.EditsEnemySide;
    }
    else if ((hovered == null || hovered == _grid) && GlobalFunctions.IsCellInsideGrid(cell))
    {
      Unit unit = _board.UnitsGrid[cell.X, cell.Y];
      if (unit == null || !IsInstanceValid(unit))
        return;
      _unit = unit.GetStartInfo();
      _cell = unit.OccupiedMainCell;
      _side = unit.Side;
    }

    if (_unit is not { NeedsSight: true })
      _unit = null;
  }

  public override void _Draw()
  {
    if (_unit == null)
      return;

    List<Vector2I> cells = _unit.OccupiedCells.ConvertAll(offset => _cell + offset);
    Vector2 tile = new(GlobalConstants.TileSize, GlobalConstants.TileSize);

    foreach (Vector2I cell in Board.Cells)
    {
      bool otherHalf = cell.Y < GlobalConstants.GridSize.Y / 2 == _side;
      if (otherHalf && !LineOfSight.CanSee(cells, cell, _board.TerrainGrid, _board.PropsGrid))
        DrawRect(new Rect2(CombatFx.CellCenter(cell) - tile / 2, tile), new Color(0, 0, 0, 0.5f));
    }

    LineOfSight.Target? target = FindTarget(cells);
    if (target == null)
      return;

    Unit targetUnit = _board.UnitsGrid[target.Cell.X, target.Cell.Y];
    List<Vector2I> targetCells = targetUnit != null && !target.Obstacle ? targetUnit.GetOccupiedCells() : [target.Cell];
    foreach (var edge in OverlayLayer.GetOutlineEdges(targetCells, CombatFx.CellCenter, tile))
      DrawLine(edge.Item1, edge.Item2, GamePalette.Enemy, 3f);
  }

  private LineOfSight.Target? FindTarget(List<Vector2I> cells)
  {
    return _unit!.Id == "sniper"
      ? LineOfSight.FindFarthest(cells, _side, _board.Units, _board.TerrainGrid, _board.PropsGrid)
      : LineOfSight.FindFrontmost(cells, _side, _board.Units, _board.TerrainGrid, _board.PropsGrid);
  }
}
