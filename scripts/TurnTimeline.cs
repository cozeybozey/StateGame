using Godot;
using System.Collections.Generic;
using System.Linq;

// Strip of unit icons along the top border of the board showing who acts next, in order. The globe is the world's own action.
// Hovering an icon outlines that unit on the board; hovering a unit on the board highlights its icons.
public partial class TurnTimeline : Node2D
{
  public struct PredictedAction
  {
    public Unit? Unit;        // Null for the world's action
    public int Tick;          // Ticks from now (0 = winding up right now)
    public bool InProgress;   // Unit is telegraphing this action right now
  }

  private const int SlotCount = 16;   // One slot per board column
  private const float IconSize = 26f;

  private List<Unit> _units = new();
  private Unit[,]? _unitsGrid = null;
  private IReadOnlyList<Unit>? _actionQueue = null;
  private List<PredictedAction> _actions = new();
  private int _worldTicksLeft = 0;
  private int _hoveredSlot = -1;
  private Unit? _hoveredUnit = null;

  public bool WorldHovered => _hoveredSlot >= 0 && _hoveredSlot < _actions.Count && _actions[_hoveredSlot].Unit == null;

  private static readonly Color PlayerColor = GamePalette.Player;
  private static readonly Color EnemyColor = GamePalette.Enemy;

  public override void _EnterTree()
  {
    ZIndex = 40;
  }

  public void SetData(List<Unit> units, Unit[,] unitsGrid, IReadOnlyList<Unit> actionQueue, int worldTicksLeft)
  {
    _units = units;
    _unitsGrid = unitsGrid;
    _actionQueue = actionQueue;
    _worldTicksLeft = worldTicksLeft;
  }

  // Predict the next `count` actions. `units` must be in turn order (World.SortUnits): units that fill
  // their bar on the same tick act in list order, which is exactly what the simulation does.
  // `queue` is World's same-tick action queue: those units act first, in queue order. A queued unit that
  // has not started yet still holds the full bar for that action, so its regular prediction starts one action later.
  public static List<PredictedAction> Predict(List<Unit> units, int count, IReadOnlyList<Unit>? queue = null)
  {
    List<PredictedAction> result = new();
    List<Unit> alive = units.Where(u => u != null && GodotObject.IsInstanceValid(u) && u.Health > 0).ToList();
    HashSet<Unit> queuedNotStarted = new();

    if (queue != null)
    {
      foreach (Unit unit in queue)
      {
        if (!alive.Contains(unit))
          continue;
        result.Add(new PredictedAction { Unit = unit, Tick = 0, InProgress = unit.IsTelegraphing });
        if (!unit.IsTelegraphing)
          queuedNotStarted.Add(unit);
      }
    }

    // Units winding up outside the queue strike next (they wait while the queue runs)
    foreach (Unit unit in alive)
    {
      if (unit.IsTelegraphing && (queue == null || !queue.Contains(unit)))
        result.Add(new PredictedAction { Unit = unit, Tick = 0, InProgress = true });
    }

    // Each unit contributes its next few actions; merge them by tick, ties by turn order
    List<(PredictedAction action, int order)> upcoming = new();
    for (int i = 0; i < alive.Count; i++)
    {
      Unit unit = alive[i];
      if (unit.Speed <= 0)
        continue;
      int firstK = queuedNotStarted.Contains(unit) ? 2 : 1;
      for (int k = firstK; k < firstK + count; k++)
      {
        int tick = unit.TicksUntilAction(k);
        upcoming.Add((new PredictedAction { Unit = unit, Tick = tick, InProgress = false }, i));
      }
    }

    result.AddRange(upcoming.OrderBy(a => a.action.Tick).ThenBy(a => a.order).Select(a => a.action));
    return result.Take(count).ToList();
  }

  // The world acts at the end of a tick, so it comes after units acting on that same tick
  private void InsertWorldActions()
  {
    if (_actions.Count == 0)
      return;

    int lastTick = _actions[^1].Tick;
    for (int tick = _worldTicksLeft; tick <= lastTick; tick += GlobalConstants.TicksPerWorldAction)
    {
      int index = _actions.FindIndex(a => a.Tick > tick);
      _actions.Insert(index < 0 ? _actions.Count : index, new PredictedAction { Unit = null, Tick = tick, InProgress = false });
    }
    _actions = _actions.Take(SlotCount).ToList();
  }

