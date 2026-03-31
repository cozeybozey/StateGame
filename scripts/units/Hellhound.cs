using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class Hellhound : Unit
{
  private Vector2I? _targetLocation = null;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I currentAttackTarget = new Vector2I(OccupiedMainCell.X, Side ? OccupiedMainCell.Y - 1 : OccupiedMainCell.Y + 1);
    if (GlobalFunctions.IsCellInsideGrid(currentAttackTarget) && 
      unitsGrid[currentAttackTarget.X, currentAttackTarget.Y] != null && 
      unitsGrid[currentAttackTarget.X, currentAttackTarget.Y].Side != Side)
    {
      _targetLocation = OccupiedMainCell;
      return [currentAttackTarget];
    }

    // Get all enemies ordered by frontmost (closest to our side)
    List<Unit> enemies = units
        .Where(u => u.Side != Side)
        .OrderByDescending(u => Side ? u.OccupiedMainCell.Y : -u.OccupiedMainCell.Y)
        .ThenBy(u => u.OccupiedMainCell.X)
        .ToList();

    foreach (Unit enemy in enemies)
    {
      // Cells in front of the enemy (towards our side)
      List<Vector2I> jumpCells = enemy.GetSurroundingCells(includeFront: true, includeBack: false, includeSides: false);

      // Check if we can land on the cells in front of this enemy
      foreach (Vector2I cell in jumpCells)
      {
        // Hellhound is 2 cells tall and occupied main cell is top cell, so for enemy side we have to do -1 hellhound
        Vector2I jumpCell = Side ? cell : new Vector2I(cell.X, cell.Y - 1);
        Vector2I attackTarget = new Vector2I(cell.X, Side ? cell.Y - 1 : cell.Y + 1);
        if (GlobalFunctions.CanMoveToCell(this, jumpCell, unitsGrid, terrainGrid, propsGrid) && GlobalFunctions.IsCellInsideGrid(attackTarget))
        {
          _targetLocation = jumpCell;
          return [attackTarget];
        }
      }
    }

    _targetLocation = null;
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (_targetLocation == null || targets.Count == 0)
      return;

    // Jump to the landing cell
    if (_targetLocation.Value != OccupiedMainCell)
      MoveToCell(_targetLocation.Value, true);

    // Deal damage to the enemy in front
    Unit enemy = unitsGrid[targets[0].X, targets[0].Y];
    if (enemy != null)
      enemy.ChangeHealth(-Damage, this);
  }
}
