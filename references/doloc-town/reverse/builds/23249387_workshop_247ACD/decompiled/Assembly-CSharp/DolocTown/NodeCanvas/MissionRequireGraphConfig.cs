using System;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public class MissionRequireGraphConfig
{
	[SerializeField]
	public MissionRequireTemplate RequireTemplate;

	[SerializeField]
	public bool hasAdditionalCondition;

	[SerializeField]
	public DialogueConditionTask additionalCondition;

	[SerializeField]
	public CustomMissionProgressFunc customMissionProgressFunc;

	public bool IsMatch<T>(bool isGlobalCount, bool hasCondition) where T : MissionRequireTemplate
	{
		if (!(RequireTemplate is T))
		{
			return false;
		}
		if (RequireTemplate is MissionRequireTemplateUniversal missionRequireTemplateUniversal && missionRequireTemplateUniversal.isGlobalCount != isGlobalCount)
		{
			return false;
		}
		if (hasAdditionalCondition != hasCondition)
		{
			return false;
		}
		return true;
	}
}
