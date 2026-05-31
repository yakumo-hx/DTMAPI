using System;
using DolocTown.GameData;
using Newtonsoft.Json;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
[Name("累加模式任务", 0)]
[Description("当指定的消息事件（INT类型参数）触发时，累加一个值，当累加值达到目标值时任务完成")]
public class MissionRequireTemplateAccumulation : MissionRequireTemplate
{
	public class Handle : MissionRequireTemplateHandle
	{
		private MissionRequireTemplateAccumulation _template;

		[JsonProperty]
		private int currentValue;

		public override bool IsComplete => currentValue >= _template.targetValue;

		public override bool IsCompleteBeforeInit => false;

		public Handle(MissionRequireTemplateAccumulation template)
			: base(template)
		{
			_template = template;
		}

		[JsonConstructor]
		public Handle(int currentValue, string fingerPrint = null)
			: base(fingerPrint)
		{
			this.currentValue = currentValue;
		}

		public override void ClearProgress()
		{
			currentValue = 0;
		}

		public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
		{
			statusChanged = false;
			GameEventType eventType2 = _template.EventType;
			if (eventType2 != eventType)
			{
				Log($"消息类型不匹配，期望 {eventType2}，实际 {eventType}", isImportant: false);
				return false;
			}
			MissionArgsType missionArgsType = eventType2.GetMissionArgsType();
			if (missionArgsType != MissionArgsType.INT)
			{
				Log($"消息类型{eventType}({missionArgsType})不是INT类型参数，无法累加", isImportant: true);
				return false;
			}
			if (!(args is GameEventArgsInt gameEventArgsInt))
			{
				Log($"消息参数类型不匹配，期望GameEventArgsInt类型, 实际 {args.GetType()}", isImportant: true);
				return false;
			}
			statusChanged = true;
			currentValue += gameEventArgsInt.value;
			if (IsComplete)
			{
				Log("需求 \"" + _template.SummaryInfo + "\" 任务状态变化：已经完成", isImportant: true);
				return true;
			}
			Log($"需求 \"{_template.SummaryInfo}\" 任务状态变化：未完成：{currentValue}/{_template.targetValue}", isImportant: true);
			return false;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			if (_template == null)
			{
				reason = "任务模板加载失败..";
				return false;
			}
			int gameEventTypeIntAccumulation = DolocAPI.archiveHandle.GetGameEventTypeIntAccumulation(_template.EventType);
			reason = $"累加值未达到目标值{gameEventTypeIntAccumulation}/{_template.targetValue}";
			return gameEventTypeIntAccumulation > _template.targetValue;
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (!(template is MissionRequireTemplateAccumulation template2))
			{
				return false;
			}
			_template = template2;
			return true;
		}

		public override string ToString()
		{
			Color color = (IsComplete ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"{currentValue}/{_template.targetValue}".Colored(color);
		}
	}

	[SerializeField]
	[FingerPrint]
	private string eventTypeStr;

	[SerializeField]
	[FingerPrint]
	private int targetValue;

	public override string SummaryInfo => $"消息 <size=12><b>{eventTypeStr}</b></size> 触发的累加值达到 <size=12><b>{targetValue}</b></size>";

	public GameEventType EventType => eventTypeStr.ConvertToEnumOrDefault<GameEventType>();

	public MissionRequireTemplateAccumulation(Graph graph)
		: base(graph)
	{
	}

	public override MissionRequireTemplateHandle CreateHandle()
	{
		return new Handle(this);
	}

	public override string GetTemplateFingerPrint()
	{
		return this.FingerPrint();
	}

	public override void Reset()
	{
		eventTypeStr = GameEventType.MAKE_MONEY.ToString();
		targetValue = 1;
	}
}
