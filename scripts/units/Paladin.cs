using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Paladin : Unit
{
  private GlobalSignals? _globalSignals;

  protected override void Start()
  {
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _globalSignals.HealingReceived += OnUnitReceivedHealing;
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = base.GetTargets(unitsGrid, terrainGrid, propsGrid, units, deadUnits);
    if (result.Count == 0)
      return result;
    Vector2I primary = result[0];

    // Determine direction for the extra cells
    Unit? primaryUnit = unitsGrid[primary.X, primary.Y];
    bool preferUpper = false; // prefer lower y values (upper on screen) if target is on enemy side
    if (primaryUnit != null)
    {
      // If the target unit belongs to the enemy side (not the paladin's side), prioritize lower y values
      preferUpper = (primaryUnit.Side == false);
    }

    // Build a 2x2 square that includes the primary cell and prefers the chosen vertical direction.
    int x = primary.X;
    int y = primary.Y;

    // Choose top-left corner x coordinate so the square includes x and fits in grid
    int x0 = x;
    if (x0 >= GlobalConstants.GridSize.X - 1)
      x0 = GlobalConstants.GridSize.X - 2;
    if (x0 < 0)
      x0 = 0;

    // Choose top-left corner y coordinate based on preference
    int y0;
    if (preferUpper)
    {
      // prefer upper / lower y values: try to place square above the target (smaller y)
      y0 = y - 1;
    }
    else
    {
      // prefer lower on screen (higher y): place square starting at the target y
      y0 = y;
    }

    // Clamp y0 to valid range so square fits
    if (y0 < 0) y0 = 0;
    if (y0 >= GlobalConstants.GridSize.Y - 1) y0 = GlobalConstants.GridSize.Y - 2;

    // Add the 2x2 square cells
    result.Add(new Vector2I(x0, y0));
    result.Add(new Vector2I(x0 + 1, y0));
    result.Add(new Vector2I(x0, y0 + 1));
    result.Add(new Vector2I(x0 + 1, y0 + 1));

    // Ensure all cells are inside grid (defensive)
    List<Vector2I> filtered = new();
    foreach (var cell in result)
      if (GlobalFunctions.IsCellInsideGrid(cell) && !filtered.Contains(cell))
        filtered.Add(cell);

    return filtered;
  }

  private void OnUnitReceivedHealing(GridEntity gridEntity, int amount)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit)
      return;

    // Gain one damage whenever an ally is healed
    if (unit.Side == Side && amount > 0)
      ChangeDamage(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.HealingReceived -= OnUnitReceivedHealing;
  }
}
