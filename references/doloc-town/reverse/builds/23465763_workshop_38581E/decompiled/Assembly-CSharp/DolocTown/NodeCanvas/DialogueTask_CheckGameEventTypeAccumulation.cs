using System;
using DolocTown.GameData;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("检查游戏事件的累加总值", 0)]
[Description("如果一个游戏事件的参数类型为INT类型，则判断这个游戏事件的参数累加值与给定值的比较结果，比如MAKE_MONEY可获取赚取的总钱数")]
public class DialogueTask_CheckGameEventTypeAccumulation : DialogueConditionTask
{
	[SerializeField]
	[ExposeField]
	private string _eventTypeStr;

	[SerializeField]
	[ExposeField]
	private int testValue;

	[SerializeField]
	[ExposeField]
	private CompareMethod _compareMethod;

	public override string taskTitle => $"{_eventTypeStr}累加值 {CompareLabel} {testValue}";

	private string CompareLabel => _compareMethod switch
	{
		CompareMethod.EqualTo => "==", 
		CompareMethod.GreaterThan => ">", 
		CompareMethod.LessThan => "<", 
		CompareMethod.GreaterOrEqualTo => ">=", 
		CompareMethod.LessOrEqualTo => "<=", 
		_ => "??", 
	};

	protected override bool CheckCondition()
	{
		if (!Enum.TryParse<GameEventType>(_eventTypeStr, ignoreCase: true, out var result))
		{
			return false;
		}
		if (result.GetMissionArgsType() != MissionArgsType.INT)
		{
			return false;
		}
		return OperationTools.Compare(DolocAPI.archiveHandle.farmData.eventRecorderManager.GetAccumulation(result), testValue, _compareMethod);
	}
}
