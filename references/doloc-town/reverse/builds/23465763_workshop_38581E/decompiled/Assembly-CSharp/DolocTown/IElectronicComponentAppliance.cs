namespace DolocTown;

public interface IElectronicComponentAppliance : IElectronicComponent
{
	float RatedPowerConsumption { get; }

	bool IsActive { get; set; }

	bool IsFull { get; }

	float PowerGap { get; }

	float PowerProgress { get; }

	float CurrentPowerConsumption
	{
		get
		{
			if (!IsActive)
			{
				return 0f;
			}
			return RatedPowerConsumption;
		}
	}

	void Charge(float power);

	void ChargeToFull();

	bool Launch();
}
