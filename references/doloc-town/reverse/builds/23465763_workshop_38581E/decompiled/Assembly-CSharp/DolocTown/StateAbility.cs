namespace DolocTown;

public class StateAbility
{
	private StateModifier modifier;

	public bool ImmuneAcidRain => modifier.immuneAcidRainCounter > 0;

	public void ComposeImmuneAcidRainCounter(int counter)
	{
		modifier.immuneAcidRainCounter += counter;
	}

	public void Clear()
	{
		modifier = default(StateModifier);
	}
}
