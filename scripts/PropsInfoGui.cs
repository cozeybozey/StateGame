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
  private VBoxContainer _types = null!;
  private Prop? _selectedProp = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _health = GetNode<Label>("Info/Health/Value");
    _damage = GetNode<Label>("Info/Damage/Value");
    _armor = GetNode<Label>("Info/Armor/Value");
    _damagable = GetNode<Label>("Info/Damagable/Value");
    _movable = GetNode<Label>("Info/Movable/Value");
    _blocking = GetNode<Label>("Info/Blocking/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<VBoxContainer>("Info/Types");
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
    _damagable.Text = propInfo.Damagable ? "yes" : "no";
    _movable.Text = propInfo.Movable ? "yes" : "no";
    _blocking.Text = propInfo.Blocking ? "yes" : "no";
    _description.Text = propInfo.Description.ToString();

    foreach (var child in _types.GetChildren())
      child.QueueFree();
    foreach (string type in propInfo.Types)
    {
      // Upper case the first letter TODO AKN make this nicer
      string displayType = "  - ";
      displayType += string.IsNullOrEmpty(type) ? type : char.ToUpper(type[0]) + type.Substring(1);
      Label label = new Label() { Text = displayType };
      _types.AddChild(label);
    }
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