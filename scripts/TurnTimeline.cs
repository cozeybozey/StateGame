using Godot;
using System.Collections.Generic;
using System.Linq;

// Strip of unit icons along the top border of the board showing who acts next, in order.
// Hovering an icon outlines that unit on the board; hovering a unit on the board highlights its icons.
public partial class TurnTimeline : Node2D
{
  public struct PredictedAction
  {
    public Unit Unit;
    public int Tick;          // Ticks from now (0 = winding up right now)
    public bool InProgress;   // Unit is telegraphing this action right now
  }

  private const int SlotCount = 16;   // One slot per board column
  private const float IconSize = 26f;

  private List<Unit> _units = new();
  private Unit[,]? _unitsGrid = null;
  private List<PredictedAction> _actions = new();
  private int _hoveredSlot = -1;
  private Unit? _hoveredUnit = null;

  private static readonly Color PlayerColor = new Color(0.15f, 0.45f, 0.85f);
  private static readonly Color EnemyColor = new Color(0.8f, 0.2f, 0.15f);

  public override void _EnterTree()
  {
    ZIndex = 40;
  }

  public void SetData(List<Unit> units, Unit[,] unitsGrid)
  {
    _units = units;
    _unitsGrid = unitsGrid;
  }

  // Predict the next `count` actions. `units` must be in turn order (World.SortUnits): units that fill
  // their bar on the same tick act in list order, which is exactly what the simulation does.
  public static List<PredictedAction> Predict(List<Unit> units, int count)
  {
    List<PredictedAction> result = new();
    List<Unit> alive = units.Where(u => u != null && GodotObject.IsInstanceValid(u) && u.Health > 0).ToList();

    foreach (Unit unit in alive)
    {
      if (unit.IsTelegraphing)
        result.Add(new PredictedAction { Unit = unit, Tick = 0, InProgress = true });
    }

    // Each unit contributes its next few actions; merge them by tick, ties by turn order
    List<(PredictedAction action, int order)> upcoming = new();
    for (int i = 0; i < alive.Count; i++)
    {
      Unit unit = alive[i];
      if (unit.Speed <= 0)
        continue;
      for (int k = 1; k <= count; k++)
      {
        int tick = unit.TicksUntilAction(k);
        upcoming.Add((new PredictedAction { Unit = unit, Tick = tick, InProgress = false }, i));
      }
    }

    result.AddRange(upcoming.OrderBy(a => a.action.Tick).ThenBy(a => a.order).Select(a => a.action));
    return result.Take(count).ToList();
  }

  public override void _Process(double delta)
  {
    if (!Visible)
      return;

    _actions = Predict(_units, SlotCount);

    // Hover detection (world coordinates, same space as the board)
    Vector2 mouse = GetGlobalMousePosition();
    _hoveredSlot = -1;
    _hoveredUnit = null;
    for (int i = 0; i < _actions.Count; i++)
    {
      if (SlotRect(i).Grow(2).HasPoint(mouse))
      {
        _hoveredSlot = i;
        _hoveredUnit = _actions[i].Unit;
        break;
      }
    }
    if (_hoveredUnit == null && _unitsGrid != null)
    {
      Vector2I cell = new Vector2I(Mathf.FloorToInt(mouse.X / GlobalConstants.TileSize), Mathf.FloorToInt(mouse.Y / GlobalConstants.TileSize));
      if (GlobalFunctions.IsCellInsideGrid(cell))
        _hoveredUnit = _unitsGrid[cell.X, cell.Y];
    }

    QueueRedraw();
  }

  // Slots sit on the board's top border row (just above grid row 0), one per column
  private static Rect2 SlotRect(int i)
  {
    float tile = GlobalConstants.TileSize;
    Vector2 center = new Vector2(tile * (0.5f + i), -tile * 0.5f);
    return new Rect2(center - new Vector2(IconSize, IconSize) / 2, new Vector2(IconSize, IconSize));
  }

  public override void _Draw()
  {
    for (int i = 0; i < _actions.Count; i++)
    {
      PredictedAction action = _actions[i];
      Unit unit = action.Unit;
      if (!IsInstanceValid(unit))
        continue;

      Rect2 rect = SlotRect(i);
      Color sideColor = unit.Side ? PlayerColor : EnemyColor;
      bool highlighted = _hoveredUnit != null && unit == _hoveredUnit;
      bool dimmed = _hoveredUnit != null && !highlighted;

      DrawRect(rect, new Color(sideColor.Darkened(0.55f), dimmed ? 0.5f : 1f));
      if (unit.Texture != null)
        DrawTextureRect(unit.Texture, rect.Grow(-3), false, new Color(1, 1, 1, dimmed ? 0.35f : 1f));

      // Border: side color; yellow while that unit is winding up; white when linked to the hovered unit
      Color border = highlighted ? Colors.White : action.InProgress ? new Color(1f, 0.85f, 0.2f) : sideColor;
      DrawRect(rect, border, false, highlighted || action.InProgress ? 2.5f : 1.5f);
    }

    // Outline the hovered unit on the board
    if (_hoveredUnit != null && IsInstanceValid(_hoveredUnit))
    {
      foreach (Vector2I cell in _hoveredUnit.GetOccupiedCells())
      {
        Rect2 cellRect = new Rect2(cell * GlobalConstants.TileSize, new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize));
        DrawRect(cellRect.Grow(-1), Colors.White, false, 2f);
      }
    }
  }
}
