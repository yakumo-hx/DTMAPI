using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.GameData;

[Name("事务计划表入口", 0)]
[Description("将事务节点连接到事务计划表的入口才可以触发它们")]
public class RoutineNodeEntrance : RoutineNode
{
	public override bool allowAsPrime => true;

	public override int maxInConnections => 0;

	protected override bool CanConnectToTarget(Node targetNode)
	{
		return targetNode is RoutineNode_Trigger;
	}
}
