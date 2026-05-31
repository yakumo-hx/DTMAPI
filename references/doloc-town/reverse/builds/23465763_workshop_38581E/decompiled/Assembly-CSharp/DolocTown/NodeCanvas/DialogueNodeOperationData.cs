using System;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public class DialogueNodeOperationData
{
	[SerializeField]
	[RequiredField]
	public string operationTypeString;

	[SerializeField]
	[RequiredField]
	public string dialogueNodeName;

	[SerializeField]
	[RequiredField]
	public bool useDefaultTarget;

	[SerializeField]
	[RequiredField]
	public string customTargetName;

	public bool isUnfolded = true;

	public DialogueOperationType operationType => operationTypeString.ConvertToEnumOrDefault<DialogueOperationType>();

	public DialogueNodeOperationData(DialogueOperationType type)
	{
		operationTypeString = type.ToString();
		dialogueNodeName = string.Empty;
		customTargetName = string.Empty;
		useDefaultTarget = true;
	}

	private DialogueNodeOperationData(string operationTypeString, string dialogueNodeName, bool useDefaultTarget, string customTargetName, bool isUnfolded)
	{
		this.operationTypeString = operationTypeString;
		this.dialogueNodeName = dialogueNodeName;
		this.useDefaultTarget = useDefaultTarget;
		this.customTargetName = customTargetName;
		this.isUnfolded = isUnfolded;
	}

	public DialogueNodeOperationData Copy()
	{
		return new DialogueNodeOperationData(operationTypeString, dialogueNodeName, useDefaultTarget, customTargetName, isUnfolded);
	}
}
