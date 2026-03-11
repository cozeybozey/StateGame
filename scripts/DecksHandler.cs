using Godot;
using Godot.NativeInterop;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class DecksHandler : Control
{
  private Dictionary<string, UnitInfo[,]> _decks = new Dictionary<string, UnitInfo[,]>();
  private List<UnitInfo> _availableUnits = new List<UnitInfo>();
  private Dictionary<string, int> _amountPerUnit = new Dictionary<string, int>();
  private string _selectedDeck;
  private string _unitsSelectionScenePath = "res://scenes/units/unit_selection.tscn";

  private GlobalSignals _globalSignals;
  private GridOverlay _gridOverlay;

  // Decks container
  private VBoxContainer _decksContainer;
  private Button _createDeckButton;
  private VBoxContainer _decksList;
  private AcceptDialog _popup;
  private LineEdit _nameInput;

  // Units container
  private VBoxContainer _unitsSelectionContainer;
  private VBoxContainer _unitsList;
  private Button _changeDeckButton;

  public override void _Ready()
  {
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _gridOverlay = GetTree().CurrentScene.GetNode<GridOverlay>("GridOverlay");

    // Decks container
    _decksContainer = GetNode<VBoxContainer>("DecksContainer");
    _createDeckButton = GetNode<Button>("DecksContainer/CreateDeck");
    _decksList = GetNode<VBoxContainer>("DecksContainer/ScrollContainer/VBoxContainer");
    _popup = GetNode<AcceptDialog>("DecksContainer/AcceptDialog");
    _nameInput = GetNode<LineEdit>("DecksContainer/AcceptDialog/LineEdit");

    // Units container
    _unitsSelectionContainer = GetNode<VBoxContainer>("UnitsSelectionContainer");
    _unitsList = GetNode<VBoxContainer>("UnitsSelectionContainer/ScrollContainer/VBoxContainer");
    _changeDeckButton = GetNode<Button>("UnitsSelectionContainer/ChangeDeck");

    _globalSignals.UnitSpawned += OnUnitSpawned;
    _globalSignals.UnitRemoved += OnUnitRemoved;
    _globalSignals.UnitMoved += OnUnitMoved;
    _createDeckButton.Pressed += OnCreateDeckPressed;
    _changeDeckButton.Pressed += OnChangeDeckPressed;
    _popup.Confirmed += OnPopupConfirmed;
  }

  private void OnUnitSpawned(Unit unit, bool playing)
  {
    // Do not process enemy units and spawned units during simulation
    if (!unit.side || playing)
      return;

    UnitInfo unitInfo = unit.GetStartInfo();
    _decks[_selectedDeck][unit.startCell.X, unit.startCell.Y] = unitInfo;
  }

  private void OnUnitRemoved(Unit unit)
  {
    _decks[_selectedDeck][unit.startCell.X, unit.startCell.Y] = null!;
  }

  private void OnUnitMoved(Unit unit, Vector2I oldCell, bool playing)
  {
    // Do not process enemy units and spawned units during simulation
    if (!unit.side || playing)
      return;

    UnitInfo unitInfo = unit.GetStartInfo();
    _decks[_selectedDeck][oldCell.X, oldCell.Y] = null!;
    _decks[_selectedDeck][unit.startCell.X, unit.startCell.Y] = unitInfo;
  }

  private void OnCreateDeckPressed()
  {
    _nameInput.Clear();
    _popup.PopupCentered();
  }

  private void OnPopupConfirmed()
  {
    string deckName = _nameInput.Text.Trim();
    if (string.IsNullOrEmpty(deckName)) return;

    Button deckButton = new();
    deckButton.Text = deckName;
    _decksList.AddChild(deckButton);
    _decks[deckName] = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    deckButton.Pressed += () => OnDeckSelected(deckButton);
  }

  private void OnDeckSelected(Button button)
  {
    _selectedDeck = button.Text;
    _gridOverlay.LoadDeck(_decks[_selectedDeck]);
    _decksContainer.Hide();
    _unitsSelectionContainer.Show();
    AddUnitsSelection(_decks[_selectedDeck]);
  }

  private void OnChangeDeckPressed()
  {
    ClearUnitsSelection();
    _decksContainer.Show();
    _unitsSelectionContainer.Hide();
  }

  private void AddUnitsSelection(UnitInfo[,] unitsGrid)
  {
    Dictionary<string, int> _amountPlacedPerUnit = new Dictionary<string, int>();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        UnitInfo unitInfo = unitsGrid[x, y];
        if (unitInfo != null)
        {
          if (_amountPlacedPerUnit.ContainsKey(unitInfo.Id))
            _amountPlacedPerUnit[unitInfo.Id] += 1;
          else
            _amountPlacedPerUnit[unitInfo.Id] = 1;
        }
      }
    }

    foreach (UnitInfo availableUnit in _availableUnits)
    {
      // Add initial turrent selection unit
      UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
      unitGui!.Info = availableUnit;
      unitGui.Amount = _amountPerUnit[availableUnit.Id];
      if (_amountPlacedPerUnit.ContainsKey(availableUnit.Id))
        unitGui.Amount -= _amountPlacedPerUnit[availableUnit.Id];
      _unitsList.AddChild(unitGui);
    }
  }

  private void ClearUnitsSelection()
  {
    foreach (var child in _unitsList.GetChildren())
    {
      child.QueueFree();
    }
  }

  public void ResetUnitsSelection()
  {
    ClearUnitsSelection();
    AddUnitsSelection(_decks[_selectedDeck]);
  }

  public void AddUnit(UnitInfo unitInfo)
  {
    if (_amountPerUnit.ContainsKey(unitInfo.Id))
      _amountPerUnit[unitInfo.Id]++;
    else
    {
      _availableUnits.Add(unitInfo);
      _amountPerUnit[unitInfo.Id] = 1;
    }
  }
}