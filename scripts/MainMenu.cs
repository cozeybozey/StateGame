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
      var unitGrids = new Godot.Collections.Array();
      var terrainGrids = new Godot.Collections.Array();
      var propGrids = new Godot.Collections.Array();
      for (int gauntletIndex = 0; gauntletIndex < level.Value.Units.Count; gauntletIndex++)
      {
        var unitsGrid = new Godot.Collections.Array();
        var terrainGrid = new Godot.Collections.Array();
        var propsGrid = new Godot.Collections.Array();
        for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
        {
          for (int x = 0; x < GlobalConstants.GridSize.X; x++)
          {
            // Save units
            UnitInfo unit = level.Value.Units[gauntletIndex][x, y];
            if (unit != null)
            {
              unitsGrid.Add(new Godot.Collections.Dictionary()
              {
                  { "x", x },
                  { "y", y },
                  { "id", unit.Id },
              });
            }

            // Save terrain
            TerrainInfo terrain = level.Value.Terrains[gauntletIndex][x, y];
            if (terrain != null)
            {
              terrainGrid.Add(new Godot.Collections.Dictionary()
              {
                  { "x", x },
                  { "y", y },
                  { "id", terrain.Id },
              });
            }

            // Save props
            PropInfo prop = level.Value.Props[gauntletIndex][x, y];
            if (prop != null)
            {
              propsGrid.Add(new Godot.Collections.Dictionary()
              {
                  { "x", x },
                  { "y", y },
                  { "id", prop.Id },
              });
            }
          }
        }
        unitGrids.Add(unitsGrid);
        terrainGrids.Add(terrainGrid);
        propGrids.Add(propsGrid);
      }

      var rewardsList = new Godot.Collections.Array();
      foreach (UnitInfo reward in level.Value.Rewards)
        rewardsList.Add(reward.Id);

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
            { "boss", level.Value.Boss },
            { "gauntlet", level.Value.Gauntlet },
            { "rewards", rewardsList },
            { "coinsReward", level.Value.CoinsReward},
            { "nextNodes", nextNodesList },
            { "terrains", terrainGrids },
            { "props", propGrids },
            { "units", unitGrids },
        };
      levelsData[level.Key] = levelData;
    }
    saveData["levels"] = levelsData;
    //saveData["levelNodesPerLayer"] = _world.AmountOfNodesPerLayer; todo fix
    saveData["coins"] = _world.Coins;

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

      List<UnitInfo> rewards = new();
      foreach (var rewardEntry in levelData["rewards"].AsGodotArray())
        rewards.Add(GlobalConstants.UnitsData[rewardEntry.AsString()]);

      var nextNodes = new List<string>();
      foreach (var nextNode in levelData["nextNodes"].AsGodotArray())
        nextNodes.Add(nextNode.AsString());

      var unitGrids = levelData["units"].AsGodotArray();
      List<UnitInfo[,]> units = new();
      foreach (var gridEntry in unitGrids)
      {
        UnitInfo[,] grid = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
        foreach (var unitEntry in gridEntry.AsGodotArray())
        {
          var unitData = unitEntry.AsGodotDictionary();
          int x = unitData["x"].AsInt32();
          int y = unitData["y"].AsInt32();
          string id = unitData["id"].AsString();
          grid[x, y] = GlobalConstants.UnitsData[id];
        }
        units.Add(grid);
      }

      var terrainGrids = levelData["terrains"].AsGodotArray();
      List<TerrainInfo[,]> terrains = new();
      foreach (var gridEntry in terrainGrids)
      {
        TerrainInfo[,] grid = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
        foreach (var terrainEntry in gridEntry.AsGodotArray())
        {
          var terrainData = terrainEntry.AsGodotDictionary();
          int x = terrainData["x"].AsInt32();
          int y = terrainData["y"].AsInt32();
          string id = terrainData["id"].AsString();
          grid[x, y] = GlobalConstants.TerrainsData[id];
        }
        terrains.Add(grid);
      }

      var propGrids = levelData["props"].AsGodotArray();
      List<PropInfo[,]> props = new();
      foreach (var gridEntry in propGrids)
      {
        PropInfo[,] grid = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
        foreach (var propEntry in gridEntry.AsGodotArray())
        {
          var propData = propEntry.AsGodotDictionary();
          int x = propData["x"].AsInt32();
          int y = propData["y"].AsInt32();
          string id = propData["id"].AsString();
          grid[x, y] = GlobalConstants.PropsData[id];
        }
        props.Add(grid);
      }

      _world.Levels[levelId] = new LevelInfo(
          id: levelId,
          name: levelData["name"].AsString(),
          completed: levelData["completed"].AsBool(),
          unlocked: levelData["unlocked"].AsBool(),
          layer: levelData["layer"].AsInt32(),
          layerIndex: levelData["layerIndex"].AsInt32(),
          boss: levelData["boss"].AsBool(),
          gauntlet: levelData["gauntlet"].AsBool(),
          rewards: rewards,
          coinsReward: levelData["coinsReward"].AsInt32(),
          quadrant: 0, // TODO fix
          nextNodes: nextNodes,
          terrains: terrains,
          props: props,
          units: units
      );
    }
    //_world.AmountOfNodesPerLayer = saveData["levelNodesPerLayer"].AsGodotArray().Select(x => x.AsInt32()).ToArray(); todo fix
    _world.UpdateCoins(saveData["coins"].AsInt32());
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
