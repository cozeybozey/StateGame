using Godot;
using Godot.Collections;
using System;
using static Godot.Control;

public partial class GridOverlay : ReferenceRect
{
	private TileMapLayer _backgroundLayer;
	private TileMapLayer _unitsLayer;

	public override void _Ready()
	{
		_backgroundLayer = GetTree().CurrentScene.GetNode<TileMapLayer>("BackgroundLayer");
		_unitsLayer = GetTree().CurrentScene.GetNode<TileMapLayer>("UnitsLayer");
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
			return false;

		var dict = (Dictionary)data;
		if (!dict.ContainsKey("item_id"))
			return false;
		
		Vector2I cell = GetCellUnderMouse(atPosition);

		// Reject if no tile exists there
		if (!IsCellValid(cell))
			return false;

		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dict = (Dictionary)data;

		Vector2I cell = GetCellUnderMouse(atPosition);

		_unitsLayer.SetCell(cell, 0, new Vector2I(1, 3));
	}

	private Vector2I GetCellUnderMouse(Vector2 atPosition)
	{
		Vector2 localPos = _backgroundLayer.ToLocal(atPosition);
		return _backgroundLayer.LocalToMap(localPos);
	}

	private bool IsCellValid(Vector2I cell)
	{
		int backgroundSourceId = _backgroundLayer.GetCellSourceId(cell);
		int unitSourceId = _unitsLayer.GetCellSourceId(cell);
		Vector2I backgroundAtlastCoords = _backgroundLayer.GetCellAtlasCoords(cell);

		// -1 means empty
		if (backgroundSourceId == -1 || unitSourceId != -1)
			return false;

		if (backgroundAtlastCoords.Equals(new Vector2I(4, 1)) ||
			backgroundAtlastCoords.Equals(new Vector2I(4, 2)))
			return false;

		// Example rule: only allow certain tile source
		return true;
	}
}
