using Godot;

// Colors for everything drawn from code. themes/game_theme.tres uses the same palette for UI controls.
public static class GamePalette
{
  public static readonly Color Background = new Color("12141c");
  public static readonly Color Panel = new Color("1c202c");
  public static readonly Color PanelLight = new Color("2b3143");
  public static readonly Color Border = new Color("3a4257");
  public static readonly Color Text = new Color("e8e6df");
  public static readonly Color Muted = new Color("959db0");
  public static readonly Color Gold = new Color("e5b567");

  public static readonly Color Player = new Color("4a8fe7");
  public static readonly Color Enemy = new Color("e0564f");
  public static readonly Color Support = new Color("6fdc7f");
  public static readonly Color Neutral = new Color("f2f2f2");
  public static readonly Color Completed = new Color("5fb878");

  // Earth, Heaven, Future, Graveyard, Hell
  private static readonly Color[] _sections =
    [new Color("6fb35a"), new Color("ead9a0"), new Color("58c4d6"), new Color("a284c9"), new Color("dd5a45")];

  public static Color Side(bool playerSide)
  {
    return playerSide ? Player : Enemy;
  }

  public static Color Rarity(string rarity)
  {
    return rarity switch
    {
      "rare" => new Color("5aa2ff"),
      "epic" => new Color("c07bf0"),
      "legendary" => new Color("f0a640"),
      _ => new Color("a9afbd"),
    };
  }

  public static Color Section(int section)
  {
    return section >= 0 && section < _sections.Length ? _sections[section] : Muted;
  }

  public static StyleBoxFlat Box(Color background, Color border, int borderWidth = 1, int radius = 6, float marginX = 8, float marginY = 4)
  {
    StyleBoxFlat box = new StyleBoxFlat { BgColor = background, BorderColor = border };
    box.SetBorderWidthAll(borderWidth);
    box.SetCornerRadiusAll(radius);
    box.ContentMarginLeft = box.ContentMarginRight = marginX;
    box.ContentMarginTop = box.ContentMarginBottom = marginY;
    return box;
  }
}
