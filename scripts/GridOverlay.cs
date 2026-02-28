using Godot;
using Godot.Collections;
using System;
using static Godot.Control;

public partial class GridOverlay : ReferenceRect, IUnitDragSource
{
	public int maxUnitSlots = 20;

	private TileMapLayer _backgroundLayer = null!;
	private Label _unitsCounter = null;

	// 2D array to track units in the 8x16 grid
	private UnitInfo[,] _unitGrid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
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

		UnitInfo unit = _unitGrid[cell.X, cell.Y];
		if (unit == null)
			return default;

		if (unit.Texture != null)
		{
			var preview = new TextureRect();
			preview.Texture = unit.Texture;
			preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			preview.CustomMinimumSize = new Vector2(32, 32);
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

			SetDragPreview(preview);
		}

		return new DragPayload(unit, this, cell);
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

    foreach (Vector2I occupiedCell in dragPayload.Unit.OccupiedCells)
      if (!IsCellValid(cell + occupiedCell))
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
			UnitInfo unitInfo = _unitGrid[dragPayload!.OriginCell!.Value.X, dragPayload!.OriginCell!.Value.Y];

      // Remove unit from previous cells
      foreach (var cell in dragPayload.Unit.UnitInstance!.GetOccupiedCells())
			{
        _unitGrid[cell.X, cell.Y] = null!;
      }

      // Move unit to new cells
      unitInfo.UnitInstance!.MoveToCell(targetCell);

			// Assign unit to new cells
      foreach (var cell in dragPayload.Unit.UnitInstance!.GetOccupiedCells())
      {
        _unitGrid[cell.X, cell.Y] = unitInfo;
      }

    }
    else
		{
      UnitInfo newUnitInfo = new UnitInfo(
				dragPayload.Unit.Id,
				dragPayload.Unit.Name,
				dragPayload.Unit.Texture,
				dragPayload.Unit.ScenePath,
				dragPayload.Unit.OccupiedCells,
        dragPayload.Unit.Cost,
        null
			);

      // Place the unit in the new cell
      PackedScene scene = GD.Load<PackedScene>(dragPayload.Unit.ScenePath);
      Node instance = scene.Instantiate();
      Unit unit = instance as Unit;
      unit.startCell = targetCell;
			unit.occupiedCells = dragPayload.Unit.OccupiedCells;
      _unitsNode.AddChild(instance);
			newUnitInfo.UnitInstance = unit;
			foreach (var cell in dragPayload.Unit.OccupiedCells)
			{
				_unitGrid[targetCell.X + cell.X,targetCell.Y + cell.Y] = newUnitInfo;
      }

      _currentUnitSlotsCount += dragPayload.Unit.OccupiedCells.Count;
      _unitsCounter.Text = $"{_currentUnitSlotsCount}/{maxUnitSlots}";
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

    // Reject if cell is not on player's side of the board
    if (!GlobalFunctions.IsCellInsideGrid(cell) || cell.Y < GlobalConstants.GridSize.Y * 0.5)
      return false;

    // Cannot place if there is already a unit there
    if (_unitGrid[cell.X, cell.Y] != null)
			return false;

		return true;
	}

	public UnitInfo[,] GetUnits()
	{
			return _unitGrid;
  }

	public void LoadUnits()
	{
		for (int x = 0; x < GlobalConstants.GridSize.X; x++)
		{
			for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
			{
				UnitInfo unitInfo = _unitGrid[x, y];
				Vector2I cell = new Vector2I(x, y);
        if (unitInfo != null && unitInfo.UnitInstance!.startCell == cell)
				{
					PackedScene scene = GD.Load<PackedScene>(unitInfo.ScenePath);
					Node instance = scene.Instantiate();
					Unit unit = instance as Unit;
					unit.startCell = cell;
					unit.occupiedCells = unitInfo.OccupiedCells;
          _unitsNode.AddChild(instance);
					_unitGrid[x, y].UnitInstance = unit;
				}
      }
		}
	}

  public void SetInteractionLocked(bool locked)
  {
    _interactionLocked = locked;
  }

  public void OnUnitPlacedSuccessfully(UnitInfo unit)
  {
		foreach (var cell in unit.UnitInstance!.GetOccupiedCells())
		{
			_unitGrid[cell.X, cell.Y] = null!;
		}    
    unit.UnitInstance!.QueueFree();
    _currentUnitSlotsCount -= unit.UnitInstance!.GetOccupiedCells().Count;
    _unitsCounter.Text = $"{_currentUnitSlotsCount}/{maxUnitSlots}";
  }
}
