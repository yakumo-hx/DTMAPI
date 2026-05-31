using UnityEngine;

namespace DolocTown;

public class RecorveryAbility
{
	private RecoveryModifier modifier;

	public float HealthScaler => modifier.healthRecoveryModifier.x;

	public float HealthAdder => modifier.healthRecoveryModifier.y;

	public float EnergyScaler => modifier.energyRecoveryModifier.x;

	public float EnergyAdder => modifier.energyRecoveryModifier.y;

	public float SpiritScaler => modifier.spiritRecoveryModifier.x;

	public float SpiritAdder => modifier.spiritRecoveryModifier.y;

	public void Clear()
	{
		modifier = default(RecoveryModifier);
	}

	public void SetHealthScaler(float scaler)
	{
		modifier.healthRecoveryModifier.x = scaler;
	}

	public void SetHealthAdder(float adder)
	{
		modifier.healthRecoveryModifier.y = adder;
	}

	public int GetHealthRecovery(int source)
	{
		if (modifier.healthRecoveryModifier == Vector2.zero)
		{
			return source;
		}
		source = (int)((float)source * (1f + modifier.healthRecoveryModifier.x) + modifier.healthRecoveryModifier.y);
		return Mathf.Max(0, source);
	}

	public int GetHealthDiff(int source)
	{
		return GetHealthRecovery(source) - source;
	}

	public void SetEnergyScaler(float scaler)
	{
		modifier.energyRecoveryModifier.x = scaler;
	}

	public void SetEnergyAdder(float adder)
	{
		modifier.energyRecoveryModifier.y = adder;
	}

	public int GetEnergyRecovery(int source)
	{
		if (modifier.energyRecoveryModifier == Vector2.zero)
		{
			return source;
		}
		source = (int)((float)source * (1f + modifier.energyRecoveryModifier.x) + modifier.energyRecoveryModifier.y);
		return Mathf.Max(0, source);
	}

	public int GetEnergyDiff(int source)
	{
		return GetEnergyRecovery(source) - source;
	}

	public void SetSpiritScaler(float scaler)
	{
		modifier.spiritRecoveryModifier.x = scaler;
	}

	public void SetSpiritAdder(float adder)
	{
		modifier.spiritRecoveryModifier.y = adder;
	}

	public int GetSpiritRecovery(int source)
	{
		if (modifier.spiritRecoveryModifier == Vector2.zero)
		{
			return source;
		}
		source = (int)((float)source * (1f + modifier.spiritRecoveryModifier.x) + modifier.spiritRecoveryModifier.y);
		return Mathf.Max(0, source);
	}

	public int GetSpiritDiff(int source)
	{
		return GetSpiritRecovery(source) - source;
	}
}