  public override void _Process(double delta)
  {
    if (!Visible)
      return;

    _actions = Predict(_units, SlotCount, _actionQueue);
    InsertWorldActions();

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

  // Largest rect with the texture's proportions, centered in the slot (wide units keep their shape)
  private static Rect2 FitInside(Vector2 textureSize, Rect2 slot)
  {
    float scale = Mathf.Min(slot.Size.X / textureSize.X, slot.Size.Y / textureSize.Y);
    Vector2 size = textureSize * scale;
    return new Rect2(slot.GetCenter() - size / 2, size);
  }

  private void DrawWorldSlot(Rect2 rect, bool hovered, bool dimmed)
  {
    Color color = new Color(GamePalette.World, dimmed ? 0.4f : 1f);
    DrawStyleBox(GamePalette.Box(GamePalette.World.Darkened(0.75f), hovered ? Colors.White : color, hovered ? 3 : 2, 5), rect);

    // Globe: outline, equator and two meridians
    Vector2 center = rect.GetCenter();
    const float radius = 7f;
    DrawArc(center, radius, 0, Mathf.Tau, 24, color, 1.2f, true);
    DrawLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), color, 1.2f, true);
    DrawLine(center - new Vector2(0, radius), center + new Vector2(0, radius), color, 1.2f, true);
    Vector2[] meridian = new Vector2[25];
    for (int i = 0; i < meridian.Length; i++)
    {
      float angle = Mathf.Tau * i / (meridian.Length - 1);
      meridian[i] = center + new Vector2(Mathf.Cos(angle) * radius * 0.5f, Mathf.Sin(angle) * radius);
    }
    DrawPolyline(meridian, color, 1.2f, true);
  }

  public override void _Draw()
  {
    // Arrow pointing at the first slot
    if (_actions.Count > 0)
    {
      float y = -GlobalConstants.TileSize * 0.5f;
      DrawColoredPolygon([new Vector2(-12, y - 6), new Vector2(-3, y), new Vector2(-12, y + 6)], GamePalette.Gold);
    }

    for (int i = 0; i < _actions.Count; i++)
    {
      PredictedAction action = _actions[i];
      Rect2 rect = SlotRect(i);
      if (action.Unit == null)
      {
        DrawWorldSlot(rect, i == _hoveredSlot, _hoveredUnit != null);
        continue;
      }

      Unit unit = action.Unit;
      if (!IsInstanceValid(unit))
        continue;

      Color sideColor = unit.Side ? PlayerColor : EnemyColor;
      bool highlighted = _hoveredUnit != null && unit == _hoveredUnit;
      bool dimmed = (_hoveredUnit != null || WorldHovered) && !highlighted;

      // Border: side color; yellow while that unit is winding up; white when linked to the hovered unit
      Color border = highlighted ? Colors.White : action.InProgress ? GamePalette.Gold : sideColor;
      int borderWidth = highlighted || action.InProgress ? 3 : 2;
      DrawStyleBox(GamePalette.Box(new Color(sideColor.Darkened(0.65f), dimmed ? 0.5f : 1f), new Color(border, dimmed ? 0.5f : 1f), borderWidth, 5), rect);
      if (unit.Texture != null)
        DrawTextureRect(unit.Texture, FitInside(unit.Texture.GetSize(), rect.Grow(-4)), false, new Color(1, 1, 1, dimmed ? 0.35f : 1f));
    }

    // Outline the hovered unit on the board
    if (_hoveredUnit != null && IsInstanceValid(_hoveredUnit))
    {
      Vector2 tileSize = new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize);
      foreach (var edge in OverlayLayer.GetOutlineEdges(_hoveredUnit.GetOccupiedCells(), CombatFx.CellCenter, tileSize))
        DrawLine(edge.Item1, edge.Item2, Colors.White, 2f);
    }
  }
}
