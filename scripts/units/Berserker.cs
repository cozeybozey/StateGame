using Godot;
using System;
using System.Collections.Generic;

public partial class Berserker : Unit
{
  public override void ChangeHealth(int amount)
  {
    int extraDamage = maxHealth - health;
    base.ChangeHealth(amount);
    if (amount > 0)
      ChangeDamage(Mathf.Min(amount, extraDamage));
  }
}
