using Godot;
using System;
using Godot.Collections;

public partial class UnitGui : HBoxContainer, IUnitDragSource
{
	[Export] public UnitInfo Info = null!;
	[Export] public int Amount;
	[Export] public bool Draggable = true;

  // Can be placed any number of times, and shows no amount
  public bool Infinite = false;

	private TextureRect _sprite = null!;
	private Label _nameText = null!;
	private Label _amountText = null!;
  private GlobalSignals _globalSignals = null!;

  [Signal]
  public delegate void ClickedEventHandler();

  public override void _Ready()
	{
		_sprite = GetNode<TextureRect>("Sprite");
    _nameText = GetNode<Label>("Name");
		_amountText = GetNode<Label>("Amount");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");

    _sprite.Texture = Info.Texture;
		_nameText.Text = Info.Name;
    _nameText.AddThemeColorOverride("font_color", GamePalette.Rarity(Info.Rarity));
    MouseEntered += QueueRedraw;
    MouseExited += QueueRedraw;
    ShowAmount();
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (!Draggable || (Amount <= 0 && !Infinite))
			return default;

		return new DragPayload(Info, this, null);
	}

  public override void _GuiInput(InputEvent @event)
  {
    if (@event is InputEventMouseButton mouseEvent &&
        mouseEvent.ButtonIndex == MouseButton.Left &&
        mouseEvent.Pressed)
    {
      _globalSignals.EmitSignal(GlobalSignals.SignalName.UnitInfoSelected, Info);
    }
  }

  public void OnUnitPlacedSuccessfully(DragPayload dragPayload)
	{
    if (Infinite)
      return;
    UpdateAmount(Amount - 1);
	}

  // Rows you can drag from fade out once none are left to place
  private void ShowAmount()
  {
    _amountText.Text = Infinite ? "" : $"x{Amount}";
    Modulate = Draggable && Amount <= 0 && !Infinite ? new Color(1, 1, 1, 0.55f) : Colors.White;
  }

  public override void _Draw()
  {
    Rect2 row = new Rect2(Vector2.Zero, Size);
    if (row.HasPoint(GetLocalMousePosition()))
      DrawStyleBox(GamePalette.Box(GamePalette.PanelLight, GamePalette.Border), row);
  }

	public void UpdateAmount(int newAmount)
	{
		Amount = newAmount;
    ShowAmount();
  }
}
