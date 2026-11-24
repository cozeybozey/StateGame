using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Parasite : Unit
{
  private Vector2 _deathDrift = Vector2.Zero;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Tuple<Vector2I, Vector2I>> targets = new List<Tuple<Vector2I, Vector2I>>();
    List<Vector2I> newSurroundingCells = [new Vector2I(-1, 0), new Vector2I(0, -1), new Vector2I(1, 0), new Vector2I(0, 1)];

    foreach (Unit unit in units)
    {
      if (unit.Side == Side)
        continue;

      foreach (Vector2I surroundingCell in unit.GetSurroundingCells())
      {
        if ((surroundingCell == OccupiedMainCell || Side ? surroundingCell.Y < OccupiedMainCell.Y : surroundingCell.Y > OccupiedMainCell.Y) &&
          GlobalFunctions.CanMoveToCell(this, surroundingCell, unitsGrid, terrainGrid, propsGrid))
        {
          foreach (Vector2I newSurroundingCell in newSurroundingCells)
          {
            Vector2I targetCell = surroundingCell + newSurroundingCell;
            if (GlobalFunctions.IsCellInsideGrid(targetCell) &&
              unitsGrid[targetCell.X, targetCell.Y] == unit)
            {
              targets.Add(new Tuple<Vector2I, Vector2I>(surroundingCell, targetCell));
            }
          }
        }
      }
    }

    if (targets.Count == 0)
      return [];
    else
    {
      int index = _rng.RandiRange(0, targets.Count - 1);
      _moveTarget = targets[index].Item1;
      return [targets[index].Item2];
    }
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0)
      return;

    // Attack target
    Unit targetUnit = unitsGrid[targets[0].X, targets[0].Y];
    if (targetUnit != null)
      targetUnit.ChangeHealth(-Damage, this);

    // The parasite burrows into its target
    _deathDrift = CombatFx.CellCenter(targets[0]) - GlobalPosition;
    Die();
  }

  protected override Vector2 GetDeathDrift()
  {
    return _deathDrift;
  }
}
