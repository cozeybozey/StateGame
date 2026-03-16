using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class MainMenu : Control
{
  private Panel _mainMenuPanel;
  private Button _optionsButton;
  private Button _saveGameButton;
  private Button _loadGameButton;
  private Button _quitGameButton;
  private Button _closeButton;

  private Panel _saveGamePanel;
  private LineEdit _saveName;
  private Button _confirmSaveButton;
  private Button _cancelSaveButton;

  private Panel _saveGamesPanel;
  private VBoxContainer _saveGames;
  private Label _saveGamesLabel;
  private Button _cancelSaveGamesButton;

  private DecksHandler _decksHandler;
  private World _world;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _mainMenuPanel = GetNode<Panel>("MainMenu");
    _optionsButton = GetNode<Button>("MainMenu/Buttons/OptionsButton");
    _saveGameButton = GetNode<Button>("MainMenu/Buttons/SaveGameButton");
    _loadGameButton = GetNode<Button>("MainMenu/Buttons/LoadGameButton");
    _quitGameButton = GetNode<Button>("MainMenu/Buttons/QuitGameButton");
    _closeButton = GetNode<Button>("MainMenu/Buttons/CloseButton");

    _saveGamePanel = GetNode<Panel>("SaveGamePanel");
    _saveName = GetNode<LineEdit>("SaveGamePanel/VBoxContainer/SaveName");
    _confirmSaveButton = GetNode<Button>("SaveGamePanel/VBoxContainer/Buttons/Confirm");
    _cancelSaveButton = GetNode<Button>("SaveGamePanel/VBoxContainer/Buttons/Cancel");

    _saveGamesPanel = GetNode<Panel>("SaveGamesPanel");
    _saveGames = GetNode<VBoxContainer>("SaveGamesPanel/VBoxContainer/SaveGames");
    _saveGamesLabel = GetNode<Label>("SaveGamesPanel/VBoxContainer/Label");
    _cancelSaveGamesButton = GetNode<Button>("SaveGamesPanel/VBoxContainer/Cancel");

    _decksHandler = GetTree().CurrentScene.GetNode<DecksHandler>("CanvasLayer/SelectionUi/HBoxContainer/DecksHandler");
    _world = GetNode<World>("/root/World");

    _optionsButton.Pressed += OnOptionsButtonPressed;
    _saveGameButton.Pressed += OnSaveGameButtonPressed;
    _loadGameButton.Pressed += OnLoadGameButtonPressed;
    _quitGameButton.Pressed += OnQuitGameButtonPressed;
    _closeButton.Pressed += OnCloseButtonPressed;
    _confirmSaveButton.Pressed += OnConfirmSaveButtonPressed;
    _cancelSaveButton.Pressed += OnCancelSaveButtonPressed;
    _cancelSaveGamesButton.Pressed += OnCancelSaveGamesButtonPressed;
    MouseFilter = MouseFilterEnum.Ignore;
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
    _mainMenuPanel.Hide();
    PopulateSaveFiles(forSaving:true);
    _saveGamesLabel.Text = "Save Game";
    _saveGamesPanel.Show();
    Button button = new();
    button.Text = "New save...";
    button.Pressed += () => OnNewSaveGameButtonPressed();
    _saveGames.AddChild(button);
  }

  private void OnNewSaveGameButtonPressed()
  {
    _saveGamePanel.Show();
    _saveName.GrabFocus();
  }

  private void OnConfirmSaveButtonPressed()
  {
    SaveGame("user://" + _saveName.Text + ".json");
    _saveName.Text = "";
    _saveGamePanel.Hide();
    _saveGamesPanel.Hide();
  }

  private void OnCancelSaveButtonPressed()
  {
    _saveName.Text = "";
    _saveGamePanel.Hide();
  }

  private void PopulateSaveFiles(bool forSaving = false)
  {
    // Clear existing buttons
    foreach (Node child in _saveGames.GetChildren())
      child.QueueFree();

    using var dir = DirAccess.Open("user://");
    if (dir == null) return;

    dir.ListDirBegin();
    string fileName = dir.GetNext();
    while (fileName != "")
    {
      if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
      {
        string captured = fileName;
        Button button = new();
        button.Text = fileName.Replace(".json", "");
        button.Pressed += () => OnSaveFileSelected(captured, forSaving);
        _saveGames.AddChild(button);
      }
      fileName = dir.GetNext();
    }
    dir.ListDirEnd();
  }

  private void OnSaveFileSelected(string fileName, bool forSaving)
  {
    if (forSaving)
      SaveGame("user://" + fileName);
    else
      LoadGame("user://" + fileName);
    _saveGamesPanel.Hide();
  }

  private void OnLoadGameButtonPressed()
  {
    _mainMenuPanel.Hide();
    PopulateSaveFiles();
    _saveGamesLabel.Text = "Load Game";
    _saveGamesPanel.Show();
  }

  private void SaveGame(string filePath)
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
    saveData["levelNodesPerLayer"] = _world.AmountOfNodesPerLayer;

    // Write to file
    string json = Json.Stringify(saveData, "\t");
    using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
    file.StoreString(json);

    GD.Print("Game saved.");
  }

  private void LoadGame(string filePath)
  {
    if (!FileAccess.FileExists(filePath))
    {
      GD.Print("No save file found.");
      return;
    }

    using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
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
        _decksHandler.Decks[deckName][x, y] = GlobalConstants.UnitsData[id];
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
      UnitInfo unitInfo = GlobalConstants.UnitsData[id];
      _decksHandler.AvailableUnits.Add(unitInfo);
      _decksHandler.AmountPerUnit[id] = amount;
    }
    _decksHandler.LoadDecks();

    // Load levels
    var levelsData = saveData["levels"].AsGodotDictionary();
    _world.Levels.Clear();

    foreach (var levelKey in levelsData.Keys)
    {
      string levelId = levelKey.AsString();
      var levelData = levelsData[levelKey].AsGodotDictionary();

      var nextNodes = new List<string>();
      foreach (var nextNode in levelData["nextNodes"].AsGodotArray())
        nextNodes.Add(nextNode.AsString());

      var unitGrid = levelData["units"].AsGodotArray();
      UnitInfo[,] units = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
      foreach (var unitEntry in unitGrid)
      {
        var unitData = unitEntry.AsGodotDictionary();
        int x = unitData["x"].AsInt32();
        int y = unitData["y"].AsInt32();
        string id = unitData["id"].AsString();
        units[x, y] = GlobalConstants.UnitsData[id];
      }

      _world.Levels[levelId] = new LevelInfo(
          id: levelId,
          name: levelData["name"].AsString(),
          completed: levelData["completed"].AsBool(),
          unlocked: levelData["unlocked"].AsBool(),
          layer: levelData["layer"].AsInt32(),
          layerIndex: levelData["layerIndex"].AsInt32(),
          isBoss: levelData["isBoss"].AsBool(),
          nextNodes: nextNodes,
          units: units
      );
    }
    _world.AmountOfNodesPerLayer = saveData["levelNodesPerLayer"].AsGodotArray().Select(x => x.AsInt32()).ToArray();

    _world.LoadLevels();

    GD.Print("Game loaded.");
    _mainMenuPanel.Visible = false;
  }

  private void OnCancelSaveGamesButtonPressed()
  {
    _saveGamesPanel.Hide();
  }

  private void OnQuitGameButtonPressed()
  {
    GetTree().Quit();
  }

  private void OnCloseButtonPressed()
  {
    _mainMenuPanel.Visible = false;
  }
}
