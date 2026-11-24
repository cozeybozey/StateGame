using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Reads the unit, terrain and prop definitions from their JSON files into GlobalConstants
public static class GameData
{
  public static void Load()
  {
    LoadUnits();
    LoadTerrains();
    LoadProps();
  }

  private static void LoadUnits()
  {
    foreach (KeyValuePair<Variant, Variant> entry in ReadJson("res://scripts/units/units.json"))
    {
      string unitId = entry.Key.ToString();
      Dictionary unitData = (Dictionary)entry.Value;

      string displayName = (string)unitData["display_name"];
      string texturePath = (string)unitData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)unitData["scene"];
      int cost = (int)unitData["cost"];
      int health = (int)unitData["health"];
      int damage = (int)unitData["damage"];
      int armor = (int)unitData["armor"];
      int speed = (int)unitData["speed"];
      string description = (string)unitData["description"];
      string rarity = (string)unitData["rarity"];
      List<string> types = unitData["types"].AsGodotArray().Select(t => t.AsString()).ToList();
      int stage = (int)unitData["stage"];

      List<Vector2I> occupiedCells = ParseCells(unitData);

      GlobalConstants.UnitsData[unitId] = new UnitInfo(unitId, displayName, texture, scenePath, occupiedCells, 
        cost, health, health, damage, armor, speed, description, rarity, types, stage);

      // Link unit info score placement function to unit specific class function using reflection
      string fileName = scenePath.GetFile().GetBaseName();
      string className = ToPascalCase(fileName);
      Type type = Type.GetType(className);
      if (type == null) continue;
      var method = type.GetMethod("ScorePlacement", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
      if (method == null) continue;
      GlobalConstants.UnitsData[unitId].ScorePlacement = (pos, unitInfo, unitsGrid, terrainGrid, propsGrid) => (int)method.Invoke(null, new object[] { pos, unitInfo, unitsGrid, terrainGrid, propsGrid });
    }
  }

  private static string ToPascalCase(string snakeCase)
  {
    return string.Concat(snakeCase.Split('_')
        .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
  }

  private static void LoadTerrains()
  {
    foreach (KeyValuePair<Variant, Variant> entry in ReadJson("res://scripts/terrains/terrains.json"))
    {
      string terrainId = entry.Key.ToString();
      Dictionary terrainData = (Dictionary)entry.Value;

      string displayName = (string)terrainData["display_name"];
      string texturePath = (string)terrainData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)terrainData["scene"];
      string description = (string)terrainData["description"];
      string rarity = (string)terrainData["rarity"];
      bool blocking = (bool)terrainData["blocking"];
      List<string> types = terrainData["types"].AsGodotArray().Select(t => t.AsString()).ToList();

      List<Vector2I> occupiedCells = ParseCells(terrainData);

      GlobalConstants.TerrainsData[terrainId] = new TerrainInfo(terrainId, displayName, texture, scenePath, 
        occupiedCells, description, rarity, blocking, types);
    }
  }

  private static void LoadProps()
  {
    foreach (KeyValuePair<Variant, Variant> entry in ReadJson("res://scripts/props/props.json"))
    {
      string propsId = entry.Key.ToString();
      Dictionary propData = (Dictionary)entry.Value;

      string displayName = (string)propData["display_name"];
      string texturePath = (string)propData["texture"];
      Texture2D texture = GD.Load<Texture2D>(texturePath);
      string scenePath = (string)propData["scene"];
      int health = (int)propData["health"];
      int damage = (int)propData["damage"];
      int armor = (int)propData["armor"];
      string description = (string)propData["description"];
      string rarity = (string)propData["rarity"];
      bool damagable = (bool)propData["damagable"];
      bool movable = (bool)propData["movable"];
      bool blocking = (bool)propData["blocking"];
      List<string> types = propData["types"].AsGodotArray().Select(t => t.AsString()).ToList();

      List<Vector2I> occupiedCells = ParseCells(propData);

      GlobalConstants.PropsData[propsId] = new PropInfo(propsId, displayName, texture, scenePath,
        occupiedCells, health, health, damage, armor, description, rarity, damagable, movable, blocking, types);
    }
  }

  private static List<Vector2I> ParseCells(Dictionary data)
  {
    List<Vector2I> cells = new();
    foreach (Godot.Collections.Array cell in (Godot.Collections.Array)data["cells"])
      cells.Add(new Vector2I((int)cell[0], (int)cell[1]));
    return cells;
  }

  private static Dictionary ReadJson(string path)
  {
    return (Dictionary)Json.ParseString(FileAccess.Open(path, FileAccess.ModeFlags.Read).GetAsText());
  }
}
