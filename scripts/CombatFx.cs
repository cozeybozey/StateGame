using Godot;
using System.Collections.Generic;
using System.Linq;

// Draws short-lived combat visuals: telegraph lines (unit is about to act),
// strike lines (unit acts) and fading highlights on the targeted cells.
// Everything is drawn procedurally with Line2D / Polygon2D and tweens.
public partial class CombatFx : Node2D
{
  public static CombatFx? Instance { get; private set; }

  // Real-time multiplier for fx durations. World sets it from the game speed (1 / sqrt(speed), clamped to 0.2..1.4).
  public float TimeScale = 1.0f;

  // Above this many targets, draw a single line to the centroid instead of one per cell.
  private const int MaxIndividualLines = 3;

  private static readonly Color PlayerColor = new Color(0.35f, 0.75f, 1.0f);
  private static readonly Color EnemyColor = new Color(1.0f, 0.4f, 0.3f);
  private static readonly Color SupportColor = new Color(0.4f, 1.0f, 0.45f);
  private static readonly Color NeutralColor = new Color(0.95f, 0.95f, 0.95f);

  public override void _EnterTree()
  {
    Instance = this;
    ZIndex = 50;
  }

  public override void _ExitTree()
  {
    if (Instance == this)
      Instance = null;
  }

  // Color of an action depends on who it hits: own side = support (green),
  // other side = attacker's side color, empty cells only = neutral.
  public static Color GetActionColor(Unit source, List<Vector2I> targets, Unit[,] unitsGrid)
  {
    bool hitsEnemy = false;
    bool hitsFriend = false;
    foreach (Vector2I cell in targets)
    {
      if (!GlobalFunctions.IsCellInsideGrid(cell))
        continue;
      Unit target = unitsGrid[cell.X, cell.Y];
      if (target == null || target == source)
        continue;
      if (target.Side == source.Side)
        hitsFriend = true;
      else
        hitsEnemy = true;
    }

    if (hitsEnemy)
      return source.Side ? PlayerColor : EnemyColor;
    if (hitsFriend)
      return SupportColor;
    return NeutralColor;
  }

  public static Vector2 CellCenter(Vector2I cell)
  {
    return GlobalFunctions.CellToGlobalPosition(cell, 1, 1, Vector2I.Zero);
  }

  // Thin, dim preview while the unit winds up. It stays until the caller removes it with ClearTelegraph
  // (on strike or death), because a wind-up pauses while the same-tick queue is running.
  public Node2D ShowTelegraph(Unit source, List<Vector2I> targets, Color color, Unit[,]? unitsGrid)
  {
    return SpawnEffect(source, targets, color, unitsGrid, lineWidth: 1.5f, lineAlpha: 0.45f, cellAlpha: 0.12f, holdTime: null, fadeTime: 0f);
  }

  public void ShowStrike(Unit source, List<Vector2I> targets, Color color, Unit[,]? unitsGrid)
  {
    SpawnEffect(source, targets, color, unitsGrid, lineWidth: 3.0f, lineAlpha: 0.95f, cellAlpha: 0.35f, holdTime: 0.12f, fadeTime: 0.3f);
  }

  public void ClearTelegraph(Node2D? telegraph)
  {
    if (telegraph != null && IsInstanceValid(telegraph))
      telegraph.QueueFree();
  }

  // holdTime null = the effect stays until it is freed by the caller
  private Node2D SpawnEffect(Unit source, List<Vector2I> targets, Color color, Unit[,]? unitsGrid, float lineWidth, float lineAlpha, float cellAlpha, float? holdTime, float fadeTime)
  {
    Node2D root = new Node2D();
    AddChild(root);

    List<Vector2I> ownCells = source.GetOccupiedCells();
    List<Vector2I> otherCells = targets.Where(c => !ownCells.Contains(c) && GlobalFunctions.IsCellInsideGrid(c)).Distinct().ToList();

    // Highlight every targeted cell, grown to the whole unit standing on it (also for self-targets, so
    // self-buffs are visible): a fill per cell plus one outline around the outer edges of each group.
    HashSet<Vector2I> highlightCells = new();
    foreach (Vector2I cell in targets)
    {
      if (!GlobalFunctions.IsCellInsideGrid(cell))
        continue;
      highlightCells.Add(cell);
      Unit? unit = unitsGrid?[cell.X, cell.Y];
      if (unit != null && IsInstanceValid(unit))
        highlightCells.UnionWith(unit.GetOccupiedCells().Where(GlobalFunctions.IsCellInsideGrid));
    }

    float half = GlobalConstants.TileSize * 0.5f;
    foreach (Vector2I cell in highlightCells)
    {
      Vector2 c = CellCenter(cell);
      Polygon2D rect = new Polygon2D
      {
        Polygon = [c + new Vector2(-half, -half), c + new Vector2(half, -half), c + new Vector2(half, half), c + new Vector2(-half, half)],
        Color = new Color(color, cellAlpha)
      };
      root.AddChild(rect);
    }

    Vector2 tileSize = new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize);
    foreach (var edge in OverlayLayer.GetOutlineEdges(highlightCells, CellCenter, tileSize))
    {
      Line2D outline = new Line2D { Width = lineWidth * 0.75f, DefaultColor = new Color(color, lineAlpha) };
      outline.AddPoint(edge.Item1);
      outline.AddPoint(edge.Item2);
      root.AddChild(outline);
    }

    // Lines from the source to its targets
    Vector2 from = source.GlobalPosition;
    List<Vector2> ends = new();
    if (otherCells.Count > MaxIndividualLines)
    {
      Vector2 centroid = Vector2.Zero;
      foreach (Vector2I cell in otherCells)
        centroid += CellCenter(cell);
      ends.Add(centroid / otherCells.Count);
    }
    else
    {
      foreach (Vector2I cell in otherCells)
        ends.Add(CellCenter(cell));
    }

    foreach (Vector2 to in ends)
    {
      Line2D line = new Line2D
      {
        Width = lineWidth,
        DefaultColor = new Color(color, lineAlpha),
        BeginCapMode = Line2D.LineCapMode.Round,
        EndCapMode = Line2D.LineCapMode.Round,
      };
      line.AddPoint(from);
      line.AddPoint(to);
      root.AddChild(line);

      // Small dot at the impact end so direction is readable
      Polygon2D dot = new Polygon2D { Polygon = Circle(to, lineWidth * 1.4f), Color = new Color(color, lineAlpha) };
      root.AddChild(dot);
    }

    if (holdTime == null)
      return root;

    Tween tween = root.CreateTween();
    tween.TweenInterval(Mathf.Max(0.02f, holdTime.Value * TimeScale));
    tween.TweenProperty(root, "modulate:a", 0.0f, Mathf.Max(0.03f, fadeTime * TimeScale));
    tween.TweenCallback(Callable.From(root.QueueFree));

    return root;
  }

  private static Vector2[] Circle(Vector2 center, float radius, int segments = 10)
  {
    Vector2[] points = new Vector2[segments];
    for (int i = 0; i < segments; i++)
    {
      float a = Mathf.Tau * i / segments;
      points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
    }
    return points;
  }

  public void ClearAll()
  {
    foreach (Node child in GetChildren())
      child.QueueFree();
  }
}
