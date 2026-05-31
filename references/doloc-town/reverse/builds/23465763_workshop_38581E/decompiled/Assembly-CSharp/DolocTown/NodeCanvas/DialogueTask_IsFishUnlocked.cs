using UnityEngine;

namespace DolocTown.NodeCanvas;

public class DialogueTask_IsFishUnlocked : DialogueConditionTask
{
	[SerializeField]
	private string fishName;

	public override string taskTitle => "\"" + fishName + "\"已解锁";

	protected override bool CheckCondition()
	{
		return DolocAPI.CheckFishUnlocked(fishName);
	}
}
