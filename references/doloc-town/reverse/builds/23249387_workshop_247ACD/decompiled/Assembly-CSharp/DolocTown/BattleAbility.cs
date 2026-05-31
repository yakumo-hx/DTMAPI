using UnityEngine;

namespace DolocTown;

public class BattleAbility
{
	private BattleModifier modifier = new BattleModifier();

	public float DefendScaler => modifier.defendModifier.x;

	public float DefendAdder => modifier.defendModifier.y;

	public void Clear()
	{
		modifier = new BattleModifier();
	}

	public void SetDefendScaler(float scaler)
	{
		modifier.defendModifier.x = scaler;
	}

	public void SetDefendAdder(float adder)
	{
		modifier.defendModifier.y = adder;
	}

	public int GetDefend(int source)
	{
		if (modifier.defendModifier == Vector2.zero)
		{
			return source;
		}
		source = (int)((float)source * (1f + modifier.defendModifier.x) + modifier.defendModifier.y);
		return Mathf.Max(0, source);
	}
}
