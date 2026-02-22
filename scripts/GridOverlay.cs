using Godot;
using Godot.Collections;
using System;
using static Godot.Control;

public partial class GridOverlay : ReferenceRect
{
	private TileMapLayer _backgroundLayer = null!;
	//private TileMapLayer _unitsLayer;

	// 2D array to track units in the 8x16 grid
	private Unit[,] _unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
	private Node _unitsNode = null!;

  public override void _Ready()
	{
		_backgroundLayer = GetTree().CurrentScene.GetNode<TileMapLayer>("BackgroundLayer");
		//_unitsLayer = GetTree().CurrentScene.GetNode<TileMapLayer>("UnitsLayer");
		MouseFilter = MouseFilterEnum.Stop;
		_unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

	public override Variant _GetDragData(Vector2 atPosition)
	{
		Vector2I cell = GetCellUnderMouse(atPosition);

		if (!IsCellInsideGrid(cell))
			return default;

		Vector2I relCel = AbsCellToRelCell(cell);
		Unit unit = _unitGrid[relCel.X, relCel.Y];
		if (unit == null)
			return default;

		if (unit.unitInfo.Texture != null)
		{
			var preview = new TextureRect();
			preview.Texture = unit.unitInfo.Texture;
			preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			preview.CustomMinimumSize = new Vector2(32, 32);
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

			SetDragPreview(preview);
		}

		return new DragPayload
		{
			Unit = unit.unitInfo,
			Source = this,
			OriginCell = relCel
	};
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is not DragPayload)
			return false;
		
		Vector2I cell = GetCellUnderMouse(atPosition);

		if (!IsCellInsideGrid(cell))
			return false;

		// Reject if no tile exists there
		if (!IsCellValid(cell))
			return false;

		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is not DragPayload dragPayload)
			return;

		Vector2I targetCell = GetCellUnderMouse(atPosition);
		Vector2I relCel = AbsCellToRelCell(targetCell);

		// Place the unit in the new cell
		PackedScene scene = GD.Load<PackedScene>(dragPayload.Unit.ScenePath);
		Node instance = scene.Instantiate();
		Unit unit = instance as Unit;
		unit.GlobalPosition = RelCellToGlobalPosition(relCel);
		unit.unitInfo = dragPayload.Unit;
		_unitsNode.AddChild(instance);
		_unitGrid[relCel.X, relCel.Y] = unit;

		// Clear origin cell if it came from this overlay
		if (dragPayload.Source == this && dragPayload.OriginCell != null)
		{
			_unitGrid[dragPayload.OriginCell.Value.X, dragPayload.OriginCell.Value.Y].QueueFree();
			_unitGrid[dragPayload.OriginCell.Value.X, dragPayload.OriginCell.Value.Y] = null;
		}

		// Notify original source if it exists and isn’t this overlay
		if (dragPayload.Source is IUnitDragSource source && dragPayload.Source != this)
		{
			source.OnUnitPlacedSuccessfully(dragPayload.Unit);
		}
	}

	private Vector2I GetCellUnderMouse(Vector2 atPosition)
	{
		Vector2 localPos = _backgroundLayer.ToLocal(atPosition);
		return _backgroundLayer.LocalToMap(localPos);
	}

	private bool IsCellValid(Vector2I cell)
	{
		int backgroundSourceId = _backgroundLayer.GetCellSourceId(cell);
		Vector2I backgroundAtlastCoords = _backgroundLayer.GetCellAtlasCoords(cell);

		// -1 means empty
		if (backgroundSourceId == -1)
			return false;

		// Cannot place if there is already a unit there
		if (_unitGrid[AbsCellToRelCell(cell).X, AbsCellToRelCell(cell).Y] != null)
			return false;

	// Cannot be opponents side of the board or edge of the board
	if (backgroundAtlastCoords.Equals(new Vector2I(4, 1)) ||
			backgroundAtlastCoords.Equals(new Vector2I(4, 2)))
			return false;

		return true;
	}

	private bool IsCellInsideGrid(Vector2I cell)
	{
		Vector2I relCell = AbsCellToRelCell(cell);
		return relCell.X >= 0 && relCell.X < GlobalConstants.GridSize.X && relCell.Y >= 0 && relCell.Y < GlobalConstants.GridSize.Y;
	}

	// TODO fix
	public Vector2I AbsCellToRelCell(Vector2I cell, bool player=true)
	{
		if (player)
		{
			return cell - GlobalConstants.GridStartPosPlayer;
	}
		else
		{
			return cell - GlobalConstants.GridStartPosEnemy;
	}
	}

	//TODO fix
	public Vector2I RelCellToGlobalPosition(Vector2I cell, bool player = true)
	{
		if (player)
		{
			return (cell + GlobalConstants.GridStartPosPlayer) * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt(0.5 * GlobalConstants.TileSize), Mathf.FloorToInt(0.5 * GlobalConstants.TileSize));
		}
		else
		{
	  return (cell + GlobalConstants.GridStartPosEnemy) * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt(0.5 * GlobalConstants.TileSize), Mathf.FloorToInt(0.5 * GlobalConstants.TileSize));
	}
  }

	public Unit[,] GetUnits()
	{
			return _unitGrid;
  }
}
