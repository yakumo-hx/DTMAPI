using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/成就")]
[Name("解锁交互对象", 0)]
[Description("锁定或解锁指定交互对象")]
public class DialogueTask_UnlockInteractableObject : DialogueTask
{
	[SerializeField]
	[RequiredField]
	private string lockObjectId;

	[SerializeField]
	private bool reverse;

	public override string taskTitle => prefix + "交互对象<" + lockObjectId + ">";

	private string prefix
	{
		get
		{
			if (!reverse)
			{
				return "解锁";
			}
			return "锁定";
		}
	}

	public override void DoAction(Graph graph)
	{
		DolocAPI.SetObjectLockState(lockObjectId, reverse);
	}
}
