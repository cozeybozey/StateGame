using Godot;
using Godot.Collections;
using System;
using System.Runtime;

public partial class PropsInfoGui : VBoxContainer
{
  private Label _emptyContainer = null!;
  private VBoxContainer _info = null!;
  private Label _name = null!;
  private TextureRect _texture = null!;
  private Label _health = null!;
  private Label _damage = null!;
  private Label _armor = null!;
  private Label _damagable = null!;
  private Label _movable = null!;
  private Label _blocking = null!;
  private Label _description = null!;
  private HFlowContainer _types = null!;
  private Prop? _selectedProp = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _health = GetNode<Label>("Info/Stats/Health/Value");
    _damage = GetNode<Label>("Info/Stats/Damage/Value");
    _armor = GetNode<Label>("Info/Stats/Armor/Value");
    _damagable = GetNode<Label>("Info/Stats/Damagable/Value");
    _movable = GetNode<Label>("Info/Stats/Movable/Value");
    _blocking = GetNode<Label>("Info/Stats/Blocking/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<HFlowContainer>("Info/Types");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (_selectedProp != null)
      DisplayInfo(_selectedProp.GetInfo());
  }

  private void DisplayInfo(PropInfo propInfo)
  {
    _name.Text = propInfo.Name;
    _texture.Texture = propInfo.Texture;
    _health.Text = propInfo.Health.ToString() + '/' + propInfo.MaxHealth.ToString();
    _damage.Text = propInfo.Damage.ToString();
    _armor.Text = propInfo.Armor.ToString();
    _damagable.Text = propInfo.Damagable ? "Yes" : "No";
    _movable.Text = propInfo.Movable ? "Yes" : "No";
    _blocking.Text = propInfo.Blocking ? "Yes" : "No";
    _description.Text = propInfo.Description.ToString();

    UiHelpers.ShowTags(_types, propInfo.Types);
  }

  public void SetSelectedProp(Prop? prop)
  {
    _selectedProp = prop;
    if (_selectedProp != null)
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

  public void ResetSelectedProp()
  {
    _selectedProp = null;
    _info.Hide();
    _emptyContainer.Show();
  }
}
