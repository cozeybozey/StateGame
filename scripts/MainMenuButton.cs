using Godot;
using System;

public partial class MainMenuButton : Button
{
  private MainMenu _mainMenu;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _mainMenu = GetTree().CurrentScene.GetNode<MainMenu>("CanvasLayer/MainMenu");
    Pressed += OnButtonPressed;
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
  }

  private void OnButtonPressed()
  {
    _mainMenu.Visible = !_mainMenu.Visible;
  }
}
