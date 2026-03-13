using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Blob : Unit
{
  private Vector2I _newOccupiedMainCell;
  private List<Vector2I> _newOccupiedCells = new List<Vector2I>();

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    List<Unit> consumedUnits = new List<Unit>();
    foreach (Vector2I target in targets)
    {
      Unit unit = unitsGrid[target.X, target.Y];
      if (unit != null && unit != this && !consumedUnits.Contains(unit))
      {
        // Consume the unit by killing it and gaining its stats
        unit.SpawnFloatingText("Consumed", Colors.Red);
        ChangeMaxHealth(unit.maxHealth);
        ChangeDamage(unit.damage);
        ChangeArmor(unit.armor);
        ChangeSpeed(unit.speed);
        unit.Die();
        consumedUnits.Add(unit);
      }
    }

    List<Vector2I> oldOccupiedCells = GetOccupiedCells();
    MoveToCell(_newOccupiedMainCell, true);
    occupiedCells = new List<Vector2I>(_newOccupiedCells);
    Vector2I cellDimensions = GlobalFunctions.CellsToDimensions(occupiedCells);
    Vector2I relPos = GlobalFunctions.GetRelPosInCells(occupiedCells, occupiedCells[0]);
    GlobalPosition = GlobalFunctions.CellToGlobalPosition(occupiedMainCell, cellDimensions.X, cellDimensions.Y, relPos);
    _globalSignals.EmitSignal(GlobalSignals.SignalName.SizeChanged, this, new Godot.Collections.Array<Vector2I>(oldOccupiedCells));
    _sprite.Scale = GlobalFunctions.CellsToDimensions(occupiedCells);
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I currentDimensions = GlobalFunctions.CellsToDimensions(occupiedCells);
    int currentSize = currentDimensions.X; // Can also use Y here cause unit should be square
    int newSize = currentSize + 1;

    // Build list of valid directions to expand (as topLeft offsets)
    // Each direction shifts the square's topLeft and grows it by 1
    var candidates = new List<Vector2I>
    {
        new Vector2I(occupiedMainCell.X, occupiedMainCell.Y), // downRight: topLeft stays, bottom-right grows
        new Vector2I(occupiedMainCell.X - 1, occupiedMainCell.Y), // downLeft:  topLeft shifts left, bottom-left grows
        new Vector2I(occupiedMainCell.X, occupiedMainCell.Y - 1), // upRight:   topLeft shifts up, top-right grows
        new Vector2I(occupiedMainCell.X - 1, occupiedMainCell.Y - 1), // upLeft:    topLeft shifts up-left
    };

    var valid = candidates.Where(c =>
        c.X >= 0 &&
        c.Y >= 0 &&
        c.X + newSize <= GlobalConstants.GridSize.X &&
        c.Y + newSize <= GlobalConstants.GridSize.Y
    ).ToList();

    if (valid.Count == 0) return [];

    Random random = new();
    Vector2I chosen = valid[random.Next(valid.Count)];
    _newOccupiedMainCell = chosen;

    // Collect all cells in the new square, minus the ones already occupied
    List<Vector2I> newCells = new();
    _newOccupiedCells.Clear();
    for (int y = 0; y < newSize; y++)
    {
      for (int x = 0; x < newSize; x++)
      {
        var cell = new Vector2I(x, y);
        _newOccupiedCells.Add(cell);
        bool alreadyOwned =
            x + _newOccupiedMainCell.X >= occupiedMainCell.X && x + _newOccupiedMainCell.X < occupiedMainCell.X + currentSize &&
            y + _newOccupiedMainCell.Y >= occupiedMainCell.Y && y + _newOccupiedMainCell.Y < occupiedMainCell.Y + currentSize;
        if (!alreadyOwned)
          newCells.Add(cell + _newOccupiedMainCell);
      }
    }

    return newCells;
  }
}
