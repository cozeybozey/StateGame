using Godot;
using System;
using Godot.Collections;

public partial class UnitsInfoGui : VBoxContainer
{
  private Label _emptyContainer = null!;
  private VBoxContainer _info = null!;
  private Label _name = null!;
  private TextureRect _texture = null!;
  private Label _health = null!;
  private Label _damage = null!;
  private Label _armor = null!;
  private Label _speed = null!;
  private Label _cooldown = null!;
  private Label _unitSlots = null!;
  private Label _description = null!;
  private VBoxContainer _types = null!;
  public Unit? SelectedUnit = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _health = GetNode<Label>("Info/Health/Value");
    _damage = GetNode<Label>("Info/Damage/Value");
    _armor = GetNode<Label>("Info/Armor/Value");
    _speed = GetNode<Label>("Info/Speed/Value");
    _cooldown = GetNode<Label>("Info/Cooldown/Value");
    _unitSlots = GetNode<Label>("Info/UnitSlots/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<VBoxContainer>("Info/Types");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (SelectedUnit != null)
      DisplayInfo(SelectedUnit.GetInfo());
  }

  public void DisplayInfo(UnitInfo unitInfo)
  {
    _name.Text = unitInfo.Name;
    _texture.Texture = unitInfo.Texture;
    _health.Text = unitInfo.Health.ToString() + '/' + unitInfo.MaxHealth.ToString();
    _damage.Text = unitInfo.Damage.ToString();
    _armor.Text = unitInfo.Armor.ToString();
    _speed.Text = unitInfo.Speed.ToString();
    _cooldown.Text = unitInfo.Cooldown.ToString() + '/' + unitInfo.StartCooldown.ToString();
    _unitSlots.Text = unitInfo.OccupiedCells.Count.ToString();
    _description.Text = unitInfo.Description.ToString();

    foreach (var child in _types.GetChildren())
      child.QueueFree();
    foreach (string type in unitInfo.Types)
    {
      // Upper case the first letter TODO AKN make this nicer
      string displayType = "  - ";
      displayType += string.IsNullOrEmpty(type) ? type : char.ToUpper(type[0]) + type.Substring(1);
      Label label = new Label() { Text = displayType };
      _types.AddChild(label);
    }
  }

  public void SetSelectedUnit(Unit? unit)
  {
    SelectedUnit = unit;
    if (SelectedUnit != null)
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

  public void ResetSelectedUnit()
  {
    SelectedUnit = null;
    _info.Hide();
    _emptyContainer.Show();
  }

  public void ShowInfo()
  {
    _info.Show();
    _emptyContainer.Hide();
  }
}