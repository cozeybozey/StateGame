using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UnitsSelectionContainer : VBoxContainer
{
  private VBoxContainer _unitGuisContainer;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _unitGuisContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
  }

  public override bool _CanDropData(Vector2 atPosition, Variant data)
  {
    if (data.Obj is not DragPayload dragPayload)
      return false;

    return true;
  }

  public override void _DropData(Vector2 atPosition, Variant data)
  {
    if (data.Obj is not DragPayload dragPayload)
      return;

    foreach (UnitGui unitGui in _unitGuisContainer.GetChildren().Cast<UnitGui>())
    {
      if (unitGui.Info == dragPayload.Unit)
      {
        unitGui.UpdateAmount(unitGui.Amount + 1);
        break;
      }
    }

    // Notify original source if it exists and isn’t this UI
    if (dragPayload.Source is IUnitDragSource source && dragPayload.Source != this)
    {
      source.OnUnitPlacedSuccessfully(dragPayload);
    }
  }
}
