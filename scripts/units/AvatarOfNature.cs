using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class AvatarOfNature : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new List<Vector2I>();
    List<Unit> targetedUnits = new List<Unit>();
    List<Prop> countedProps = new List<Prop>();
    
    // Determine number of nature props
    int numNatureProps = 0;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        Prop prop = propsGrid[x, y];
        if (prop != null && prop.Types.Contains("nature") && !countedProps.Contains(prop))
        {
          numNatureProps++;
          countedProps.Add(prop);
        }
      }
    }

    if (Side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Unit unit = unitsGrid[x, y];
          if (unit != null && unit.Side != Side && !targetedUnits.Contains(unit))
          {
            targets.Add(new Vector2I(x, y));
            targetedUnits.Add(unit);
            if (targetedUnits.Count >= numNatureProps)
              return targets;
          }
        }
      }
    }
    else
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Unit unit = unitsGrid[x, y];
          if (unit != null && unit.Side != Side && !targetedUnits.Contains(unit))
          {
            targets.Add(new Vector2I(x, y));
            targetedUnits.Add(unit);
            if (targetedUnits.Count >= numNatureProps)
              return targets;
          }
        }
      }
    }

    return targets;
  }
}
