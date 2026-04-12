using Godot;
using System;
using System.Collections.Generic;

public partial class Wraith : Unit
{
  public override int ChangeHealth(int amount, GridEntity? unit)
  {
    if (amount > 0 || _rng.Randf() < 0.5f)
      return base.ChangeHealth(amount, unit);
    else
    {
      SpawnFloatingText("Missed", Colors.Green);
      return 0;
    }
  }
}
