using Godot;
using System;

public partial class FloatingText : Label
{
  [Export]
  public float FloatDistance = 30.0f;

  [Export]
  public float Lifetime = 1.0f;

  private Vector2 _startPosition;
  private float _time = 0.0f;

  public override void _Ready()
  {
    _startPosition = Position;

    // Fully visible
    Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);
  }

  public override void _Process(double delta)
  {
    _time += (float)delta;

    //float t = _time / Lifetime;

    //// Move up
    //Position = new Vector2(
    //    _startPosition.X,
    //    _startPosition.Y - t * FloatDistance
    //);

    //// Fade out
    //Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f - t);

    float t = Mathf.Clamp(_time / Lifetime, 0f, 1f);

    Position = _startPosition + new Vector2(0, -t * FloatDistance);
    Modulate = Modulate with { A = 1.0f - t };

    // Delete after lifetime
    if (_time >= Lifetime)
    {
      QueueFree();
    }
  }
}