using Godot;

// The board's frame and empty cells. Terrain covers the cells during a level.
// With IsDivider set, the node only draws the line between the two halves, so it can sit above the terrain.
public partial class BoardFrame : Node2D
{
  [Export] public bool IsDivider = false;

  public override void _Draw()
  {
    float tile = GlobalConstants.TileSize;
    Vector2I grid = GlobalConstants.GridSize;
    Vector2 size = new Vector2(grid.X * tile, grid.Y * tile);

    if (IsDivider)
    {
      DrawLine(new Vector2(0, size.Y / 2), new Vector2(size.X, size.Y / 2), new Color(GamePalette.Gold, 0.55f), 2f);
      return;
    }

    DrawStyleBox(GamePalette.Box(GamePalette.Panel, GamePalette.Border, 2, 6), new Rect2(Vector2.Zero, size).Grow(3));

    for (int y = 0; y < grid.Y; y++)
    {
      for (int x = 0; x < grid.X; x++)
      {
        Color color = y < grid.Y / 2 ? new Color("2c2229") : new Color("222b3b");
        if ((x + y) % 2 == 0)
          color = color.Lightened(0.05f);
        DrawRect(new Rect2(x * tile, y * tile, tile, tile), color);
      }
    }
  }
}
