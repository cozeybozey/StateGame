using Godot;
using System.Collections.Generic;

// Shows the dragged unit above all other UI, and marks the cells it would occupy: green when it can be
// placed there, red when it cannot. Replaces the mouse cursor during a drag, so the operating system's
// "forbidden" cursor never shows.
public partial class UnitDragPreview : CanvasLayer
{
  private GridOverlay _grid = null!;
  private Control _canvas = null!;
  private DragPayload? _payload = null;
  private bool _cursorHidden = false;

  public override void _Ready()
  {
    Layer = 100;
    _grid = GetTree().CurrentScene.GetNode<GridOverlay>("GridOverlay");
    _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
    _canvas.Draw += OnDraw;
    AddChild(_canvas);
  }

  public override void _Process(double delta)
  {
    Viewport viewport = GetViewport();
    _payload = viewport.GuiIsDragging() ? viewport.GuiGetDragData().Obj as DragPayload : null;
    bool dragging = _payload != null;

    if (dragging != _cursorHidden)
    {
      Input.MouseMode = dragging ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
      _cursorHidden = dragging;
    }
    _canvas.QueueRedraw();
  }

  private void OnDraw()
  {
    if (_payload == null)
      return;

    float tile = GlobalConstants.TileSize;
    Vector2 mouse = _canvas.GetLocalMousePosition();
    Vector2 boardOrigin = _grid.GetGlobalTransformWithCanvas().Origin;
    Vector2I cell = _grid.GetCellUnderMouse(mouse - boardOrigin);
    bool overBoard = GlobalFunctions.IsCellInsideGrid(cell);
    bool valid = overBoard && _grid.CanPlace(_payload, cell);

    List<Vector2I> cells = _payload.Unit.OccupiedCells;
    Vector2 topLeft = mouse - new Vector2(tile, tile) / 2;
    if (overBoard)
    {
      topLeft = boardOrigin + (Vector2)cell * tile;
      Color color = valid ? GamePalette.Support : GamePalette.Enemy;
      foreach (Vector2I offset in cells)
      {
        if (GlobalFunctions.IsCellInsideGrid(cell + offset))
          _canvas.DrawRect(new Rect2(topLeft + (Vector2)offset * tile, new Vector2(tile, tile)), new Color(color, 0.35f));
      }
      foreach (var edge in OverlayLayer.GetOutlineEdges(cells, c => topLeft + ((Vector2)c + new Vector2(0.5f, 0.5f)) * tile, new Vector2(tile, tile)))
        _canvas.DrawLine(edge.Item1, edge.Item2, color, 2f);
    }

    Texture2D texture = _payload.Unit.Texture;
    if (texture == null)
      return;

    // CellToGlobalPosition of cell (0, 0) is the sprite's center relative to the main cell's top-left corner
    Vector2I dimensions = GlobalFunctions.CellsToDimensions(cells);
    Vector2 center = topLeft + (Vector2)GlobalFunctions.CellToGlobalPosition(Vector2I.Zero, dimensions.X, dimensions.Y, GlobalFunctions.GetRelPosInCells(cells, cells[0]));
    Color modulate = overBoard && !valid ? new Color(1f, 0.55f, 0.5f, 0.75f) : new Color(1, 1, 1, 0.9f);
    _canvas.DrawTexture(texture, center - texture.GetSize() / 2, modulate);
  }
}
