using Godot;
using System;
using Godot.Collections;

public partial class TerrainsInfoGui : VBoxContainer
{
  private Label _emptyContainer = null!;
  private VBoxContainer _info = null!;
  private Label _name = null!;
  private TextureRect _texture = null!;
  private Label _blocking = null!;
  private Label _description = null!;
  private VBoxContainer _types = null!;
  private Terrain? _selectedTerrain = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _blocking = GetNode<Label>("Info/Blocking/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<VBoxContainer>("Info/Types");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (_selectedTerrain != null)
      DisplayInfo(_selectedTerrain.GetInfo());
  }

  private void DisplayInfo(TerrainInfo terrainInfo)
  {
    _name.Text = terrainInfo.Name;
    _texture.Texture = terrainInfo.Texture;
    _blocking.Text = terrainInfo.Blocking ? "yes" : "no";
    _description.Text = terrainInfo.Description.ToString();

    foreach (var child in _types.GetChildren())
      child.QueueFree();
    foreach (string type in terrainInfo.Types)
    {
      // Upper case the first letter TODO AKN make this nicer
      string displayType = "  - ";
      displayType += string.IsNullOrEmpty(type) ? type : char.ToUpper(type[0]) + type.Substring(1);
      Label label = new Label() { Text = displayType };
      _types.AddChild(label);
    }
  }

  public void SetSelectedTerrain(Terrain? terrain)
  {
    _selectedTerrain = terrain;
    if (_selectedTerrain != null)
    {
      _info.Show();
      _emptyContainer.Hide();
    }
    else
    {
      _info.Hide();
      _emptyContainer.Show();
    }
  }

  public void ResetSelectedTerrain()
  {
    _selectedTerrain = null;
    _info.Hide();
    _emptyContainer.Show();
  }
}