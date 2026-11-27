using Godot;

// The game's own mouse cursor: a pointer drawn in the game's colors. While a unit is dragged it turns gold,
// and it replaces the operating system's "forbidden" cursor, so the cursor never changes into a stop sign.
public static class GameCursor
{
  private const int Size = 24;

  // Outline of the pointer, in pixels from its tip
  private static readonly Vector2[] Shape =
  [
    new(1, 1), new(1, 17.5f), new(4.5f, 14), new(7.5f, 21.5f), new(12.5f, 19.5f), new(9.5f, 12.5f), new(14.5f, 12.5f),
  ];

  public static void Apply()
  {
    Texture2D normal = Draw(GamePalette.Text);
    Texture2D dragging = Draw(GamePalette.Gold);

    Input.SetCustomMouseCursor(normal, Input.CursorShape.Arrow, Vector2.One);
    Input.SetCustomMouseCursor(normal, Input.CursorShape.PointingHand, Vector2.One);
    foreach (Input.CursorShape shape in new[] { Input.CursorShape.Drag, Input.CursorShape.CanDrop, Input.CursorShape.Forbidden })
      Input.SetCustomMouseCursor(dragging, shape, Vector2.One);
  }

  // Fills the pointer with the color and gives it a dark border, so it shows on light and dark tiles
  private static Texture2D Draw(Color fill)
  {
    Image image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
    for (int y = 0; y < Size; y++)
    {
      for (int x = 0; x < Size; x++)
      {
        Vector2 point = new(x + 0.5f, y + 0.5f);
        if (Geometry2D.IsPointInPolygon(point, Shape))
          image.SetPixel(x, y, DistanceToEdge(point) < 1.1f ? GamePalette.Background : fill);
      }
    }
    return ImageTexture.CreateFromImage(image);
  }

  private static float DistanceToEdge(Vector2 point)
  {
    float closest = float.MaxValue;
    for (int i = 0; i < Shape.Length; i++)
    {
      Vector2 onEdge = Geometry2D.GetClosestPointToSegment(point, Shape[i], Shape[(i + 1) % Shape.Length]);
      closest = Mathf.Min(closest, point.DistanceTo(onEdge));
    }
    return closest;
  }
}
