using System.Collections.Generic;
using Newtonsoft.Json;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("多事件任务", 0)]
[Description("指定多个事件，任意一个事件触发时，累加一个值，当累加值达到目标值时任务完成")]
public class MissionRequireTemplateMultiArgs : MissionRequireTemplate
{
	public class Handle : MissionRequireTemplateHandle
	{
		private readonly MissionRequireTemplateMultiArgs template;

		[JsonProperty]
		private int count;

		public override bool IsComplete => count >= template.count;

		public override bool IsCompleteBeforeInit => false;

		public Handle(MissionRequireTemplateMultiArgs template)
			: base(template)
		{
			this.template = template;
		}

		[JsonConstructor]
		public Handle(int count, string fingerPrint = null)
			: base(fingerPrint)
		{
			this.count = count;
		}

		public override void ClearProgress()
		{
			count = 0;
		}

		public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
		{
			statusChanged = false;
			foreach (MissionEvent @event in template.events)
			{
				if (!@event.CheckMessage(eventType, args, out var reason, out var isImportant))
				{
					Log(reason, isImportant);
					continue;
				}
				count++;
				statusChanged = true;
				if (count >= template.count)
				{
					Log(reason, isImportant);
					Log("需求 \"" + template.SummaryInfo + "\" 任务状态变化：已经完成", isImportant);
					return true;
				}
				Log($"需求 \"{template.SummaryInfo}\" 任务状态变化：未完成: {count}/{template.count}", isImportant);
				Log(reason, isImportant);
				return false;
			}
			return false;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			reason = "MissionRequireTemplateMultiArgs: 暂不支持重建历史数据";
			return false;
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (template is MissionRequireTemplateMultiArgs missionRequireTemplateMultiArgs)
			{
				count = missionRequireTemplateMultiArgs.count;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			Color color = (IsComplete ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"{count}/{template.count}".Colored(color);
		}
	}

	[SerializeField]
	private List<MissionEvent> events = new List<MissionEvent>();

	[SerializeField]
	private int count;

	public override string SummaryInfo
	{
		get
		{
			if (events.IsNullOrEmpty())
			{
				return "暂未配置任何事件";
			}
			if (events.Count == 1)
			{
				return events[0].RequireLabel;
			}
			return events[0].RequireLabel + $"等{events.Count}个事件..";
		}
	}

	public MissionRequireTemplateMultiArgs(Graph graph)
		: base(graph)
	{
	}

	public override MissionRequireTemplateHandle CreateHandle()
	{
		return new Handle(this);
	}

	public override string GetTemplateFingerPrint()
	{
		List<byte> list = new List<byte>();
		foreach (MissionEvent @event in events)
		{
			list.AddRange(BinUtils.GetBinDatas(@event));
		}
		list.AddRange(BinUtils.GetBinDatas(count));
		return BinUtils.Encode_Sha256(list.ToArray());
	}

	public override void Reset()
	{
		count = 0;
		events.Clear();
	}
}
