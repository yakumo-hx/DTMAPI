using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("计划表入口", 0)]
[Icon("MacroIn", false, "")]
[Color("4876bb")]
public class SwitchScheduleNodeEntrance : SwitchScheduleNode
{
	public override int maxInConnections => 0;

	public override bool allowAsPrime => true;

	protected override bool CanConnectToTarget(Node targetNode)
	{
		if (!(targetNode is SwitchScheduleNodeFilter))
		{
			return targetNode is SwitchScheduleNodeTarget;
		}
		return true;
	}
}
