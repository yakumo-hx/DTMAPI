using System;
using System.Collections.Generic;
using DolocTown.GameData;
using Newtonsoft.Json;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
[Name("通用任务需求", 0)]
[Description("最常用的任务需求，可以描述绝大部分的任务，新增需求时默认为该需求")]
public class MissionRequireTemplateUniversal : MissionRequireTemplate
{
	public class HandleLocal : MissionRequireTemplateHandle
	{
		private MissionRequireTemplateUniversal _universal;

		[JsonProperty]
		public int count;

		public override bool IsComplete
		{
			get
			{
				if (_universal == null)
				{
					return false;
				}
				return count >= _universal.count;
			}
		}

		public override bool IsCompleteBeforeInit => false;

		public HandleLocal(MissionRequireTemplateUniversal universalTemplate)
			: base(universalTemplate)
		{
			_universal = universalTemplate;
			count = 0;
		}

		[JsonConstructor]
		public HandleLocal(int count, string fingerPrint = null)
			: base(fingerPrint)
		{
			this.count = count;
		}

		public override bool SendMessage(GameEventType type, GameEventArgs e, out bool statusChanged)
		{
			statusChanged = false;
			if (_universal == null)
			{
				return false;
			}
			if (_universal.CheckMessage(type, e, out var reason, out var isImportant))
			{
				statusChanged = true;
				if (++count >= _universal.count)
				{
					Log(reason, isImportant);
					Log("需求 \"" + _universal.SummaryInfo + "\" 任务状态变化：已经完成", isImportant);
					return true;
				}
				Log(reason, isImportant);
				Log($"需求 \"{_universal.SummaryInfo}\" 任务状态变化：未完成: {count}/{_universal.count}", isImportant);
				return false;
			}
			Log(reason, isImportant);
			return false;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			if (_universal == null)
			{
				reason = "任务模板加载失败..";
				return false;
			}
			GameEventRecorder recorder = DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(_universal.EventType);
			if (recorder == null)
			{
				reason = "没有找到目标事件的数据记录器";
				return false;
			}
			reason = string.Empty;
			int num = 0;
			switch (_universal.EventType.GetMissionArgsType())
			{
			case MissionArgsType.INT:
				foreach (KeyValuePair<int, int> intCount in ((GameEventRecorderInt)recorder).intCounts)
				{
					if (_universal.CheckMissionArgs(intCount.Key))
					{
						num += intCount.Value;
					}
					if (num >= _universal.count)
					{
						return true;
					}
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				count = num;
				return false;
			case MissionArgsType.STRING:
				foreach (KeyValuePair<string, int> data in ((GameEventRecorderString)recorder).Datas)
				{
					if (_universal.CheckMissionArgs(data.Key))
					{
						num += data.Value;
					}
					if (num >= _universal.count)
					{
						return true;
					}
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				count = num;
				return false;
			case MissionArgsType.BOOL:
			{
				GameEventRecorderBool gameEventRecorderBool = (GameEventRecorderBool)recorder;
				if (_universal.CheckMissionArgs(args: true))
				{
					num += gameEventRecorderBool.GetCount(value: true);
				}
				if (_universal.CheckMissionArgs(args: false))
				{
					num += gameEventRecorderBool.GetCount(value: false);
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				if (num >= _universal.count)
				{
					count = num;
					return true;
				}
				return false;
			}
			case MissionArgsType.NONE:
				reason = $"触发事件次数不足{recorder.totalCount}/{_universal.count}";
				if (recorder.totalCount >= _universal.count)
				{
					return true;
				}
				count = num;
				return false;
			default:
				reason = "未知参数类型";
				return false;
			}
		}

		public override void ClearProgress()
		{
			count = 0;
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (template is MissionRequireTemplateUniversal { isGlobalCount: false } missionRequireTemplateUniversal)
			{
				_universal = missionRequireTemplateUniversal;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			if (_universal.hideRequireStatus)
			{
				return string.Empty;
			}
			if (_universal.EventType == GameEventType.COMPLETE_DIALOGUE)
			{
				return "";
			}
			Color color = ((count >= _universal.count) ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"{count}/{_universal.count}".Colored(color);
		}
	}

	public class HandleGlobal : MissionRequireTemplateHandle
	{
		private MissionRequireTemplateUniversal _universal;

		public override bool IsComplete => _universal.GlobalCount >= _universal.count;

		public override bool IsCompleteBeforeInit
		{
			get
			{
				if (!_universal.shouldListenAtLeastOnce)
				{
					return IsComplete;
				}
				return false;
			}
		}

		public HandleGlobal(MissionRequireTemplateUniversal universal)
			: base(universal)
		{
			_universal = universal;
		}

		[JsonConstructor]
		public HandleGlobal(string fingerPrint = null)
			: base(fingerPrint)
		{
		}

		public override bool SendMessage(GameEventType type, GameEventArgs e, out bool statusChanged)
		{
			statusChanged = false;
			if (_universal == null)
			{
				return false;
			}
			if (_universal.CheckMessage(type, e, out var reason, out var isImportant))
			{
				statusChanged = true;
				if (IsComplete)
				{
					Log(reason, isImportant);
					Log("需求 \"" + _universal.SummaryInfo + "\" 任务状态变化：已经完成", isImportant);
					return true;
				}
				Log(reason, isImportant);
				Log($"需求 \"{_universal.SummaryInfo}\" 任务状态变化：未完成: {_universal.GlobalCount}/{_universal.count}", isImportant);
				return false;
			}
			Log(reason, isImportant);
			return false;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			if (_universal == null)
			{
				reason = "任务模板加载失败..";
				return false;
			}
			GameEventRecorder recorder = DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(_universal.EventType);
			if (recorder == null)
			{
				reason = "没有找到目标事件的数据记录器";
				return false;
			}
			reason = string.Empty;
			int num = 0;
			switch (_universal.EventType.GetMissionArgsType())
			{
			case MissionArgsType.INT:
				foreach (KeyValuePair<int, int> intCount in ((GameEventRecorderInt)recorder).intCounts)
				{
					if (_universal.CheckMissionArgs(intCount.Key))
					{
						num += intCount.Value;
					}
					if (num >= _universal.count)
					{
						return true;
					}
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				return false;
			case MissionArgsType.STRING:
				foreach (KeyValuePair<string, int> data in ((GameEventRecorderString)recorder).Datas)
				{
					if (_universal.CheckMissionArgs(data.Key))
					{
						num += data.Value;
					}
					if (num >= _universal.count)
					{
						return true;
					}
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				return false;
			case MissionArgsType.BOOL:
			{
				GameEventRecorderBool gameEventRecorderBool = (GameEventRecorderBool)recorder;
				if (_universal.CheckMissionArgs(args: true))
				{
					num += gameEventRecorderBool.GetCount(value: true);
				}
				if (_universal.CheckMissionArgs(args: false))
				{
					num += gameEventRecorderBool.GetCount(value: false);
				}
				reason = $"触发事件次数不足{num}/{_universal.count}";
				return num >= _universal.count;
			}
			case MissionArgsType.NONE:
				reason = $"触发事件次数不足{recorder.totalCount}/{_universal.count}";
				return recorder.totalCount > _universal.count;
			default:
				reason = "未知参数类型";
				return false;
			}
		}

		public override void ClearProgress()
		{
		}

		public override bool LoadTemplate(MissionRequireTemplate template)
		{
			if (template is MissionRequireTemplateUniversal { isGlobalCount: not false } missionRequireTemplateUniversal)
			{
				_universal = missionRequireTemplateUniversal;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			if (_universal.hideRequireStatus)
			{
				return string.Empty;
			}
			if (_universal.EventType == GameEventType.COMPLETE_DIALOGUE)
			{
				return "";
			}
			int globalCount = _universal.GlobalCount;
			Color color = ((globalCount >= _universal.count) ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"{globalCount}/{_universal.count}".Colored(color);
		}
	}

	[SerializeField]
	[FingerPrint]
	public string eventTypeStr;

	[SerializeField]
	[FingerPrint]
	public bool hasMissionArgs;

	[SerializeField]
	[FingerPrint]
	public string args = string.Empty;

	[SerializeField]
	[FingerPrint]
	public StringCompareMethod compareMethodOfString;

	[SerializeField]
	[FingerPrint]
	public bool argsBool;

	[SerializeField]
	[FingerPrint]
	public int argsInt;

	[SerializeField]
	[FingerPrint]
	public int count = 1;

	[SerializeField]
	[FingerPrint]
	public bool isGlobalCount;

	[SerializeField]
	[FingerPrint]
	public bool shouldListenAtLeastOnce;

	[SerializeField]
	[FingerPrint]
	public bool hideRequireStatus;

	[SerializeField]
	[FingerPrint]
	public string expendArgs_1 = "default_expend_args";

	[SerializeField]
	[FingerPrint]
	public string expendArgs_2 = "default_expend_args";

	[SerializeField]
	[FingerPrint]
	public CompareMethod compareMethodOfInt;

	public GameEventType EventType => eventTypeStr.ConvertToEnumOrDefault<GameEventType>();

	public override string SummaryInfo => RequireLabel;

	public int GlobalCount
	{
		get
		{
			GameEventType eventType = EventType;
			MissionArgsType missionArgsType = eventType.GetMissionArgsType();
			if (hasMissionArgs)
			{
				switch (missionArgsType)
				{
				case MissionArgsType.NONE:
					break;
				case MissionArgsType.INT:
					return DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(eventType, argsInt, compareMethodOfInt);
				case MissionArgsType.BOOL:
					return DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(eventType, argsBool);
				case MissionArgsType.STRING:
					return DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(eventType, args, compareMethodOfString);
				default:
					return -1;
				}
			}
			return DolocAPI.archiveHandle.farmData.eventRecorderManager.GetTotalCount(eventType);
		}
	}

	public string RequireLabel
	{
		get
		{
			string text = "fffde3";
			string text2 = (hideRequireStatus ? " <color=#969696><size=12><b>(隐藏进度)</b></size></color>" : string.Empty);
			if (EventType == GameEventType.CUSTOM)
			{
				return "完成自定义事件 <color=#df426e><b><size=12>" + args + "</size></b></color>" + text2;
			}
			string text3 = (shouldListenAtLeastOnce ? "<color=#969696><size=12><b>[■]</b></size></color>" : string.Empty);
			string text4 = (isGlobalCount ? $"总数达到<color=#{text}><b><size=12> {count} </size></b></color>次" : $"<color=#{text}><b><size=12> {count} </size></b></color>次");
			text4 += text3;
			string text5 = (hasMissionArgs ? ("(<color=#df426e><b><size=12>" + MissionArgsLabel(EventType.GetMissionArgsType()) + "</size></b></color>)") : "");
			return "监听消息 <color=#" + text + "><b><size=12> " + eventTypeStr + text5 + " </size></b></color>" + text4 + text2;
		}
	}

	public MissionRequireTemplateUniversal(Graph graph)
		: base(graph)
	{
	}

	public bool CheckMessage(GameEventType type, GameEventArgs e, out string reason, out bool isImportant)
	{
		GameEventType eventType = EventType;
		isImportant = true;
		if (type != eventType)
		{
			isImportant = false;
			reason = $"事件不符合条件，期望{eventType}，实际{type}";
			return false;
		}
		if (!hasMissionArgs)
		{
			isImportant = false;
			reason = "事件符合条件且无需参数";
			return true;
		}
		switch (type.GetMissionArgsType())
		{
		case MissionArgsType.NONE:
			isImportant = false;
			reason = "事件符合条件且无需参数";
			return true;
		case MissionArgsType.INT:
			if (!(e is GameEventArgsInt gameEventArgsInt))
			{
				reason = "参数类型不符合条件，要求INT类型参数";
				return false;
			}
			if (!OperationTools.Compare(gameEventArgsInt.value, argsInt, compareMethodOfInt))
			{
				reason = $"参数不符合条件，要求{compareMethodOfInt.CompareLabel()}{argsInt}，实际{gameEventArgsInt.value}";
				return false;
			}
			reason = $"参数符合条件，要求{compareMethodOfInt.CompareLabel()}{argsInt}，实际{gameEventArgsInt.value}";
			return true;
		case MissionArgsType.STRING:
			if (!(e is GameEventArgsString gameEventArgsString))
			{
				reason = "参数类型不符合条件，要求STRING类型参数";
				return false;
			}
			if (!MissionUtils.CompareString(args, gameEventArgsString.value, compareMethodOfString))
			{
				reason = "参数不符合条件，要求参数" + compareMethodOfString.Label() + args + "，实际参数为" + gameEventArgsString.value;
				return false;
			}
			reason = "参数符合条件，要求参数" + compareMethodOfString.Label() + args + "，实际参数为" + gameEventArgsString.value;
			return true;
		case MissionArgsType.BOOL:
			if (!(e is GameEventArgsBool gameEventArgsBool))
			{
				reason = "参数类型不符合条件，要求BOOL类型参数";
				return false;
			}
			if (!gameEventArgsBool.Check(argsBool))
			{
				reason = $"参数不符合条件，要求{argsBool}，实际{gameEventArgsBool.value}";
				return false;
			}
			reason = $"参数符合条件，要求{argsBool}，实际{gameEventArgsBool.value}";
			return true;
		default:
			reason = $"未知参数类型{type.GetMissionArgsType()}";
			return false;
		}
	}

	public bool CheckMissionArgs(int value)
	{
		if (!hasMissionArgs)
		{
			return true;
		}
		if (EventType.GetMissionArgsType() != MissionArgsType.INT)
		{
			return false;
		}
		return OperationTools.Compare(value, argsInt, compareMethodOfInt);
	}

	public bool CheckMissionArgs(string args)
	{
		if (!hasMissionArgs)
		{
			return true;
		}
		if (EventType.GetMissionArgsType() != MissionArgsType.STRING)
		{
			return false;
		}
		return MissionUtils.CompareString(this.args, args, compareMethodOfString);
	}

	public bool CheckMissionArgs(bool args)
	{
		if (!hasMissionArgs)
		{
			return true;
		}
		if (EventType.GetMissionArgsType() != MissionArgsType.BOOL)
		{
			return false;
		}
		return args == argsBool;
	}

	public override MissionRequireTemplateHandle CreateHandle()
	{
		if (!isGlobalCount)
		{
			return new HandleLocal(this);
		}
		return new HandleGlobal(this);
	}

	public override string GetTemplateFingerPrint()
	{
		return this.FingerPrint();
	}

	public override void Reset()
	{
		eventTypeStr = GameEventType.PLANT_CROP.ToString();
		hasMissionArgs = false;
		args = string.Empty;
		compareMethodOfString = StringCompareMethod.EqualTo;
		argsBool = false;
		argsInt = 0;
		count = 1;
		isGlobalCount = false;
		shouldListenAtLeastOnce = false;
		hideRequireStatus = false;
		expendArgs_1 = "default_expend_args";
		expendArgs_2 = "default_expend_args";
		compareMethodOfInt = CompareMethod.EqualTo;
	}

	private string GetMissionArgs(MissionArgsType argsType)
	{
		return argsType switch
		{
			MissionArgsType.INT => argsInt.ToString(), 
			MissionArgsType.BOOL => argsBool.ToString(), 
			MissionArgsType.STRING => args, 
			_ => string.Empty, 
		};
	}

	private string MissionArgsLabel(MissionArgsType argsType)
	{
		string missionArgs = GetMissionArgs(argsType);
		return argsType switch
		{
			MissionArgsType.INT => compareMethodOfInt.CompareLabel() + missionArgs, 
			MissionArgsType.STRING => compareMethodOfString.Label() + missionArgs, 
			_ => missionArgs, 
		};
	}
}
