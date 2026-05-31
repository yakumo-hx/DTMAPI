using System;
using DolocTown.GameData;
using ParadoxNotion;

namespace DolocTown.NodeCanvas;

public abstract class NpcScheduleNode : HorizontalLinkedNode, INpcScheduleNode
{
	public override int maxOutConnections => -1;

	public override int maxInConnections => -1;

	public override bool allowAsPrime => false;

	public override bool canSelfConnect => false;

	public override Type outConnectionType => typeof(NpcScheduleConnection);

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;
}
