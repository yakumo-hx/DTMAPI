using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("进入地牢", 0)]
[Description("在合适的时机进入指定的地牢")]
[Category("多洛可小镇/地牢")]
public class DialogueTask_EnterDungeon : DialogueTask
{
	[SerializeField]
	private string dungeonName;

	public override string taskTitle => "进入地牢:\"" + dungeonName + "\"";

	public override void DoAction(Graph graph)
	{
		DolocAPI.WaitUntil(() => DolocAPI.IsCurrentStateSupportCutscenes, delegate
		{
			DolocAPI.EnterDungeon(dungeonName);
		});
	}
}
