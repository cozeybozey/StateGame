using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// A hand-made level. Units stand on the enemy half, on their main cells.
public class DesignedLevel
{
  // Section of levels that are not part of the world by themselves, like bosses, which are placed by their id
  public const int NoSection = -1;

  public string Id = "";
  public string Name = "";
  public int Section = NoSection;
  public int Layer = 1;
  public UnitInfo[,] Units = new UnitInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

  // Cells without an entry have the section's base terrain, or no prop
  public TerrainInfo?[,] Terrain = new TerrainInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];
  public PropInfo?[,] Props = new PropInfo[GlobalConstants.GridSize.X, GlobalConstants.GridSize.Y];

  // Leaves out units that stand on another unit, like the game does. Returns whether there were any.
  public bool RemoveOverlaps()
  {
    bool overlaps = false;
    HashSet<Vector2I> taken = new();
    foreach (Vector2I cell in Board.Cells)
    {
      UnitInfo unit = Units[cell.X, cell.Y];
      if (unit == null)
        continue;

      if (unit.OccupiedCells.Any(c => taken.Contains(cell + c)))
      {
        Units[cell.X, cell.Y] = null!;
        overlaps = true;
      }
      else
        taken.UnionWith(unit.OccupiedCells.Select(c => cell + c));
    }
    return overlaps;
  }
}

// Reads and writes the hand-made levels in levels.json
public class LevelStore
{
  private const string Path = "res://scripts/levels.json";

  // The raw data is kept, so saving one level leaves everything else in the file as it was
  private readonly Dictionary _data;

  public LevelStore()
  {
    _data = (Dictionary)Json.ParseString(FileAccess.Open(Path, FileAccess.ModeFlags.Read).GetAsText());
  }

  public IEnumerable<DesignedLevel> Levels => _data.Keys.Select(id => Get(id.AsString()));

  public bool Contains(string id)
  {
    return _data.ContainsKey(id);
  }

  public DesignedLevel Get(string id)
  {
    Dictionary data = (Dictionary)_data[id];
    DesignedLevel level = new DesignedLevel
    {
      Id = id,
      Name = data.ContainsKey("name") ? data["name"].AsString() : id.Capitalize(),
      Section = data.ContainsKey("section") ? data["section"].AsInt32() : DesignedLevel.NoSection,
      Layer = data.ContainsKey("layer") ? data["layer"].AsInt32() : 1,
    };
    foreach (Dictionary unit in data["units"].AsGodotArray())
    {
      if (GlobalConstants.UnitsData.TryGetValue(unit["name"].AsString(), out UnitInfo unitInfo))
        level.Units[unit["x"].AsInt32(), unit["y"].AsInt32()] = unitInfo;
    }
    ReadCells(data, "terrain", GlobalConstants.TerrainsData, level.Terrain);
    ReadCells(data, "props", GlobalConstants.PropsData, level.Props);
    return level;
  }

  public bool Save(DesignedLevel level)
  {
    Array units = new();
    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        if (level.Units[x, y] != null)
          units.Add(new Dictionary { { "x", x }, { "y", y }, { "name", level.Units[x, y].Id } });
      }
    }

    Dictionary data = Contains(level.Id) ? (Dictionary)_data[level.Id] : new Dictionary();
    data["name"] = level.Name;
    data["units"] = units;
    WriteCells(data, "terrain", level.Terrain, t => t.Id);
    WriteCells(data, "props", level.Props, p => p.Id);
    if (level.Section == DesignedLevel.NoSection)
    {
      data.Remove("section");
      data.Remove("layer");
    }
    else
    {
      data["section"] = level.Section;
      data["layer"] = level.Layer;
    }
    _data[level.Id] = data;
    return Write();
  }

  // Terrain and props are stored as a list of [x, y] cells per kind
  private static void ReadCells<[MustBeVariant] T>(Dictionary data, string field, Godot.Collections.Dictionary<string, T> kinds, T?[,] grid) where T : class
  {
    if (!data.ContainsKey(field))
      return;
    foreach (KeyValuePair<Variant, Variant> kind in (Dictionary)data[field])
    {
      if (!kinds.TryGetValue(kind.Key.AsString(), out T info))
        continue;
      foreach (Array cell in kind.Value.AsGodotArray())
        grid[cell[0].AsInt32(), cell[1].AsInt32()] = info;
    }
  }

  private static void WriteCells<T>(Dictionary data, string field, T?[,] grid, System.Func<T, string> getId) where T : class
  {
    Dictionary kinds = new();
    foreach (Vector2I cell in Board.Cells)
    {
      T? info = grid[cell.X, cell.Y];
      if (info == null)
        continue;
      string id = getId(info);
      if (!kinds.ContainsKey(id))
        kinds[id] = new Array();
      kinds[id].AsGodotArray().Add(new Array { cell.X, cell.Y });
    }

    if (kinds.Count > 0)
      data[field] = kinds;
    else
      data.Remove(field);
  }

  public bool Delete(string id)
  {
    _data.Remove(id);
    return Write();
  }

  // One unit per line, so changes to a level stay readable in version control
  private bool Write()
  {
    StringBuilder json = new("{\n");
    json.AppendJoin(",\n", _data.Select(level =>
    {
      IEnumerable<string> fields = ((Dictionary)level.Value).Select(field =>
      {
        string value = field.Key.AsString() == "units"
          ? "[\n" + string.Join(",\n", field.Value.AsGodotArray().Select(unit => "\t\t\t" + Format(unit))) + "\n\t\t]"
          : Format(field.Value);
        return $"\t\t{Json.Stringify(field.Key)}: {value}";
      });
      return $"\t{Json.Stringify(level.Key)}: {{\n{string.Join(",\n", fields)}\n\t}}";
    }));
    json.Append("\n}\n");

    using FileAccess file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
    if (file == null)
      return false;
    file.StoreString(json.ToString());
    return true;
  }

  // Numbers read from JSON are floats, which would be written as 1.0
  private static string Format(Variant value)
  {
    if (value.VariantType == Variant.Type.Array)
      return "[" + string.Join(", ", value.AsGodotArray().Select(Format)) + "]";
    if (value.VariantType == Variant.Type.Dictionary)
      return "{ " + string.Join(", ", ((Dictionary)value).Select(field => $"{Json.Stringify(field.Key)}: {Format(field.Value)}")) + " }";
    return value.VariantType == Variant.Type.Float && value.AsDouble() == System.Math.Floor(value.AsDouble())
      ? value.AsInt64().ToString()
      : Json.Stringify(value);
  }
}
