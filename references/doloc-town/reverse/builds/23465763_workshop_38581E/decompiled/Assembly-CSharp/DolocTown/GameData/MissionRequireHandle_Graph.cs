using System.Collections.Generic;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class MissionRequireHandle_Graph : MissionRequireHandle
{
	public readonly MissionRequire_Graph requireGraph;

	[JsonProperty]
	public readonly MissionRequireTemplateHandle handle;

	public override bool IsInvalid => requireGraph.Template == null;

	public override bool IsComplete => handle.IsComplete;

	public override bool IsCompleteLoadArchive
	{
		get
		{
			if (requireGraph.Condition == null || !(requireGraph.Template is MissionRequireTemplateUniversal { isGlobalCount: not false }))
			{
				return handle.IsComplete;
			}
			if (handle.IsComplete)
			{
				return requireGraph.IsConditionMet;
			}
			return false;
		}
	}

	public override bool IsCompleteBeforeInit
	{
		get
		{
			if (requireGraph.IsConditionMet)
			{
				return handle.IsCompleteBeforeInit;
			}
			return false;
		}
	}

	public override IEnumerable<MissionLog> MissionLogs => handle.MissionLogs;

	public MissionRequireHandle_Graph(MissionRequire_Graph require)
		: base(require)
	{
		requireGraph = require;
		handle = requireGraph.Template.CreateHandle();
	}

	[JsonConstructor]
	public MissionRequireHandle_Graph(MissionRequire_Graph require, MissionRequireTemplateHandle handle)
		: base(require)
	{
		requireGraph = require;
		this.handle = handle;
		if (requireGraph.Template == null)
		{
			Debug.LogError("MissionRequireHandle_Graph: 任务节点加载失败:" + require.missionId);
		}
		else if (!handle.LoadTemplate(requireGraph.Template))
		{
			this.handle = requireGraph.Template.CreateHandle();
		}
	}

	public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
	{
		if (requireGraph.Condition == null)
		{
			return handle.SendMessage(eventType, args, out statusChanged);
		}
		if (requireGraph.Condition.IsConditionMet)
		{
			handle.Log("条件 " + requireGraph.Condition.taskTitle + " 当前满足", isImportant: false);
			return handle.SendMessage(eventType, args, out statusChanged);
		}
		handle.Log("条件 " + requireGraph.Condition.taskTitle + " 当前不满足", isImportant: false);
		statusChanged = false;
		return false;
	}

	public override bool TryInvokeHistoryInSandBox(out string reason)
	{
		if (requireGraph.Condition != null)
		{
			reason = "不支持对带有条件的任务进行历史重建";
			return false;
		}
		return handle.TryInvokeHistoryInSandBox(out reason);
	}

	public override void ClearProgress()
	{
		handle.ClearProgress();
	}

	public override string ToString()
	{
		if (requireGraph.CustomProgress == null)
		{
			return handle.ToString();
		}
		return requireGraph.CustomProgress.GetProgressInfo();
	}
}
