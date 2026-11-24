using Godot;
using System;

public partial class FloatingText : Label
{
  [Export]
  public float FloatDistance = 26.0f;

  [Export]
  public float Lifetime = 1.0f;

  private Vector2 _startPosition;
  private float _time = 0.0f;

  public override void _Ready()
  {
    // Center on the spawn point, with a small sideways spread so texts on one unit stay apart
    Size = GetCombinedMinimumSize();
    PivotOffset = Size / 2;
    _startPosition = Position + new Vector2(-Size.X / 2 + (float)GD.RandRange(-6.0, 6.0), -6);
    Position = _startPosition;
    Scale = new Vector2(0.6f, 0.6f);
    Modulate = Modulate with { A = 1.0f };
  }

  public override void _Process(double delta)
  {
    _time += (float)delta;
    float t = Mathf.Clamp(_time / Lifetime, 0f, 1f);

    // Pop in, slow down while rising, and only fade during the second half
    Scale = Vector2.One * Mathf.Min(1.0f, 0.6f + t * 4.0f);
    Position = _startPosition + new Vector2(0, -FloatDistance * Mathf.Sqrt(t));
    Modulate = Modulate with { A = 1.0f - Mathf.Clamp((t - 0.5f) * 2.0f, 0f, 1f) };

    if (_time >= Lifetime)
      QueueFree();
  }
}
