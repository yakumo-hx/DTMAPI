using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/城镇")]
[Name("【弃用】锁定传送门", 0)]
[Description("【弃用】锁定或解锁对传送门的使用,锁定时,与传送门交互无效并弹出锁定理由")]
public class DialogueTask_LockGate : DialogueTask
{
	[SerializeField]
	public bool isLock;

	[SerializeField]
	public string reason;

	public override string taskTitle => operationName + "传送";

	private string operationName
	{
		get
		{
			if (!isLock)
			{
				return "解锁";
			}
			return "锁定";
		}
	}

	public override void DoAction(Graph graph)
	{
	}
}
