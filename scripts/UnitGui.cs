using Godot;
using System;
using Godot.Collections;

public partial class UnitGui : HBoxContainer, IUnitDragSource
{
	[Export] public UnitInfo Info = null!;
	[Export] public int Amount;
	[Export] public bool Draggable = true;

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
		_amountText.Text = Amount.ToString();
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (!Draggable || Amount <= 0)
			return default;

		if (_sprite != null)
		{
			var preview = new TextureRect();
			preview.Texture = _sprite.Texture;
			preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			preview.CustomMinimumSize = new Vector2(32, 32);
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

			SetDragPreview(preview);
		}

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
    UpdateAmount(Amount - 1);
	}

	public void UpdateAmount(int newAmount)
	{
		Amount = newAmount;
		_amountText.Text = Amount.ToString();
  }
}
