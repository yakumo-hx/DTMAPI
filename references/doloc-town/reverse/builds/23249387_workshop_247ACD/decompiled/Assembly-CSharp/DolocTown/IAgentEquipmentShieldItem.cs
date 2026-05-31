namespace DolocTown;

public interface IAgentEquipmentShieldItem
{
	float ShieldPercent { get; }

	bool TryBlockAttack(int damage, out int blockedDamage);
}
