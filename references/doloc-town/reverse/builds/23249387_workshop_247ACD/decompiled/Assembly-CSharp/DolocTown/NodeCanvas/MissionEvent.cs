using System;
using DolocTown.GameData;
using ParadoxNotion;
using RedSaw.Data;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public class MissionEvent
{
	[SerializeField]
	[FingerPrint]
	public string eventTypeStr = GameEventType.PLANT_CROP.ToString();

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
	public string expendArgs_1 = "0";

	[SerializeField]
	[FingerPrint]
	public string expendArgs_2 = "1";

	[SerializeField]
	[FingerPrint]
	public bool argsBool;

	[SerializeField]
	[FingerPrint]
	public CompareMethod compareMethodOfInt;

	[SerializeField]
	[FingerPrint]
	public int argsInt;

	public GameEventType EventType => eventTypeStr.ConvertToEnumOrDefault<GameEventType>();

	public string RequireLabel
	{
		get
		{
			if (EventType == GameEventType.CUSTOM)
			{
				return "完成自定义事件 <color=#df426e><b><size=12>" + args + "</size></b></color>";
			}
			string text = (hasMissionArgs ? ("(<color=#df426e><b><size=12>" + MissionArgsLabel(EventType.GetMissionArgsType()) + "</size></b></color>)") : "");
			return "监听消息 <color=#fffde3><b><size=12> " + eventTypeStr + text + " </size></b></color>";
		}
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
}
