using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("任务链入口", 0)]
[Color("ff7f50")]
[Icon("Macroin", false, "")]
[Description("任务链入口")]
public class MissionNodeEntrance : MissionNode
{
	public override MissionNodeType nodeType => MissionNodeType.ENTRANCE;

	public override Alignment2x2 iconAlignment => Alignment2x2.Top;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override string name => "任务链入口";

	public override int maxOutConnections => -1;

	public override int maxInConnections => 0;

	public override Type outConnectionType => typeof(MissionConnection);

	public override bool allowAsPrime => true;

	public override bool canSelfConnect => false;

	protected override bool CanConnectToTarget(Node targetNode)
	{
		return !(targetNode is MissionNodeReward);
	}
}
