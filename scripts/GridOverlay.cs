using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using static Godot.Control;

public partial class GridOverlay : ReferenceRect, IUnitDragSource
{
	public int maxUnitSlots = 20;

	private TileMapLayer _backgroundLayer = null!;
	private Label _unitsCounter = null!;

	// 2D array to track units in the 8x16 grid
	private Unit[,] _unitGrid = new Unit[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
	private Node _unitsNode = null!;

  private bool _interactionLocked = false;
	private int _currentUnitSlotsCount = 0;

  public override void _Ready()
	{
		_backgroundLayer = GetTree().CurrentScene.GetNode<TileMapLayer>("BackgroundLayer");
    _unitsCounter = GetTree().CurrentScene.GetNode<Label>("CanvasLayer/BottomUi/UnitCounter/Counter");
		_unitsCounter.Text = $"{_currentUnitSlotsCount }/{maxUnitSlots}";
    MouseFilter = MouseFilterEnum.Stop;
		_unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

	public override Variant _GetDragData(Vector2 atPosition)
	{
		Vector2I cell = GetCellUnderMouse(atPosition);

		if (!GlobalFunctions.IsCellInsideGrid(cell) || cell.Y < GlobalConstants.GridSize.Y * 0.5)
			return default;

		Unit unit = _unitGrid[cell.X, cell.Y];
		if (unit == null)
			return default;

		if (unit.texture != null)
		{
			var preview = new TextureRect();
			preview.Texture = unit.texture;
			preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			preview.CustomMinimumSize = new Vector2(32, 32);
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

			SetDragPreview(preview);
		}

		return new DragPayload(unit.GetStartInfo(), this, unit.occupiedMainCell);
  }

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
    if (_interactionLocked)
      return false;

    if (data.Obj is not DragPayload dragPayload)
			return false;

		if (dragPayload.Source != this && _currentUnitSlotsCount + dragPayload.Unit.OccupiedCells.Count > maxUnitSlots)
			return false;
		
		Vector2I cell = GetCellUnderMouse(atPosition);

    if (!IsCellValid(dragPayload, cell))
      return false;

    return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
    if (_interactionLocked)
      return;

    if (data.Obj is not DragPayload dragPayload)
			return;

		Vector2I targetCell = GetCellUnderMouse(atPosition);

    // Clear origin cell if it came from this overlay
    if (dragPayload.Source == this)
    {
			Unit unit = _unitGrid[dragPayload!.OriginCell!.Value.X, dragPayload!.OriginCell!.Value.Y];

      // Remove unit from previous cells
      foreach (var cell in unit.GetOccupiedCells())
			{
        _unitGrid[cell.X, cell.Y] = null!;
      }

      // Move unit to new cells
      unit.MoveToCell(targetCell);

			// Assign unit to new cells
      foreach (var cell in unit.GetOccupiedCells())
      {
        _unitGrid[cell.X, cell.Y] = unit;
      }

    }
    else
		{ 
      // Place the unit in the new cell
      PackedScene scene = GD.Load<PackedScene>(dragPayload.Unit.ScenePath);
      Node instance = scene.Instantiate();
      Unit unit = instance as Unit;
			unit!.Initialize(dragPayload.Unit, true, targetCell);
      _unitsNode.AddChild(instance);
			foreach (var cell in dragPayload.Unit.OccupiedCells)
			{
				_unitGrid[targetCell.X + cell.X,targetCell.Y + cell.Y] = unit;
      }

      _currentUnitSlotsCount += dragPayload.Unit.OccupiedCells.Count;
      _unitsCounter.Text = $"{_currentUnitSlotsCount}/{maxUnitSlots}";
    }

		// Notify original source if it exists and isn’t this overlay
		if (dragPayload.Source is IUnitDragSource source && dragPayload.Source != this)
		{
			source.OnUnitPlacedSuccessfully(dragPayload);
		}
	}

  public Vector2I GetCellUnderMouse(Vector2 atPosition)
	{
		Vector2 localPos = _backgroundLayer.ToLocal(atPosition);
		return _backgroundLayer.LocalToMap(localPos);
	}

	private bool IsCellValid(DragPayload dragPayload, Vector2I cell)
	{
		List<Vector2I> originalCells = new List<Vector2I>();
		if (dragPayload.Source == this)
		{
      foreach (Vector2I occupiedCell in dragPayload.Unit.OccupiedCells)
			{
				originalCells.Add(dragPayload.OriginCell!.Value + occupiedCell);
			}
		}

		foreach (Vector2I occupiedCell in dragPayload.Unit.OccupiedCells)
		{
			// Reject if cell is not on player's side of the board
			if (!GlobalFunctions.IsCellInsideGrid(cell + occupiedCell) || cell.Y + occupiedCell.Y < GlobalConstants.GridSize.Y * 0.5)
				return false;

			// Cannot place if there is already a unit there and that unit is not this unit
			if (_unitGrid[cell.X + occupiedCell.X, cell.Y + occupiedCell.Y] != null && !originalCells.Contains(cell + occupiedCell))
				return false;
		}

		return true;
	}

	public Unit[,] GetUnits()
	{
		return _unitGrid;
  }

	public void LoadUnits()
	{
		for (int x = 0; x < GlobalConstants.GridSize.X; x++)
		{
			for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
			{
				Unit unit = _unitGrid[x, y];
				Vector2I cell = new Vector2I(x, y);
        if (unit != null && unit.startCell == cell)
				{
          UnitInfo unitInfo = _unitGrid[x, y].GetStartInfo();
          PackedScene scene = GD.Load<PackedScene>(unitInfo.ScenePath);
					Node instance = scene.Instantiate();
					Unit newUnit = instance as Unit;
          newUnit!.Initialize(unitInfo, true, new Vector2I(x, y));
          _unitsNode.AddChild(instance);

					foreach (Vector2I occupiedCell in newUnit!.GetOccupiedCells())
						_unitGrid[occupiedCell.X, occupiedCell.Y] = newUnit;
        }
      }
		}
	}

  public void SetInteractionLocked(bool locked)
  {
    _interactionLocked = locked;
  }

  public void OnUnitPlacedSuccessfully(DragPayload dragPayload)
  {
		Unit unit = _unitGrid[dragPayload.OriginCell!.Value.X, dragPayload.OriginCell!.Value.Y];
		foreach (var cell in unit.GetOccupiedCells())
		{
			_unitGrid[cell.X, cell.Y] = null!;
		}    
    unit.QueueFree();
    _currentUnitSlotsCount -= unit.GetOccupiedCells().Count;
    _unitsCounter.Text = $"{_currentUnitSlotsCount}/{maxUnitSlots}";
  }
}
