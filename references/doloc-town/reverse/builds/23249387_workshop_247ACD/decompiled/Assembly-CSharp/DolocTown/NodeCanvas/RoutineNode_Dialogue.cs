using System.Collections.Generic;
using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("对话节点", 0)]
[Description("设置对话入口/添加对话节点/移除对话节点")]
[Color("8c7ca0")]
[ParadoxNotion.Design.Icon("Dialogue", false, "")]
public class RoutineNode_Dialogue : RoutineNode, IDialogueNode
{
	[SerializeField]
	[ExposeField]
	private List<DialogueNodeOperationData> dialogueOperations = new List<DialogueNodeOperationData>();

	public override int maxOutConnections => 0;

	public override int maxInConnections => int.MaxValue;

	public override string name => "对话节点";

	public void Execute()
	{
		foreach (DialogueNodeOperationData dialogueOperation in dialogueOperations)
		{
			string text = ((dialogueOperation.operationType != DialogueOperationType.START && string.IsNullOrEmpty(DolocAPI.GetDefaultNpcNameForNode(dialogueOperation.dialogueNodeName))) ? dialogueOperation.customTargetName : (dialogueOperation.useDefaultTarget ? DolocAPI.GetDefaultNpcNameForNode(dialogueOperation.dialogueNodeName) : dialogueOperation.customTargetName));
			int num = dialogueOperation.operationType switch
			{
				DialogueOperationType.ADD => DolocAPI.AddDialogueNode(dialogueOperation.dialogueNodeName, text) ? 1 : 0, 
				DialogueOperationType.ENTRANCE => DolocAPI.SetDialogueEntrance(dialogueOperation.dialogueNodeName, text) ? 1 : 0, 
				DialogueOperationType.REMOVE => DolocAPI.RemoveDialogueNode(dialogueOperation.dialogueNodeName, text) ? 1 : 0, 
				DialogueOperationType.START => DolocAPI.StartDialogueNode(dialogueOperation.dialogueNodeName, text) ? 1 : 0, 
				_ => 0, 
			};
			string text2 = dialogueOperation.operationTypeString + ": 对话节点<" + dialogueOperation.dialogueNodeName + ">";
			if (!string.IsNullOrEmpty(text))
			{
				text2 = text2 + " 对话目标<" + text + ">";
			}
			if (num != 0)
			{
				Debug.Log(text2 + "成功");
			}
			else
			{
				Debug.LogError(text2 + "失败!");
			}
		}
	}
}
