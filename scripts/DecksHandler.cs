using Godot;
using Godot.NativeInterop;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Principal;

public partial class DecksHandler : Control
{
  public Dictionary<string, UnitInfo[,]> Decks = new Dictionary<string, UnitInfo[,]>();
  public List<UnitInfo> AvailableUnits = new List<UnitInfo>();
  public Dictionary<string, int> AmountPerUnit = new Dictionary<string, int>();

  private string _selectedDeck;
  private string _unitsSelectionScenePath = "res://scenes/units/unit_selection.tscn";
  private bool _processUnitChanges = true;

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

    _globalSignals.GridEntitySpawned += OnUnitSpawned;
    _globalSignals.UnitRemoved += OnUnitRemoved;
    _globalSignals.GridEntityMoved += OnUnitMoved;
    _createDeckButton.Pressed += OnCreateDeckPressed;
    _changeDeckButton.Pressed += OnChangeDeckPressed;
    _popup.Confirmed += OnPopupConfirmed;
  }

  private void OnUnitSpawned(GridEntity gridEntity, bool playing)
  {
    // Do not process spawned terrain
    if (gridEntity is not Unit unit)
      return;

    // Do not process enemy units and spawned units during simulation
    if (!_processUnitChanges)
      return;

    UnitInfo unitInfo = unit.GetStartInfo();
    Decks[_selectedDeck][unit.StartCell.X, unit.StartCell.Y] = unitInfo;
  }

  private void OnUnitRemoved(Unit unit)
  {
    // If units were removed due to a switch deck call,
    // then the deck itself should not be changed
    if (_processUnitChanges)
      Decks[_selectedDeck][unit.StartCell.X, unit.StartCell.Y] = null!;
  }

  private void OnUnitMoved(GridEntity gridEntity, Vector2I oldCell, bool playing)
  {
    // Do not process spawned terrain
    if (gridEntity is not Unit unit)
      return;

    // Do not process enemy units and spawned units during simulation
    if (!_processUnitChanges)
      return;

    UnitInfo unitInfo = unit.GetStartInfo();
    Decks[_selectedDeck][oldCell.X, oldCell.Y] = null!;
    Decks[_selectedDeck][unit.StartCell.X, unit.StartCell.Y] = unitInfo;
  }

  private void OnCreateDeckPressed()
  {
    _nameInput.Clear();
    _popup.PopupCentered();
    _nameInput.GrabFocus();
  }

  private void OnPopupConfirmed()
  {
    string deckName = _nameInput.Text.Trim();
    if (string.IsNullOrEmpty(deckName)) return;

    Button deckButton = new();
    deckButton.Text = deckName;
    _decksList.AddChild(deckButton);
    Decks[deckName] = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    deckButton.Pressed += () => OnDeckSelected(deckButton);
  }

  private void OnDeckSelected(Button button)
  {
    _processUnitChanges = false;
    _selectedDeck = button.Text;
    _gridOverlay.LoadDeck(Decks[_selectedDeck]);
    _decksContainer.Hide();
    _unitsSelectionContainer.Show();
    AddUnitsSelection(Decks[_selectedDeck]);
    _processUnitChanges = true;
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

    foreach (UnitInfo availableUnit in AvailableUnits)
    {
      // Add initial turrent selection unit
      UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
      unitGui!.Info = availableUnit;
      unitGui.Amount = AmountPerUnit[availableUnit.Id];
      if (_amountPlacedPerUnit.ContainsKey(availableUnit.Id))
        unitGui.Amount -= _amountPlacedPerUnit[availableUnit.Id];
      _unitsList.AddChild(unitGui);
    }
  }

  private void AddUnitsSelectionBeforeLevel(UnitInfo[,] unitsGrid)
  {
    List<UnitInfo> units = new List<UnitInfo>();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        UnitInfo unitInfo = unitsGrid[x, y];
        if (unitInfo != null && !units.Contains(unitInfo))
            units.Add(unitInfo);
      }
    }

    foreach (UnitInfo unitInfo in units)
    {
      // Add initial turrent selection unit
      UnitGui unitGui = GD.Load<PackedScene>(_unitsSelectionScenePath).Instantiate() as UnitGui;
      unitGui!.Info = unitInfo;
      unitGui.Amount = 0;
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
    if (Decks.ContainsKey(_selectedDeck))
      AddUnitsSelection(Decks[_selectedDeck]);
  }

  public void LoadBeforeLevel()
  {
    _processUnitChanges = false;
    _changeDeckButton.Visible = false;
    _decksContainer.Hide();
    _unitsSelectionContainer.Show();
    //_gridOverlay.ClearUnits();
    ClearUnitsSelection();
    AddUnitsSelectionBeforeLevel(Decks[_selectedDeck]);
  }

  public void LoadAfterLevel()
  {
    ResetUnitsSelection();
    _gridOverlay.LoadDeck(Decks[_selectedDeck]);
    _changeDeckButton.Visible = true;
    _processUnitChanges = true;
  }

  public void AddUnit(UnitInfo unitInfo)
  {
    if (AmountPerUnit.ContainsKey(unitInfo.Id))
      AmountPerUnit[unitInfo.Id]++;
    else
    {
      AvailableUnits.Add(unitInfo);
      AmountPerUnit[unitInfo.Id] = 1;
    }
  }

  public void LoadDecks()
  {
    // This function is called after main menu already set the public variables here correctly
    foreach (var child in _decksList.GetChildren())
    {
      child.QueueFree();
    }
    _decksContainer.Show();
    _unitsSelectionContainer.Hide();

    string newSelectedDeck = null!;
    foreach (var deck in Decks)
    {
      if (newSelectedDeck == null)
        newSelectedDeck = deck.Key;
      Button deckButton = new();
      deckButton.Text = deck.Key;
      _decksList.AddChild(deckButton);
      deckButton.Pressed += () => OnDeckSelected(deckButton);
    }

    _selectedDeck = newSelectedDeck;
    _gridOverlay.ClearUnits();
    ClearUnitsSelection();
  }
}