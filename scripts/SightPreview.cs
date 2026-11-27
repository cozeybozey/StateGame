using Godot;
using System.Collections.Generic;
using System.Linq;

// While placing units: darkens the cells on the other half that the dragged or hovered unit cannot reach,
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

    if (_unit is not { Targeting: not "" })
      _unit = null;
  }

  public override void _Draw()
  {
    if (_unit == null)
      return;

    List<Vector2I> cells = _unit.OccupiedCells.ConvertAll(offset => _cell + offset);
    HashSet<Vector2I> reach = GetReach(cells);
    Vector2 tile = new(GlobalConstants.TileSize, GlobalConstants.TileSize);

    foreach (Vector2I cell in Board.Cells)
    {
      bool otherHalf = cell.Y < GlobalConstants.GridSize.Y / 2 == _side;
      if (otherHalf && !reach.Contains(cell))
        DrawRect(new Rect2(CombatFx.CellCenter(cell) - tile / 2, tile), new Color(0, 0, 0, 0.5f));
    }

    foreach (List<Vector2I> target in GetTargets(cells, reach))
    {
      foreach (var edge in OverlayLayer.GetOutlineEdges(target, CombatFx.CellCenter, tile))
        DrawLine(edge.Item1, edge.Item2, GamePalette.Enemy, 3f);
    }
  }

  // The cells this unit can hit: what it sees, or only the cells its beam or column passes
  private HashSet<Vector2I> GetReach(List<Vector2I> cells)
  {
    Terrain[,] terrain = _board.TerrainGrid;
    Prop[,] props = _board.PropsGrid;
    IEnumerable<Vector2I> reach = _unit!.Targeting switch
    {
      "column" => cells.SelectMany(cell => LineOfSight.GetColumn(cell, _side, terrain, props)),
      "beam" => new[] { _cell, _cell + Vector2I.Right }.SelectMany(cell => LineOfSight.GetColumn(cell, _side, terrain, props)),
      "rows" => cells.SelectMany(cell => Enumerable.Range(0, GlobalConstants.GridSize.X)
        .Select(x => new Vector2I(x, GlobalConstants.GridSize.Y - cell.Y - 1))),
      _ => Board.Cells.Where(cell => LineOfSight.CanSee(cells, cell, terrain, props)),
    };
    return reach.ToHashSet();
  }

  // The cells of each enemy or prop that the next action would hit
  private List<List<Vector2I>> GetTargets(List<Vector2I> cells, HashSet<Vector2I> reach)
  {
    Terrain[,] terrain = _board.TerrainGrid;
    Prop[,] props = _board.PropsGrid;
    LineOfSight.Target? target = _unit!.Targeting switch
    {
      "frontmost" => LineOfSight.FindFrontmost(cells, _side, _board.Units, terrain, props),
      "farthest" => LineOfSight.FindFarthest(cells, _side, _board.Units, terrain, props),
      "column" => LineOfSight.FindInColumn(cells, _side, _board.UnitsGrid, terrain, props),
      _ => null,
    };

    if (target != null)
      return [CellsOf(target.Cell, target.Obstacle)];

    // Beams and rows hit every enemy they reach
    return _board.Units
      .Where(u => IsInstanceValid(u) && u.Side != _side && u.GetOccupiedCells().Any(reach.Contains))
      .Select(u => u.GetOccupiedCells())
      .ToList();
  }

  private List<Vector2I> CellsOf(Vector2I cell, bool obstacle)
  {
    Unit unit = _board.UnitsGrid[cell.X, cell.Y];
    return unit != null && !obstacle ? unit.GetOccupiedCells() : [cell];
  }
}
