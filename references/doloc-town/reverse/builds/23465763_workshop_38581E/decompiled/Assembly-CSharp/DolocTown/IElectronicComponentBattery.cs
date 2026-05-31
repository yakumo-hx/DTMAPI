namespace DolocTown;

public interface IElectronicComponentBattery : IElectronicComponent
{
	float Power { get; }

	float Capacity { get; }

	float PowerGap => Capacity - Power;

	bool IsNotFull => Capacity - Power > 0f;

	void Charge(float power);

	void ClearPower();

	float Discharge();

	void ChargeToFull()
	{
		Charge(Capacity);
	}
}
