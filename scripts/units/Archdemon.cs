using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Archdemon : Unit
{
  private Node _propsNode = null!;
  private PropInfo _fireInfo = null!;

  protected override void Start()
  {
    _globalSignals.GridEntityDied += OnUnitDied;
    _globalSignals.GridEntitySpawned += OnUnitSpawned;
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _fireInfo = GlobalConstants.PropsData["fire"];
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Unit> enemies = units
        .Where(u => u.Side != Side)
        .OrderBy(_ => GD.Randi())
        .Take(3)
        .ToList();

    List<Vector2I> targets = new();
    foreach (Unit enemy in enemies)
    {
      Vector2I center = enemy.OccupiedMainCell;
      targets.Add(center);
      targets.Add(new Vector2I(center.X + 1, center.Y));
      targets.Add(new Vector2I(center.X - 1, center.Y));
      targets.Add(new Vector2I(center.X, center.Y + 1));
      targets.Add(new Vector2I(center.X, center.Y - 1));
    }

    return targets.Where(c => GlobalFunctions.IsCellInsideGrid(c)).Distinct().ToList();
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
        targetUnit.ChangeHealth(-Damage, this);

      Prop targetProp = propsGrid[target.X, target.Y];
      if (targetProp != null && targetProp.Damagable)
        targetProp.ChangeHealth(-Damage, this);

      // Spawn fire
      if (!GlobalFunctions.CanSpawnProp(_fireInfo, target, unitsGrid, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(_fireInfo.ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(_fireInfo, target);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }

  private void OnUnitDied(GridEntity gridEntity)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit)
      return;

    if (unit.Side == Side && unit.Types.Contains("demonic"))
    {
      ChangeDamage(1);
      ChangeSpeed(1);
    }
  }

  private void OnUnitSpawned(GridEntity gridEntity, bool playing)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit || !playing)
      return;

    if (unit.Side == Side && unit.Types.Contains("demonic"))
    {
      ChangeDamage(1);
      ChangeSpeed(1);
    }
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
    {
      _globalSignals.GridEntityDied -= OnUnitDied;
      _globalSignals.GridEntitySpawned -= OnUnitSpawned;
    }
  }
}
