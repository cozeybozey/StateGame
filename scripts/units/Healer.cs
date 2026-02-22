using Godot;
using System;
using System.Collections.Generic;

public partial class Healer : Unit
{
  public override int maxHealth { get; set; } = 5;
  public override int cooldown { get; set; } = 2;
  public override int startingCooldown { get; set; } = 2;
  public override int damage { get; set; } = 2;

  public override void Act(List<Unit> units)
  {
    cooldown -= 1;
    if (cooldown <= 0)
    {
      List<Vector2I> targets = GetTargets(units);
      cooldown = startingCooldown;
    }
  }

  protected override List<Vector2I> GetTargets(List<Unit> units)
  {
    foreach (var unit in units)
    {
      // Heal the first friendly ally with less than max health
      if (unit.side == side && unit.health < unit.maxHealth)
      {
        unit.ChangeHealth(damage);

        int cellX = Mathf.FloorToInt(unit.GlobalPosition.X / GlobalConstants.TileSize);
        int cellY = Mathf.FloorToInt(unit.GlobalPosition.Y / GlobalConstants.TileSize);

        return [new Vector2I(cellX, cellY)];
      }
    }

    return [];
  }
}
