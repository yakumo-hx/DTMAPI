using System.Collections.Generic;
using System.Linq;

namespace DolocTown.Params;

public struct AgentEquipmentParams
{
	public readonly int defense;

	public bool immuneAcidRain;

	public float moveSpeedAdditionPercent;

	public float moveSpeedAdditionValue;

	public float dashCdDecrease;

	public float recoveryAdditionPercentOnNap;

	public int fellCoundAdditionOre;

	public HashSet<string> ShieldSightOfMonsterTypes;

	public float criticalRateChanged;

	public AgentEquipmentParams(int defense)
	{
		this.defense = defense;
		moveSpeedAdditionPercent = 0f;
		moveSpeedAdditionValue = 0f;
		dashCdDecrease = 0f;
		recoveryAdditionPercentOnNap = 0f;
		fellCoundAdditionOre = 0;
		immuneAcidRain = false;
		ShieldSightOfMonsterTypes = new HashSet<string>();
		criticalRateChanged = 0f;
	}

	public AgentEquipmentAbility Commit(IMotionParam baseMotionParam)
	{
		float moveSpeedAddition = GetMoveSpeedAddition(baseMotionParam.MoveSpeed);
		return new AgentEquipmentAbility(defense, moveSpeedAddition, immuneAcidRain, dashCdDecrease, recoveryAdditionPercentOnNap, fellCoundAdditionOre, ShieldSightOfMonsterTypes.ToArray(), criticalRateChanged);
	}

	private float GetMoveSpeedAddition(float originMoveSpeed)
	{
		return moveSpeedAdditionPercent * originMoveSpeed + moveSpeedAdditionValue;
	}
}
