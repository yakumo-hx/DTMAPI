using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/玩家")]
[Name("设置精力值是否可用", 0)]
[Description("当设置精力值不可用时，玩家不会再消耗精力值")]
public class DialogueTask_SetSpiritEnabled : DialogueTask
{
	[SerializeField]
	[ExposeField]
	private bool enabled;

	public override string taskTitle
	{
		get
		{
			if (!enabled)
			{
				return "关闭精力值";
			}
			return "开启精力值";
		}
	}

	public override void DoAction(Graph graph)
	{
		DolocAPI.SetSpiritEnabled(enabled);
	}
}
