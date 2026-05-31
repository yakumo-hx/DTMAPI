namespace DolocTown;

public interface IDurability
{
	int maxDurability { get; }

	int currentDurability { get; }

	float remainingPercent => (float)currentDurability / (float)maxDurability;
}
