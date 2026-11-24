using Godot;
using System.Collections.Generic;
using System.Linq;

public static class UiHelpers
{
  public static string Capitalize(string text)
  {
    return string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text.Substring(1);
  }

  // Info panels refresh every frame, so the tags are only rebuilt when they actually differ.
  public static void ShowTags(Container container, IEnumerable<string> tags)
  {
    List<string> list = tags.Select(Capitalize).ToList();
    string key = string.Join("|", list);
    if (container.HasMeta("tags") && container.GetMeta("tags").AsString() == key)
      return;
    container.SetMeta("tags", key);

    foreach (Node child in container.GetChildren())
      child.QueueFree();
    foreach (string tag in list)
      container.AddChild(MakeTag(tag));
    container.Visible = list.Count > 0;
  }

  private static PanelContainer MakeTag(string text)
  {
    PanelContainer tag = new PanelContainer { ThemeTypeVariation = "ChipPanel" };
    Label label = new Label { Text = text };
    label.AddThemeFontSizeOverride("font_size", 12);
    tag.AddChild(label);
    return tag;
  }

  public static Button MakeButton(string text, bool primary = false)
  {
    return new Button
    {
      Text = text,
      ThemeTypeVariation = primary ? "PrimaryButton" : "",
      CustomMinimumSize = new Vector2(120, 38),
      SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
      SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };
  }
}
