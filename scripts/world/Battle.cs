using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// The battle simulation. Runs in fixed ticks, so the same board always plays out the same way at any game speed.
public class Battle
{
  private const int MaxTicksPerFrame = 32;

  // From this turn on, every unit takes damage at the end of each turn, and more every turn
  private const int LastTurnWithoutDamage = 50;

  // Stays true after the battle ended, until Stop
  public bool Playing { get; private set; } = false;
  public bool Ended { get; private set; } = false;
  public bool Running => Playing && !Ended;

  public bool Paused = false;
  public double Speed = 1.0;

  // Units that became ready on the same tick, acting one by one
  public IReadOnlyList<Unit> ActionQueue => _actionQueue;

  public int WorldTicksLeft => GlobalConstants.TicksPerWorldAction - _tick % GlobalConstants.TicksPerWorldAction;

  public event Action<int>? TurnChanged;
  public event Action? WorldActing;

  private readonly Board _board;
  private readonly OverlayLayer _activeUnitLayer;
  private readonly OverlayLayer _targetedCellsLayer;
  private readonly List<Unit> _actionQueue = new();
  private double _tickAccumulator = 0;
  private int _tick = 0;
  private int _turn = 0;
  private int _turnEndDamage = 1;

  public Battle(Board board, OverlayLayer activeUnitLayer, OverlayLayer targetedCellsLayer)
  {
    _board = board;
    _activeUnitLayer = activeUnitLayer;
    _targetedCellsLayer = targetedCellsLayer;
  }

  public void Start()
  {
    foreach (Unit unit in _board.Units.ToList())
    {
      SetPlayData(unit);
      unit.GameStart();
    }

    // Everyone starts with an empty action bar; units filling up on the same tick go leftmost, then frontmost first
    _board.SortUnits();
    foreach (Unit unit in _board.Units)
      unit.ResetActionBar();
    _board.HideTurnOrder();

    _tick = 0;
    _tickAccumulator = 0;
    _actionQueue.Clear();
    Playing = true;
    _board.InBattle = true;
  }

  public void End()
  {
    Ended = true;
  }

  public void Stop()
  {
    Playing = false;
    Ended = false;
    _board.InBattle = false;
    _tick = 0;
    _tickAccumulator = 0;
    _actionQueue.Clear();
    _turnEndDamage = 1;
    _turn = 0;
    TurnChanged?.Invoke(_turn);
  }

  // Run as many ticks as real time (scaled by game speed) allows, up to a limit so a hitch doesn't make the board jump
  public void Advance(double delta)
  {
    if (Paused || !Running)
      return;

    const double tickDuration = 1.0 / GlobalConstants.TicksPerSecond;
    _tickAccumulator += delta * Speed;
    int ticks = 0;
    while (_tickAccumulator >= tickDuration && ticks < MaxTicksPerFrame && Running)
    {
      _tickAccumulator -= tickDuration;
      ticks++;
      Tick();
    }
    if (ticks == MaxTicksPerFrame)
      _tickAccumulator = 0;
  }

  // One simulation tick. Normally every action bar fills and wind-ups count down.
  // When two or more units become ready on the same tick, they go into _actionQueue and the battle
  // freezes: only the front unit of the queue winds up and strikes, one unit after another in turn
  // order, while all other bars, wind-ups and the turn clock wait. The queue tick does not count as
  // simulation time, so every unit keeps exactly its own speed.
  private void Tick()
  {
    if (_actionQueue.Count > 0)
    {
      ProcessActionQueue();
      if (_actionQueue.Count == 0)
        AdvanceClock();
      return;
    }

    // Copy, because units can die or spawn while acting
    List<Unit> units = _board.Units.ToList();
    foreach (Unit unit in units)
    {
      if (!IsActive(unit))
        continue;
      unit.FillActionBar();
      unit.AdvanceTelegraph();
      if (Ended)
        break;
    }

    if (!Ended)
    {
      List<Unit> ready = units.Where(u => IsActive(u) && u.IsReady).ToList();
      if (ready.Count == 1)
        ready[0].TryStartAction();
      else if (ready.Count > 1)
      {
        _actionQueue.AddRange(ready);
        ProcessActionQueue();
      }
    }

    // A running queue finishes this tick first
    if (_actionQueue.Count == 0)
      AdvanceClock();
  }

