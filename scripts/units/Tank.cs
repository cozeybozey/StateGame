using Godot;
using System;
using System.Collections.Generic;

public partial class Tank : Unit
{
  public override int maxHealth { get; set; } = 5;
  public override int health { get; set; } = 5;
  public override int damage { get; set; } = 0;
  public override int armor { get; set; } = 1;
  public override int startingCooldown { get; set; } = 0;
  public override int cooldown { get; set; } = 0;
  public override int speed { get; set; } = 0;

  public override bool CanAct()
  {
    return false;
  }
}
