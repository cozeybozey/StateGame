using Godot;
using System;
using Godot.Collections;

public partial class UnitGui : HBoxContainer
{
	[Export] public string ItemName = "DefaultItem";
	[Export] public int ItemID = 0;

	private TextureRect _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<TextureRect>("Sprite"); // Adjust name if needed
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		// Create metadata dictionary
		var data = new Dictionary
		{
			{ "item_name", ItemName },
			{ "item_id", ItemID },
			{ "source_node", this }
		};

		// Optional: create drag preview
		if (_sprite != null)
		{
			var preview = new TextureRect();
			preview.Texture = _sprite.Texture;
			preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			preview.CustomMinimumSize = new Vector2(32, 32);
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

			SetDragPreview(preview);
		}

		return data;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType == Variant.Type.Dictionary)
		{
			var dict = (Dictionary)data;
			return dict.ContainsKey("item_id");
		}

		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dict = (Dictionary)data;

		GD.Print("Dropped item:");
		GD.Print("Name: " + dict["item_name"]);
		GD.Print("ID: " + dict["item_id"]);
	}
}
