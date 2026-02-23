using Godot;
using System;
using System.Collections.Generic;

public partial class Turret : Unit
{
  public override int damage { get; set; } = 4;
  public override int speed { get; set; } = 5;
}
