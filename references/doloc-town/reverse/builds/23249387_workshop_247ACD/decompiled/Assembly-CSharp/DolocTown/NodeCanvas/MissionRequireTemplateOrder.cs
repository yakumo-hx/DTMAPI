using System;
using System.Collections.Generic;
using DolocTown.GameData;
using Newtonsoft.Json;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
[Name("顺序监听", 0)]
[Description("按照一定的顺序依次监听多个消息， 当所有消息都被触发时任务完成，中断时从头开始")]
public class MissionRequireTemplateOrder : MissionRequireTemplate
{
	public class Handle : MissionRequireTemplateHandle
	{
		private MissionRequireTemplateOrder _template;

		[JsonProperty]
		private int currentIndex;

		public override bool IsComplete => currentIndex >= _template.events.Count;

		public override bool IsCompleteBeforeInit => false;

		public Handle(MissionRequireTemplateOrder template)
			: base(template)
		{
			_template = template;
		}

		[JsonConstructor]
		protected Handle(int currentIndex, string fingerPrint = null)
			: base(fingerPrint)
		{
			this.currentIndex = currentIndex;
		}

		public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
		{
			statusChanged = false;
			if (IsComplete)
			{
				Log("已完成，不再接受消息", isImportant: false);
				statusChanged = true;
				return true;
			}
			EventSimple eventSimple = _template.events[currentIndex];
			if (eventSimple.EventType != eventType)
			{
				Log($"消息类型不匹配 期待 {eventSimple.eventType} 实际 {eventType}", isImportant: false);
				return false;
			}
			if (!eventSimple.CheckMessage(eventType, args, out var reason, out var isImportant))
			{
				Log(reason, isImportant);
				if (!_template.clearWhenInterrupt)
				{
					return false;
				}
				currentIndex = 0;
				Log("中断，清空进度", isImportant: true);
				return false;
			}
			statusChanged = true;
			currentIndex++;
			Log(reason, isImportant);
			Log($"需求 \"{_template.SummaryInfo}\" 任务状态变化 {currentIndex}/{_template.events.Count}", isImportant: true);
			if (!IsComplete)
			{
				return false;
			}
			Log("需求完成", isImportant: true);
			return true;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			reason = "MissionRequireTemplateOrder: 暂不支持重建历史数据";
			return false;
		}

		public override void ClearProgress()
		{
			currentIndex = 0;
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (!(template is MissionRequireTemplateOrder missionRequireTemplateOrder))
			{
				return false;
			}
			if (missionRequireTemplateOrder.events.Count <= currentIndex)
			{
				return false;
			}
			_template = missionRequireTemplateOrder;
			return true;
		}

		public override string ToString()
		{
			Color color = (IsComplete ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"[{currentIndex}/{_template.events.Count}]".Colored(color);
		}
	}

	[Serializable]
	public class EventSimple
	{
		[SerializeField]
		[FingerPrint]
		public string eventType;

		[SerializeField]
		[FingerPrint]
		public bool hasArgs;

		[SerializeField]
		[FingerPrint]
		public string args;

		public GameEventType EventType => eventType.ConvertToEnumOrDefault<GameEventType>();

		public bool CheckMessage(GameEventType type, GameEventArgs e, out string reason, out bool isImportant)
		{
			if (!hasArgs)
			{
				isImportant = false;
				reason = "无参数";
				return true;
			}
			MissionArgsType missionArgsType = type.GetMissionArgsType();
			if (missionArgsType != MissionArgsType.STRING)
			{
				isImportant = true;
				reason = $"消息类型{type}({missionArgsType})不是STRING类型参数，无法比较";
				return false;
			}
			if (!(e is GameEventArgsString gameEventArgsString))
			{
				isImportant = true;
				reason = $"消息参数类型不匹配，期望GameEventArgsString类型, 实际 {e.GetType()}";
				return false;
			}
			isImportant = true;
			if (args == gameEventArgsString.value)
			{
				reason = "需求参数匹配";
				return true;
			}
			reason = "需求参数不匹配，期望 " + args + "，实际 " + gameEventArgsString.value;
			return false;
		}
	}

	[SerializeField]
	private List<EventSimple> events = new List<EventSimple>();

	[SerializeField]
	private bool clearWhenInterrupt;

	public override string SummaryInfo => (clearWhenInterrupt ? "强制" : "") + "按顺序依次监听 " + ((events.Count == 0) ? ".." : (events[0].eventType + "等消息.."));

	public MissionRequireTemplateOrder(Graph graph)
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
		foreach (EventSimple @event in events)
		{
			list.AddRange(BinUtils.GetBinDatas(@event));
		}
		list.AddRange(BitConverter.GetBytes(clearWhenInterrupt));
		return BinUtils.Encode_Sha256(list.ToArray());
	}

	public override void Reset()
	{
		clearWhenInterrupt = false;
		events.Clear();
	}
}
