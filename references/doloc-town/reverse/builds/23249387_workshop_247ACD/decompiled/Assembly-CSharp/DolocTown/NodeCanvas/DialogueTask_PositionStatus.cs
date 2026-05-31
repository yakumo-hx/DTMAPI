namespace DolocTown.NodeCanvas;

public class DialogueTask_PositionStatus : DialogueConditionTask
{
	public override string taskTitle => "检查是否与自定义事件对象接触";

	protected override bool CheckCondition()
	{
		return false;
	}
}
