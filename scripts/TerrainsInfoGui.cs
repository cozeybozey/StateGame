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
  private HFlowContainer _types = null!;
  private Terrain? _selectedTerrain = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _blocking = GetNode<Label>("Info/Stats/Blocking/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<HFlowContainer>("Info/Types");
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
    _blocking.Text = terrainInfo.Blocking ? "Yes" : "No";
    _description.Text = terrainInfo.Description.ToString();

    UiHelpers.ShowTags(_types, terrainInfo.Types);
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
