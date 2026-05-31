namespace DolocTown.Params;

public readonly struct AgentEquipmentAbility
{
	public readonly int defence;

	public readonly float moveSpeedAddition;

	public readonly bool immuneAcidRain;

	public readonly float dashCdDecrease;

	public readonly float recoveryAdditionPercent;

	public readonly int fellCoundAdditionOre;

	public readonly string[] ShieldSightOfMonsterNames;

	public readonly float CriticalRateChanged;

	public AgentEquipmentAbility(int defence, float moveSpeedAddition, bool immuneAcidRain, float dashCdDecrease, float recoveryAdditionPercent, int fellCoundAdditionOre, string[] shieldSightOfMonsterNames, float criticalRateChanged)
	{
		this.defence = defence;
		this.moveSpeedAddition = moveSpeedAddition;
		this.immuneAcidRain = immuneAcidRain;
		this.dashCdDecrease = dashCdDecrease;
		this.recoveryAdditionPercent = recoveryAdditionPercent;
		this.fellCoundAdditionOre = fellCoundAdditionOre;
		ShieldSightOfMonsterNames = shieldSightOfMonsterNames;
		CriticalRateChanged = criticalRateChanged;
	}
}
