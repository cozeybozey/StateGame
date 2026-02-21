using Godot;
using System;
using Godot.Collections;

public partial class UnitGui : HBoxContainer, IUnitDragSource
{
	[Export] public UnitInfo Info;
	[Export] public int Amount;

	private TextureRect _sprite;
	private Label _amountText;

	public override void _Ready()
	{
		_sprite = GetNode<TextureRect>("Sprite");
		_amountText = GetNode<Label>("Amount");


		Info = new UnitInfo(1, "Turret", GD.Load<Texture2D>("res://sprites/units/blue_unit.png"), new Vector2I(1, 3), "res://scenes/units/turret.tscn");
		_sprite.Texture = Info.Texture;
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

		return new DragPayload
		{
			Unit = Info,
			Source = this
		};
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
		Amount--;
		_amountText.Text = Amount.ToString();
	}
}
