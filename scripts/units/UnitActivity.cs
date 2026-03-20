using Godot;
using System.Collections.Generic;

public partial class UnitActivity : RefCounted
{
	public int DamageDealth { get; set; }
	public int DamageTaken { get; set; }
	public int HealingDone { get; set; }
	public int HealingReceived { get; set; }

	public UnitActivity(
		int damageDealt,
		int damageTaken,
		int healingDone,
		int healingReceived)
	{
		DamageDealth = damageDealt;
		DamageTaken = damageTaken;
		HealingDone = healingDone;
		HealingReceived = healingReceived;
	}
}
