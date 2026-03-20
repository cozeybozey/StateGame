using Godot;
using System;
using System.Collections.Generic;

public partial class GoldenTurret : Unit
{
  private Sprite2D _glowSprite;

  protected override void Start()
  {
    _glowSprite = GetNode<Sprite2D>("Sprite");
    _glowSprite.Modulate = new Color(1f, 0.85f, 0.2f, 0.7f);
    _glowSprite.Scale = new Vector2(1.1f, 1.1f);
    _glowSprite.Material = new CanvasItemMaterial
    {
      BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    damage = 1;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        if (unitsGrid[x, y] != null && unitsGrid[x, y].side == side && unitsGrid[x, y] is Turret)
          damage++;
      }
    }

    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-damage, this);
      }
    }
  }
}
