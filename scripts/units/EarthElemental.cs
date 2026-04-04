using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class EarthElemental : Unit
{
  private Node _propsNode = null!;
  private PropInfo _rockInfo = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _rockInfo = GlobalConstants.PropsData["rock"];
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> randomUnit = units
    .Where(u => u.Side != Side)
    .OrderBy(_ => GD.Randi())
    .Take(1)
    .Select(u => u.OccupiedMainCell)
    .ToList();

    return randomUnit;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-Damage, this);
      }

      Prop targetProp = propsGrid[target.X, target.Y];
      if (targetProp != null)
      {
        targetProp.ChangeHealth(-Damage, this);
      }

      // Spawn rock
      if (targetUnit == null)
        continue;
      foreach (Vector2I cell in targetUnit.GetSurroundingCells())
      {
        if (!GlobalFunctions.CanSpawnProp(_rockInfo, cell, unitsGrid, terrainGrid, propsGrid))
          continue;

        Prop propInstance = GD.Load<PackedScene>(_rockInfo.ScenePath).Instantiate() as Prop;
        propInstance!.Initialize(_rockInfo, cell);
        _propsNode.AddChild(propInstance);
        propInstance.SpawnFloatingText("Created", Colors.Green);
        break;
      }
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
