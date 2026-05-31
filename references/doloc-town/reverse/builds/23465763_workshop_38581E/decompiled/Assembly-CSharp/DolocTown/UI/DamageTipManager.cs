using UnityEngine;

namespace DolocTown.UI;

public class DamageTipManager
{
	public void Raise(int count, Vector2 ws, bool isHeavy, float duration, float popDistance, float waitTime = 1.5f)
	{
		DamageTip fromPoolInScene = DolocAPI.uiSystem.GetFromPoolInScene<DamageTip>();
		fromPoolInScene.recycleHandle = delegate(DamageTip x)
		{
			DolocAPI.uiSystem.RecycleToPoolInScene(x);
		};
		if (!(fromPoolInScene == null))
		{
			if (isHeavy)
			{
				fromPoolInScene.RaiseHeavy(count, ws, duration, popDistance, waitTime);
			}
			else
			{
				fromPoolInScene.Raise(count, ws, duration, popDistance, waitTime);
			}
		}
	}
}