  // Advance the front unit of the queue by one step: start its action, or count down its wind-up.
  // Units that act without a wind-up (no targets, or stunned) are handled right away so they add no delay.
  private void ProcessActionQueue()
  {
    while (_actionQueue.Count > 0 && !Ended)
    {
      Unit unit = _actionQueue[0];
      if (!IsActive(unit))
      {
        _actionQueue.RemoveAt(0);
        continue;
      }

      bool wasTelegraphing = unit.IsTelegraphing;
      if (wasTelegraphing)
        unit.AdvanceTelegraph();
      else
        unit.TryStartAction();

      // Still winding up: continue with this unit next tick
      if (unit.IsTelegraphing && IsActive(unit))
        break;
      _actionQueue.RemoveAt(0);

      // After a strike, the next unit starts on the next tick, so strikes never land together
      if (wasTelegraphing)
        break;
    }
  }

  private bool IsActive(Unit unit)
  {
    return GodotObject.IsInstanceValid(unit) && _board.Units.Contains(unit) && unit.Health > 0;
  }

  private void AdvanceClock()
  {
    _tick++;
    if (_tick % GlobalConstants.TicksPerTurn == 0 && !Ended)
      EndTurn();
  }

  private void EndTurn()
  {
    _turn += 1;
    TurnChanged?.Invoke(_turn);
    if (_turn > LastTurnWithoutDamage)
      DamageAllUnits();

    // Use shallow copy to prevent spawned units having their turn end called as well
    foreach (Unit unit in _board.Units.ToList())
      unit.TurnEnd(_board.UnitsGrid, _board.TerrainGrid, _board.PropsGrid, _board.Units, _board.RemovedUnits);

    if (_turn % GlobalConstants.TurnsPerBoardUpdate == 0)
      ActWorld();

    _board.SortUnits();
  }

  private void DamageAllUnits()
  {
    int index = 0;
    while (index < _board.Units.Count)
    {
      Unit unit = _board.Units[index];
      unit.ChangeHealth(-_turnEndDamage, null);
      // A unit that died is no longer in the list, so the next unit is at the same index
      if (_board.Units.Contains(unit))
        index += 1;
    }
    _turnEndDamage += 1;
  }

  // The world acts: dead zombies revive, and every prop's turn end function is called
  private void ActWorld()
  {
    WorldActing?.Invoke();
    RespawnZombies();

    // Use shallow copy to prevent spawned props having their turn end called as well
    foreach (Prop prop in _board.Props.ToList())
      prop.TurnEnd(_board.UnitsGrid, _board.TerrainGrid, _board.PropsGrid, _board.Units, _board.RemovedUnits);
  }

  private void RespawnZombies()
  {
    List<Unit> zombies = _board.RemovedUnits.Where(u => u.Id == "zombie").ToList();
    foreach (Unit zombie in zombies)
    {
      Vector2I? cell = GlobalFunctions.GetRandomGridEntitySpawnLocation(_board.UnitsGrid, _board.TerrainGrid, _board.PropsGrid, zombie.OccupiedCells, zombie.Side);
      if (cell == null)
        continue;

      Unit revived = _board.SpawnUnit(zombie.GetStartInfo(), zombie.Side, cell.Value, beforeAdd: SetPlayData);
      revived.SpawnFloatingText("Revived", Colors.Green);
    }

    foreach (Unit zombie in zombies)
    {
      if (GodotObject.IsInstanceValid(zombie))
        zombie.QueueFree();
      _board.RemovedUnits.Remove(zombie);
    }
  }

  private void SetPlayData(Unit unit)
  {
    unit.SetPlayData(_board.UnitsGrid, _board.TerrainGrid, _board.PropsGrid, _board.Units, _board.RemovedUnits, _activeUnitLayer, _targetedCellsLayer);
  }
}
