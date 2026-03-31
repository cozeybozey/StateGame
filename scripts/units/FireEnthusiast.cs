using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class FireEnthusiast : Unit
{

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0) return;

    Unit target = unitsGrid[targets[0].X, targets[0].Y];
    if (target == null) return;

    int fireBonus = 0;
    // Count burning props
    List<Prop> countedProps = new List<Prop>();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        Prop prop = propsGrid[x, y];
        if (prop != null && prop.Types.Contains("burning") && !countedProps.Contains(prop))
        {
          fireBonus++;
          countedProps.Add(prop);
        }
      }
    }

    target.ChangeHealth(-Damage - fireBonus, this);
  }
}
