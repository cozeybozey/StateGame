using Godot;
using System;
using System.Collections.Generic;

public partial class Tank : Unit
{
  public override bool CanAct()
  {
    return false;
  }
}
