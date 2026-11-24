using Godot;

// Round badge with a unit's place in the turn order, ringed in its side's color
public partial class TurnBadge : Node2D
{
  private const float Radius = 9f;
  private const int FontSize = 12;

  // Height of Rubik's digits relative to the font size
  private const float DigitHeight = 0.7f;

  private string _text = "";
  private Color _color = Colors.White;

  public void Show(int number, bool playerSide)
  {
    _text = number.ToString();
    _color = GamePalette.Side(playerSide);
    Visible = true;
    QueueRedraw();
  }

  public override void _Draw()
  {
    Font font = ThemeDB.GetProjectTheme()?.GetFont("font", "ValueLabel") ?? ThemeDB.FallbackFont;
    DrawCircle(Vector2.Zero, Radius, GamePalette.Background, true, -1, true);
    DrawCircle(Vector2.Zero, Radius - 1, _color, false, 2, true);

    float width = font.GetStringSize(_text, HorizontalAlignment.Left, -1, FontSize).X;
    Vector2 baseline = new Vector2(-width / 2, FontSize * DigitHeight / 2);
    DrawString(font, baseline, _text, HorizontalAlignment.Left, -1, FontSize, Colors.White);
  }
}
