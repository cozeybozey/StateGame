using Godot;
using System;
using System.Collections.Generic;

public partial class Masochist : Unit
{
  public override int ChangeHealth(int amount, GridEntity? unit)
  {
    int effectiveAmount = base.ChangeHealth(amount, unit);
    if (amount < 0)
      ChangeDamage(Mathf.FloorToInt(-amount));
    return effectiveAmount;
  }
}
