using Godot;

public partial class DragPayload : RefCounted
{
    public UnitInfo Unit;
    public Node Source;
    public Vector2I? OriginCell;
}
