using Godot;
using System;
using Godot.Collections;

public partial class UnitsInfoGui : VBoxContainer
{
  private Label _emptyContainer = null!;
  private VBoxContainer _info = null!;
  private Label _name = null!;
  private Label _rarity = null!;
  private TextureRect _texture = null!;
  private Label _health = null!;
  private Label _damage = null!;
  private Label _armor = null!;
  private Label _speed = null!;
  private Label _cooldown = null!;
  private Label _unitSlots = null!;
  private Label _description = null!;
  private HFlowContainer _types = null!;
  public Unit? SelectedUnit = null!;

  public override void _Ready()
  {
    _emptyContainer = GetNode<Label>("EmptyContainer");
    _info = GetNode<VBoxContainer>("Info");
    _name = GetNode<Label>("Info/Id/Name");
    _rarity = GetNode<Label>("Info/Rarity");
    _texture = GetNode<TextureRect>("Info/Id/Texture");
    _health = GetNode<Label>("Info/Stats/Health/Value");
    _damage = GetNode<Label>("Info/Stats/Damage/Value");
    _armor = GetNode<Label>("Info/Stats/Armor/Value");
    _speed = GetNode<Label>("Info/Stats/Speed/Value");
    _cooldown = GetNode<Label>("Info/Stats/Cooldown/Value");
    _unitSlots = GetNode<Label>("Info/Stats/UnitSlots/Value");
    _description = GetNode<Label>("Info/Description/Value");
    _types = GetNode<HFlowContainer>("Info/Types");
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
    _rarity.Text = UiHelpers.Capitalize(unitInfo.Rarity);
    _rarity.AddThemeColorOverride("font_color", GamePalette.Rarity(unitInfo.Rarity));
    _texture.Texture = unitInfo.Texture;
    _health.Text = unitInfo.Health.ToString() + '/' + unitInfo.MaxHealth.ToString();
    _damage.Text = unitInfo.Damage.ToString();
    _armor.Text = unitInfo.Armor.ToString();
    _speed.Text = unitInfo.Speed.ToString();
    // Time between actions at 1x speed, plus how full the action bar is for a unit on the board
    string interval = unitInfo.Speed > 0
      ? FormattableString.Invariant($"{(float)GlobalConstants.ActionBarSize / unitInfo.Speed / GlobalConstants.TicksPerSecond:0.0}s")
      : "never";
    if (SelectedUnit != null && IsInstanceValid(SelectedUnit))
      interval += $" ({100 * SelectedUnit.ActionProgress / GlobalConstants.ActionBarSize}%)";
    _cooldown.Text = interval;
    _unitSlots.Text = unitInfo.OccupiedCells.Count.ToString();
    _description.Text = unitInfo.Description.ToString();

    UiHelpers.ShowTags(_types, unitInfo.Types);
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
