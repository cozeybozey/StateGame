using Godot;

public partial class GlobalSignals : Node
{
  [Signal]
  public delegate void UnitDiedEventHandler(Unit unit);

  [Signal]
  public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldCell);
}