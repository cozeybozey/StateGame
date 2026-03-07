using Godot;
using System;
using System.Collections.Generic;

public partial class Zombie : Unit
{
  public override bool CanAct()
  {
    return false;
  }
}
