using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class PutridAlchemist : Unit
{
  private bool _attackTargetFound = false;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new();
    int frontDir = Side ? -1 : 1;

    // Find frontmost enemy in same column
    foreach (Vector2I cell in GetOccupiedCells())
    {
      int checkY = cell.Y + frontDir;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(cell.X, checkY)))
      {
        Unit unit = unitsGrid[cell.X, checkY];
        if (unit != null)
        {
          targets.Add(unit.OccupiedMainCell);
          _attackTargetFound = true;
          break;
        }
        checkY += frontDir;
      }
    }

    // Find frontmost allied unit
    Unit? frontmostAlly = units
        .Where(u => u.Side == Side && u.Health < u.MaxHealth)
        .OrderByDescending(u => Side ? u.OccupiedMainCell.Y : -u.OccupiedMainCell.Y)
        .ThenBy(u => u.OccupiedMainCell.X)
        .FirstOrDefault();

    if (frontmostAlly != null)
      targets.Add(frontmostAlly.OccupiedMainCell);

    return targets;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0) return;

    // Deal damage to all enemy targets (all except last which is the ally)
    if (_attackTargetFound)
    {
      int damageDealt = 0;
      Unit unitToAttack = unitsGrid[targets[0].X, targets[0].Y];
      if (unitToAttack != null)
        damageDealt = unitToAttack.ChangeHealth(-Damage, this);

      if (damageDealt > 0 && targets.Count > 1)
      {
        Unit unitToHeal = unitsGrid[targets[1].X, targets[1].Y];
        if (unitToHeal != null)
          unitToHeal.ChangeHealth(Damage, this);
      }
    }

    _attackTargetFound = false;
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Negative score for each unit in front, because this unit deals damage to the first unit it sees
    foreach (Vector2I cell in unitInfo.OccupiedCells)
    {
      Vector2I newPos = pos + cell;
      int checkY = newPos.Y + 1;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(newPos.X, checkY)))
      {
        UnitInfo unit = unitsGrid[newPos.X, checkY];
        if (unit != null)
        {
          score -= 5;
        }
        checkY += 1;
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
