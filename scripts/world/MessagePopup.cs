using Godot;
using System;
using System.Collections.Generic;

// Popup above the board with a message, and buttons or rewards to choose from
public partial class MessagePopup : PanelContainer
{
  private Label _message = null!;
  private HBoxContainer _responses = null!;
  private GlobalSignals _globalSignals = null!;

  public override void _Ready()
  {
    _message = GetNode<Label>("MessageContainer/Message");
    _responses = GetNode<HBoxContainer>("MessageContainer/Responses");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
  }

  // The first button is the primary one
  public void ShowMessage(string message, params (string Text, Action OnPressed)[] buttons)
  {
    Clear();
    _message.Text = message;
    for (int i = 0; i < buttons.Length; i++)
    {
      Button button = UiHelpers.MakeButton(buttons[i].Text, primary: i == 0);
      button.Pressed += buttons[i].OnPressed;
      _responses.AddChild(button);
    }
    Show();
  }

  public void Close()
  {
    Clear();
    Hide();
  }

  public void ShowRewards(string message, List<UnitInfo> rewards, Action<UnitInfo> onTaken)
  {
    Clear();
    _message.Text = message;

    VBoxContainer vbox = new();
    vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    vbox.AddThemeConstantOverride("separation", 12);
    _responses.AddChild(vbox);

    HBoxContainer hbox = new();
    hbox.Alignment = BoxContainer.AlignmentMode.Center;
    hbox.AddThemeConstantOverride("separation", 10);
    vbox.AddChild(hbox);

    // Confirming only makes sense once a reward is picked
    UnitInfo? selected = null;
    Button confirmButton = UiHelpers.MakeButton("Take reward", primary: true);
    confirmButton.Disabled = true;
    confirmButton.Pressed += () =>
    {
      if (selected != null)
        onTaken(selected);
    };

    List<Button> rewardButtons = new();
    foreach (UnitInfo unitInfo in rewards)
    {
      Button button = MakeRewardButton(unitInfo);
      button.Pressed += () =>
      {
        selected = unitInfo;
        confirmButton.Disabled = false;
        _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitInfoSelected, unitInfo);

        foreach (Button other in rewardButtons)
          other.Modulate = new Color(0.5f, 0.5f, 0.5f);
        button.Modulate = Colors.White;
      };
      rewardButtons.Add(button);
      hbox.AddChild(button);
    }

    vbox.AddChild(confirmButton);
    Show();
  }

  private static Button MakeRewardButton(UnitInfo unitInfo)
  {
    Button button = new Button
    {
      Text = unitInfo.Name,
      Icon = unitInfo.Texture,
      IconAlignment = HorizontalAlignment.Center,
      VerticalIconAlignment = VerticalAlignment.Top,
      ExpandIcon = true,
      ClipText = true,
      TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
      TooltipText = unitInfo.Description,
      CustomMinimumSize = new Vector2(132, 108),
    };
    button.AddThemeConstantOverride("icon_max_width", 64);

    Color rarityColor = GamePalette.Rarity(unitInfo.Rarity);
    button.AddThemeColorOverride("font_color", rarityColor);
    button.AddThemeStyleboxOverride("normal", GamePalette.Box(GamePalette.PanelLight, new Color(rarityColor, 0.6f), 2, 8, 10, 10));
    button.AddThemeStyleboxOverride("hover", GamePalette.Box(GamePalette.PanelLight.Lightened(0.1f), rarityColor, 2, 8, 10, 10));
    button.AddThemeStyleboxOverride("pressed", GamePalette.Box(GamePalette.Panel, rarityColor, 2, 8, 10, 10));
    return button;
  }

  private void Clear()
  {
    _message.Text = "";
    foreach (Node child in _responses.GetChildren())
      child.QueueFree();
  }
}
