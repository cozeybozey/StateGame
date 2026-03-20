using Godot;
using System;
using System.Collections.Generic;

public partial class Masochist : Unit
{
  public override void ChangeHealth(int amount, Unit? unit)
  {
    base.ChangeHealth(amount, unit);
    if (amount < 0)
      ChangeDamage(Mathf.FloorToInt(-amount));
  }
}
