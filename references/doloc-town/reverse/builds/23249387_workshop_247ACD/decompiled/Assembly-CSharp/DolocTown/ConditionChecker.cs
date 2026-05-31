using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct ConditionChecker
{
	[SerializeField]
	private ConditionType conditionType;

	[SerializeField]
	private MissionCondition missionCondition;

	[SerializeField]
	private EventDecoratorCondition eventDecoratorCondition;

	[SerializeField]
	private FarmLevelCondition farmLevelCondition;

	[SerializeField]
	private FactionMissionCondition factionMissionCondition;

	[SerializeField]
	private LikabilityCondition likabilityCondition;

	[SerializeField]
	private MonthCondition monthCondition;

	[SerializeField]
	private UnlockCondition unlockCondition;

	[SerializeField]
	private RoomVisitCondition roomVisitCondition;

	[SerializeField]
	private PortalVisitCondition portalVisitCondition;

	[SerializeField]
	[Tooltip("正常情况检测条件完成，反转后则检测未完成")]
	private bool reverseCondition;

	[SerializeField]
	private TextTipConfig conditionFailedText;

	public bool IsConditionMet(out TextTipConfig conditionFailedTextTip)
	{
		conditionFailedTextTip = default(TextTipConfig);
		if (!DolocAPI.IsDataLoaded)
		{
			return false;
		}
		if ((conditionType switch
		{
			ConditionType.Mission => missionCondition, 
			ConditionType.EventDecorator => eventDecoratorCondition, 
			ConditionType.FarmLevel => farmLevelCondition, 
			ConditionType.FactionMission => factionMissionCondition, 
			ConditionType.Likability => likabilityCondition, 
			ConditionType.Month => monthCondition, 
			ConditionType.Unlock => unlockCondition, 
			ConditionType.RoomVisit => roomVisitCondition, 
			ConditionType.PortalVisit => portalVisitCondition, 
			_ => default(NoneCondition), 
		}).IsConditionMet(reverseCondition))
		{
			return true;
		}
		conditionFailedTextTip = conditionFailedText;
		return false;
	}

	public override string ToString()
	{
		return conditionType switch
		{
			ConditionType.Mission => missionCondition.ToString(), 
			ConditionType.EventDecorator => eventDecoratorCondition.ToString(), 
			ConditionType.FarmLevel => farmLevelCondition.ToString(), 
			ConditionType.FactionMission => factionMissionCondition.ToString(), 
			ConditionType.Likability => likabilityCondition.ToString(), 
			ConditionType.Month => monthCondition.ToString(), 
			ConditionType.Unlock => unlockCondition.ToString(), 
			ConditionType.RoomVisit => roomVisitCondition.ToString(), 
			ConditionType.PortalVisit => portalVisitCondition.ToString(), 
			_ => string.Empty, 
		};
	}
}
