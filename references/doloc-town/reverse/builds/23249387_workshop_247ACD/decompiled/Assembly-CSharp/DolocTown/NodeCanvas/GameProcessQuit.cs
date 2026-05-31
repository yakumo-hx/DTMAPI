using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("终止", 0)]
[Description("流程终止时或直接被中断时执行该节点")]
[Color("ee3046")]
public class GameProcessQuit : GameProcessNode
{
	[SerializeField]
	private DialogueTask task;

	public override string name => "终止";

	public void DoQuitActions()
	{
		task?.DoAction(base.graph);
	}
}
