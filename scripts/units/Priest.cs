using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;

public partial class Priest : Unit
{
  private GlobalSignals? _globalSignals;
  private Node _unitsNode;
  private Unit _unitToBeRevived;

  protected override void Start()
  {
    _unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      // Spawn unit
      Unit unitInstance = GD.Load<PackedScene>(_unitToBeRevived.scenePath).Instantiate() as Unit;
      unitInstance!.Initialize(_unitToBeRevived.GetStartInfo(), _unitToBeRevived.side, target);
      _unitsNode.AddChild(unitInstance);
      unitInstance.SpawnFloatingText("Revived", Colors.Green);
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    // Pick random unit from dead units
    if (deadUnits.Count == 0)
      return [];
    List<Unit> friendlyDeadUnits = new List<Unit>();
    foreach (Unit unit in deadUnits)
    {
      if (unit.side == side)
        friendlyDeadUnits.Add(unit);
    }
    if (friendlyDeadUnits.Count == 0)
      return [];
    RandomNumberGenerator rng = new();
    _unitToBeRevived = friendlyDeadUnits[rng.RandiRange(0, friendlyDeadUnits.Count - 1)];
    UnitInfo unitInfo = _unitToBeRevived.GetStartInfo();

    // Spawn unit at random location
    List<Vector2I> possiblePositions = new();
    int yStart = (side) ? Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5) : 0;
    int yEnd = (side) ? GlobalConstants.GridSize.Y : Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5);
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = yStart; y < yEnd; y++)
      {
        bool canPlace = true;
        foreach (Vector2I cell in unitInfo.OccupiedCells)
        {
          int checkX = x + cell.X;
          int checkY = y + cell.Y;
          if (checkX >= GlobalConstants.GridSize.X || checkY >= yEnd ||
              checkX < 0 || checkY < yStart || unitsGrid[checkX, checkY] != null)
          {
            canPlace = false;
            break;
          }
        }
        if (canPlace)
        {
          possiblePositions.Add(new Vector2I(x, y));
        }
      }
    }

    // Now that the unit has been revided it can be removed from the dead units list
    if (IsInstanceValid(_unitToBeRevived))
      _unitToBeRevived.QueueFree();
    deadUnits.Remove(_unitToBeRevived);

    if (possiblePositions.Count > 0)
    {
      Vector2I cellPos = cellPos = possiblePositions[rng.RandiRange(0, possiblePositions.Count - 1)];
      return [cellPos];
    }
    else
      return [];
  }
}
