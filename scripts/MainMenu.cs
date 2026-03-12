using Godot;
using System;

public partial class MainMenu : Panel
{
  private Button _optionsButton;
  private Button _saveGameButton;
  private Button _loadGameButton;
  private Button _quitGameButton;
  private Button _closeButton;
  private DecksHandler _decksHandler;
  private World _world;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _optionsButton = GetNode<Button>("Buttons/OptionsButton");
    _saveGameButton = GetNode<Button>("Buttons/SaveGameButton");
    _loadGameButton = GetNode<Button>("Buttons/LoadGameButton");
    _quitGameButton = GetNode<Button>("Buttons/QuitGameButton");
    _closeButton = GetNode<Button>("Buttons/CloseButton");
    _decksHandler = GetTree().CurrentScene.GetNode<DecksHandler>("CanvasLayer/SelectionUi/HBoxContainer/DecksHandler");
    _world = GetNode<World>("/root/World");

    _optionsButton.Pressed += OnOptionsButtonPressed;
    _saveGameButton.Pressed += OnSaveGameButtonPressed;
    _loadGameButton.Pressed += OnLoadGameButtonPressed;
    _quitGameButton.Pressed += OnQuitGameButtonPressed;
    _closeButton.Pressed += OnCloseButtonPressed;
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
  }

  private void OnOptionsButtonPressed()
  {

  }

  private void OnSaveGameButtonPressed()
  {
    var saveData = new Godot.Collections.Dictionary();

    // Save decks
    var decksData = new Godot.Collections.Dictionary();
    foreach (var deck in _decksHandler.Decks)
    {
      var grid = new Godot.Collections.Array();
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          UnitInfo unit = deck.Value[x, y];
          if (unit == null) continue;
          var unitData = new Godot.Collections.Dictionary()
                {
                    { "x", x },
                    { "y", y },
                    { "id", unit.Id },
                };
          grid.Add(unitData);
        }
      decksData[deck.Key] = grid;
    }
    saveData["decks"] = decksData;

    // Save available units
    var availableUnitsData = new Godot.Collections.Array();
    foreach (UnitInfo unitInfo in _decksHandler.AvailableUnits)
    {
      var unitData = new Godot.Collections.Dictionary()
                {
                    { "id", unitInfo.Id },
                    { "amount", _decksHandler.AmountPerUnit[unitInfo.Id] },
                };
      availableUnitsData.Add(unitData);
    }
    saveData["units"] = availableUnitsData;

    // Save levels
    var levelsData = new Godot.Collections.Dictionary();
    foreach (var level in _world.Levels)
    {
      var unitGrid = new Godot.Collections.Array();
      if (level.Value.Units != null)
      {
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        {
          for (int x = 0; x < GlobalConstants.GridSize.X; x++)
          {
            UnitInfo unit = level.Value.Units[x, y];
            if (unit == null) continue;
            var unitData = new Godot.Collections.Dictionary()
                    {
                        { "x", x },
                        { "y", y },
                        { "id", unit.Id },
                    };
            unitGrid.Add(unitData);
          }
        }
      }

      var nextNodesList = new Godot.Collections.Array();
      foreach (string nextNode in level.Value.NextNodes)
        nextNodesList.Add(nextNode);

      var levelData = new Godot.Collections.Dictionary()
        {
            { "id", level.Value.Id },
            { "name", level.Value.Name },
            { "completed", level.Value.Completed },
            { "unlocked", level.Value.Unlocked },
            { "layer", level.Value.Layer },
            { "layerIndex", level.Value.LayerIndex },
            { "isBoss", level.Value.IsBoss },
            { "nextNodes", nextNodesList },
            { "units", unitGrid },
        };
      levelsData[level.Key] = levelData;
    }
    saveData["levels"] = levelsData;

    // Write to file
    string json = Json.Stringify(saveData, "\t");
    using var file = FileAccess.Open("user://savegame.json", FileAccess.ModeFlags.Write);
    file.StoreString(json);

    GD.Print("Game saved.");
  }

  private void OnLoadGameButtonPressed()
  {
    if (!FileAccess.FileExists("user://savegame.json"))
    {
      GD.Print("No save file found.");
      return;
    }

    using var file = FileAccess.Open("user://savegame.json", FileAccess.ModeFlags.Read);
    string json = file.GetAsText();
    var saveData = Json.ParseString(json).AsGodotDictionary();

    // Load decks
    _decksHandler.Decks.Clear();
    var decksData = saveData["decks"].AsGodotDictionary();
    foreach (var deckKey in decksData.Keys)
    {
      string deckName = deckKey.AsString();
      _decksHandler.Decks[deckName] = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
      var grid = decksData[deckKey].AsGodotArray();
      foreach (var unitEntry in grid)
      {
        var unitData = unitEntry.AsGodotDictionary();
        int x = unitData["x"].AsInt32();
        int y = unitData["y"].AsInt32();
        string id = unitData["id"].AsString();
        _decksHandler.Decks[deckName][x, y] = _world.UnitsData[id];
      }
    }

    // Load available units
    _decksHandler.AvailableUnits.Clear();
    _decksHandler.AmountPerUnit.Clear();
    var availableUnitsData = saveData["units"].AsGodotArray();
    foreach (var unitEntry in availableUnitsData)
    {
      var unitData = unitEntry.AsGodotDictionary();
      string id = unitData["id"].AsString();
      int amount = unitData["amount"].AsInt32();
      UnitInfo unitInfo = _world.UnitsData[id];
      _decksHandler.AvailableUnits.Add(unitInfo);
      _decksHandler.AmountPerUnit[id] = amount;
    }
    _decksHandler.LoadDecks();

    // Load levels
    //var levelsData = saveData["levels"].AsGodotDictionary();
    //foreach (var levelKey in levelsData.Keys)
    //{
    //  string levelId = levelKey.AsString();
    //  if (!_world.Levels.ContainsKey(levelId)) continue;

    //  var levelData = levelsData[levelKey].AsGodotDictionary();
    //  LevelInfo level = _world.Levels[levelId];

    //  level.Completed = levelData["completed"].AsBool();
    //  level.Unlocked = levelData["unlocked"].AsBool();

    //  var unitGrid = levelData["units"].AsGodotArray();
    //  level.Units = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
    //  foreach (var unitEntry in unitGrid)
    //  {
    //    var unitData = unitEntry.AsGodotDictionary();
    //    int x = unitData["x"].AsInt32();
    //    int y = unitData["y"].AsInt32();
    //    string id = unitData["id"].AsString();
    //    level.Units[x, y] = _world.UnitsData[id];
    //  }
    //}

    GD.Print("Game loaded.");
  }

  private void OnQuitGameButtonPressed()
  {
    GetTree().Quit();
  }

  private void OnCloseButtonPressed()
  {
    Visible = false;
  }
}
