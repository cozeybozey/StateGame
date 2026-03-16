using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Consumer : Unit
{
  private bool _shouldConsume = true;  // Start with a consume turn
  private int _consumedCount = 0; // number of units consumed

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    // If no targets, ensure we will attempt to consume next turn
    if (targets == null || targets.Count == 0)
    {
      _shouldConsume = true;
      return;
    }

    if (_shouldConsume)
    {
      // Consume attempt: only consume on consume-turn. If consumption succeeds, switch to attack next turn (false).
      Vector2I alliedTarget = targets[0];
      bool consumed = false;

      if (GlobalFunctions.IsCellInsideGrid(alliedTarget))
      {
        Unit ally = unitsGrid[alliedTarget.X, alliedTarget.Y];
        if (ally != null && ally.side == side && ally != this)
        {
          // Absorb ally's stats
          ChangeMaxHealth(4);
          ChangeDamage(4);

          // Remove the consumed unit
          ally.SpawnFloatingText("Consumed", Colors.Red);
          ally.Die();

          // Move to the ally's main cell (during play)
          MoveToCell(ally.occupiedMainCell, true);

          consumed = true;
          _consumedCount++;
        }
      }

      // If we consumed a unit, next turn should be an attack turn. If we failed to consume, stay/return to consume behaviour.
      _shouldConsume = !consumed;
    }
    else
    {
      // Attack turn: deal damage to enemy targets only
      for (int i = 0; i < targets.Count; i++)
      {
        Vector2I t = targets[i];
        if (GlobalFunctions.IsCellInsideGrid(t))
        {
          Unit targetUnit = unitsGrid[t.X, t.Y];
          if (targetUnit != null && targetUnit.side != side)
          {
            targetUnit.ChangeHealth(-damage);
          }
        }
      }

      // After attacking, next turn we attempt to consume
      _shouldConsume = true;
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();

    if (_shouldConsume)
    {
      Unit? closestAlly = null;
      float closestDist = float.MaxValue;

      if (units == null || units.Count == 0)
        return new List<Vector2I>();

      foreach (Unit unit in units)
      {
        if (unit == null || unit == this)
          continue;

        float dist = occupiedMainCell.DistanceTo(unit.occupiedMainCell);
        if (dist < closestDist)
        {
          closestDist = dist;
          closestAlly = unit;
        }
      }

      if (closestAlly != null)
      {
        result.Add(closestAlly.occupiedMainCell);
      }
      else
      {
        // No valid ally to consume — fallback to attacking frontmost enemies this turn
        SpawnFloatingText("No ally to consume", Colors.Red);
        int numTargets = 1 + _consumedCount;
        List<Vector2I> enemies = FindFrontmostEnemies(unitsGrid, numTargets);
        result.AddRange(enemies);
      }
    }
    else
    {
      // Attack turn: target frontmost enemies. Number of targets = 1 + consumedCount
      int numTargets = 1 + _consumedCount;
      List<Vector2I> enemies = FindFrontmostEnemies(unitsGrid, numTargets);
      result.AddRange(enemies);
    }

    return result;
  }

  private List<Vector2I> FindFrontmostEnemies(Unit[,] unitsGrid, int maxTargets)
  {
    List<Vector2I> found = new List<Vector2I>();
    if (maxTargets <= 0)
      return found;

    if (side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          if (unitsGrid[x, y] != null && unitsGrid[x, y].side != side)
          {
            Vector2I pos = new Vector2I(x, y);
            if (!found.Contains(pos))
              found.Add(pos);
            if (found.Count >= maxTargets)
              return found;
          }
        }
      }
    }
    else
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          if (unitsGrid[x, y] != null && unitsGrid[x, y].side != side)
          {
            Vector2I pos = new Vector2I(x, y);
            if (!found.Contains(pos))
              found.Add(pos);
            if (found.Count >= maxTargets)
              return found;
          }
        }
      }
    }

    return found;
  }
}
