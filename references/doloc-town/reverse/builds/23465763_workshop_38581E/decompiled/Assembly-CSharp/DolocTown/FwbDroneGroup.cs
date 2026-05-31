using UnityEngine;

namespace DolocTown;

public class FwbDroneGroup : MonsterGroup
{
	public override bool TryAddMonster(MonsterController controller)
	{
		if (!(controller.MonsterAI is MonsterAI_FwbDrone entity))
		{
			return false;
		}
		Add(entity);
		return true;
	}

	public void BroadcastHurt(Transform transform)
	{
		Foreach(delegate(MonsterAI monster)
		{
			((MonsterAI_FwbDrone)monster).OnBroadcastHurt(transform);
		});
	}
}
