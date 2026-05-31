namespace DolocTown;

public struct DroneStructureParams
{
	public readonly float PowerCapacity;

	public readonly float PowerRecoveryPerSecond;

	public readonly float MoveSpeed;

	public DroneStructureParams(float powerCapacity, float powerRecoveryPerSecond, float moveSpeed)
	{
		PowerCapacity = powerCapacity;
		PowerRecoveryPerSecond = powerRecoveryPerSecond;
		MoveSpeed = moveSpeed;
	}
}
