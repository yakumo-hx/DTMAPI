using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("控制游戏流程", 0)]
[Category("多洛可小镇/城镇")]
[Description("启动或终止一个游戏流程")]
public class DialogueTask_GameProcess : DialogueTask
{
	[SerializeField]
	private string processName;

	[SerializeField]
	private bool isStart;

	public override string taskTitle
	{
		get
		{
			if (!isStart)
			{
				return "终止 \"" + processName + "\"";
			}
			return "启动 \"" + processName + "\"";
		}
	}
}
