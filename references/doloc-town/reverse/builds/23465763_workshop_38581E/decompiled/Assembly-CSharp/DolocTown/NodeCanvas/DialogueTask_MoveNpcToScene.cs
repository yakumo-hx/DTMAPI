using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/城镇")]
[Name("移动Npc到传送点", 0)]
[Description("将指定的Npc移动到指定的标记点,如果标记为排除或者场景不存在,则移动到虚空")]
public class DialogueTask_MoveNpcToScene : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public string npcName;

	[SerializeField]
	[RequiredField]
	public string portalName;

	public override string taskTitle => "将<" + npcName + ">放置到传送点" + portalName;

	public override void DoAction(Graph graph)
	{
		Debug.Log(taskTitle);
		DolocAPI.SetNpcToMarkPoint(npcName, portalName);
	}
}
