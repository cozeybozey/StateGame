using Godot;
using System;
using Godot.Collections;

public partial class UnitGui : HBoxContainer, IUnitDragSource
{
	[Export] public UnitInfo Info = null!;
	[Export] public int Amount;

	private TextureRect _sprite = null!;
	private Label _nameText = null!;
	private Label _amountText = null!;

	public override void _Ready()
	{
		_sprite = GetNode<TextureRect>("Sprite");
    _nameText = GetNode<Label>("Name");
		_amountText = GetNode<Label>("Amount");

		_sprite.Texture = Info.Texture;
		_nameText.Text = Info.Name;
		_amountText.Text = Amount.ToString();
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (Amount <= 0)
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

	// TODO Fix
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType == Variant.Type.Dictionary)
		{
			var dict = (Dictionary)data;
			return dict.ContainsKey("item_id");
		}

		return false;
	}

	// TODO Fix
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dict = (Dictionary)data;

		GD.Print("Dropped item:");
		GD.Print("Name: " + dict["item_name"]);
		GD.Print("ID: " + dict["item_id"]);
	}

	public void OnUnitPlacedSuccessfully(UnitInfo unit)
	{
    UpdateAmount(Amount - 1);
	}

	public void UpdateAmount(int newAmount)
	{
		Amount = newAmount;
		_amountText.Text = Amount.ToString();
  }
}
