using Godot;
using System.Collections.Generic;
using System.Linq;

// The world's action bar below the board: when it is full, props act and dead zombies revive.
// Props that will act are outlined shortly before that, and while the bar or the world's timeline slot is hovered.
public partial class WorldBar : Node2D
{
  // The outlines appear this many ticks before the world acts, matching the wind-up of units
  private const int WarningTicks = 2 * GlobalConstants.TelegraphTicks;
  private const int FontSize = 12;

  private int _ticksLeft = GlobalConstants.TicksPerWorldAction;
  private bool _playing = false;
  private bool _highlight = false;
  private List<Prop> _actingProps = new();
  private float _flash = 0.0f;

  private static Rect2 BarRect
  {
    get
    {
      Vector2I board = GlobalConstants.GridSize * GlobalConstants.TileSize;
      return new Rect2(58, board.Y + 12, board.X - 58 - 64, 10);
    }
  }

  public override void _EnterTree()
  {
    ZIndex = 35;
  }

  public void SetData(int ticksLeft, bool playing, bool highlight, IEnumerable<Prop> props)
  {
    _ticksLeft = ticksLeft;
    _playing = playing;
    _actingProps = props.Where(p => p.ActsWithWorld).ToList();
    _highlight = highlight || BarRect.GrowIndividual(58, 8, 64, 8).HasPoint(GetGlobalMousePosition());
  }

  public void PlayAction()
  {
    _flash = 1.0f;
    Tween tween = CreateTween();
    tween.TweenMethod(Callable.From<float>(value => _flash = value), 1.0f, 0.0f, 0.5f * (CombatFx.Instance?.TimeScale ?? 1.0f));
    foreach (Prop prop in _actingProps)
    {
      if (IsInstanceValid(prop))
        prop.PlaySupportReaction();
    }
  }

  public override void _Process(double delta)
  {
    if (Visible)
      QueueRedraw();
  }

  public override void _Draw()
  {
    Font font = ThemeDB.GetProjectTheme()?.GetFont("font", "ValueLabel") ?? ThemeDB.FallbackFont;
    float progress = 1.0f - (float)_ticksLeft / GlobalConstants.TicksPerWorldAction;
    float seconds = (float)_ticksLeft / GlobalConstants.TicksPerSecond;
    Color color = GamePalette.World.Lerp(Colors.White, _flash);
    Rect2 bar = BarRect;
    float baseline = bar.GetCenter().Y + FontSize * 0.35f;

    DrawString(font, new Vector2(2, baseline), "WORLD", HorizontalAlignment.Left, -1, FontSize, color);
    DrawStyleBox(GamePalette.Box(GamePalette.Background, GamePalette.Border, 1, 5, 0, 0), bar.Grow(1));
    if (bar.Size.X * progress >= 1)
      DrawStyleBox(GamePalette.Box(color, color, 0, 4, 0, 0), new Rect2(bar.Position, new Vector2(bar.Size.X * progress, bar.Size.Y)));
    DrawString(font, new Vector2(bar.End.X + 10, baseline), System.FormattableString.Invariant($"{seconds:0.0}s"), HorizontalAlignment.Left, -1, FontSize, GamePalette.Muted);

    bool warning = _playing && _ticksLeft <= WarningTicks;
    if (!warning && !_highlight)
      return;

    Vector2 tileSize = new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize);
    foreach (Prop prop in _actingProps)
    {
      if (!IsInstanceValid(prop))
        continue;
      foreach (Vector2I cell in prop.GetOccupiedCells())
        DrawRect(new Rect2(CombatFx.CellCenter(cell) - tileSize / 2, tileSize), new Color(GamePalette.World, 0.2f));
      foreach (var edge in OverlayLayer.GetOutlineEdges(prop.GetOccupiedCells(), CombatFx.CellCenter, tileSize))
        DrawLine(edge.Item1, edge.Item2, GamePalette.World, 2f);
    }
  }
}
