using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class MissionRequire_Graph : MissionRequire
{
	[JsonProperty]
	public readonly string chainId;

	[JsonProperty]
	public readonly string missionId;

	[JsonProperty]
	public readonly string uid;

	[JsonProperty]
	public readonly int index;

	public readonly MissionRequireTemplate Template;

	public readonly DialogueConditionTask Condition;

	public readonly CustomMissionProgressFunc CustomProgress;

	public bool IsConditionMet => Condition?.IsConditionMet ?? true;

	public MissionRequire_Graph(string chainId, string missionId, string uid, int index, MissionRequireTemplate template, DialogueConditionTask condition, CustomMissionProgressFunc customProgress)
	{
		this.chainId = chainId;
		this.missionId = missionId;
		this.uid = uid;
		this.index = index;
		Template = template;
		Condition = condition;
		CustomProgress = customProgress;
	}

	[JsonConstructor]
	public MissionRequire_Graph(string chainId, string missionId, string uid, int index)
	{
		this.chainId = chainId;
		this.missionId = missionId;
		this.uid = uid;
		this.index = index;
		TryLoadTemplateAndCondition(out Template, out Condition, out CustomProgress);
	}

	private bool TryLoadTemplateAndCondition(out MissionRequireTemplate template, out DialogueConditionTask condition, out CustomMissionProgressFunc customProgress)
	{
		template = null;
		condition = null;
		customProgress = null;
		if (!DolocAPI.assets.missionChains.QueryData(chainId, out var data))
		{
			Debug.LogError("任务链模板\"" + chainId + "\"加载失败");
			return false;
		}
		if (!data.QueryMissionNode(missionId, out var node))
		{
			Debug.LogError("找不到任务节点\"" + missionId + "\"");
			return false;
		}
		if (!(node is MissionNodeListener missionNodeListener))
		{
			Debug.LogError("任务节点\"" + missionId + "\"不是MissionNodeListener");
			return false;
		}
		if (missionNodeListener.QueryRequire(index, out template, out condition, out customProgress))
		{
			return true;
		}
		Debug.LogError($"任务链模板: {chainId}.{missionId}.{index} 加载失败");
		return false;
	}

	public override MissionRequireHandle CreateHandle()
	{
		return new MissionRequireHandle_Graph(this);
	}
}
