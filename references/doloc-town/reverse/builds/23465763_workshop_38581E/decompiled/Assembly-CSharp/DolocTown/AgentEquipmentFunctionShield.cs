using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionShield : AgentEquipmentFunction, IAgentEquipmentShieldItem
{
	private readonly AgentEquipmentFuncProtoShield _func;

	private readonly ItemHatShield _shieldItem;

	public float ShieldPercent => _shieldItem.ShieldPercent;

	public AgentEquipmentFunctionShield(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoShield)skill.Function;
		_shieldItem = (ItemHatShield)item;
	}

	public bool TryBlockAttack(int damage, out int blockedDamage)
	{
		damage -= _func.Defend;
		if (damage <= 0)
		{
			blockedDamage = 0;
			return true;
		}
		bool num = _shieldItem.TryBlockAttack(damage, out blockedDamage);
		DolocAPI.uiSystem.agentStatusBar.UpdateHealth();
		if (!num)
		{
			DolocAPI.RaiseInstantPSEffects(DolocAPI.agent.PositionHeadTop, InstantParticleEffectsType.PAPERBOX);
			DolocAPI.EquipHat(string.Empty, out var _);
		}
		return num;
	}
}
