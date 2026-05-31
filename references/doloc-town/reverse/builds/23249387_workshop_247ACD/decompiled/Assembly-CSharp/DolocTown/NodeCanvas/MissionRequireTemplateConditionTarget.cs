using System.Collections.Generic;
using System.Text;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("条件模板", 0)]
[Description("每触发一次消息则检查一次条件是否达成")]
public class MissionRequireTemplateConditionTarget : MissionRequireTemplate
{
	public class Handle : MissionRequireTemplateHandle
	{
		private MissionRequireTemplateConditionTarget _template;

		public override bool IsComplete => _template.targetCondition?.IsConditionMet ?? true;

		public override bool IsCompleteBeforeInit
		{
			get
			{
				if (_template.checkWhileInit)
				{
					return _template.targetCondition?.IsConditionMet ?? false;
				}
				return false;
			}
		}

		public Handle(MissionRequireTemplateConditionTarget template)
			: base(template)
		{
			_template = template;
		}

		public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
		{
			if (_template.targetCondition == null)
			{
				statusChanged = true;
				return true;
			}
			foreach (MissionEvent @event in _template.events)
			{
				if (!@event.CheckMessage(eventType, args, out var reason, out var isImportant))
				{
					Log(reason, isImportant);
					continue;
				}
				if (_template.targetCondition.IsConditionMet)
				{
					Log("条件已满足: " + _template.targetCondition.taskTitle, isImportant);
					statusChanged = true;
					return true;
				}
				Log("条件未满足: " + _template.targetCondition.taskTitle, isImportant);
			}
			statusChanged = false;
			return false;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			reason = "条件任务不支持沙盒历史回放";
			return false;
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (!(template is MissionRequireTemplateConditionTarget template2))
			{
				return false;
			}
			_template = template2;
			return true;
		}

		public override void ClearProgress()
		{
		}

		public override string ToString()
		{
			if (!IsComplete)
			{
				return "条件未满足";
			}
			return "条件已满足";
		}
	}

	[SerializeField]
	[FingerPrint]
	private DialogueConditionTask targetCondition;

	[SerializeField]
	[FingerPrint]
	private List<MissionEvent> events = new List<MissionEvent>();

	[SerializeField]
	[FingerPrint]
	private bool checkWhileInit;

	public override bool AllowCondition => false;

	private string triggerInfos
	{
		get
		{
			if (events.Count == 0)
			{
				return "<color=red>未设置触发事件</color>";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("触发事件列表:");
			foreach (MissionEvent @event in events)
			{
				stringBuilder.AppendLine("- " + @event.RequireLabel);
			}
			return stringBuilder.ToString().TrimEnd('\n');
		}
	}

	public override string SummaryInfo
	{
		get
		{
			if (targetCondition != null)
			{
				return "条件任务: " + targetCondition.taskTitle + " \n" + triggerInfos;
			}
			return "未指定条件任务";
		}
	}

	public MissionRequireTemplateConditionTarget(Graph graph)
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
}
