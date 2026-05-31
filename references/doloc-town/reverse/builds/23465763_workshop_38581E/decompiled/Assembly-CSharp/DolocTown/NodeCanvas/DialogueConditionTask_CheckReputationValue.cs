using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("声望值检查", 0)]
[Description("检查声望值是否达到某个值")]
public class DialogueConditionTask_CheckReputationValue : DialogueConditionTask
{
	public string FactionName;

	public int TargetReputationValue;

	public override string taskTitle => $"达到目标声望值：{FactionName}@{TargetReputationValue}";

	protected override bool CheckCondition()
	{
		return DolocAPI.archiveHandle.cityData.treatyPortFactionManager.GetReputationValue(FactionName) >= TargetReputationValue;
	}
}
