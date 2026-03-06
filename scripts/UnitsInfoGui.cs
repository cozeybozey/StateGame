using Godot;
using System;
using Godot.Collections;

public partial class UnitsInfoGui : VBoxContainer
{
  private Label _name = null!;
  private TextureRect _texture = null!;
  private Label _health = null!;
  private Label _damage = null!;
  private Label _armor = null!;
  private Label _speed = null!;
  private Label _cooldown = null!;
  private Label _description = null!;
  private GlobalSignals _globalSignals = null!;

  public override void _Ready()
  {
    _name = GetNode<Label>("Id/Name");
    _texture = GetNode<TextureRect>("Id/Texture");
    _health = GetNode<Label>("Health/Value");
    _damage = GetNode<Label>("Damage/Value");
    _armor = GetNode<Label>("Armor/Value");
    _speed = GetNode<Label>("Speed/Value");
    _cooldown = GetNode<Label>("Cooldown/Value");
    _description = GetNode<Label>("Description/Value");
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
  
    _globalSignals.UnitInfoSelected += OnUnitInfoSelected;
  }

  private void OnUnitInfoSelected(UnitInfo unitInfo)
  {
    Visible = true;
    _name.Text = unitInfo.Name;
    _texture.Texture = unitInfo.Texture;
    _health.Text = unitInfo.Health.ToString();
    _damage.Text = unitInfo.Damage.ToString();
    _armor.Text = unitInfo.Armor.ToString();
    _speed.Text = unitInfo.Speed.ToString();
    _cooldown.Text = unitInfo.Cooldown.ToString();
    _description.Text = unitInfo.Description.ToString();
  }
}