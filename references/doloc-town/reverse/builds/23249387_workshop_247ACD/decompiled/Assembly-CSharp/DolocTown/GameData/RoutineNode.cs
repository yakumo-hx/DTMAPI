using System;
using DolocTown.NodeCanvas;
using ParadoxNotion;
using ParadoxNotion.Design;

namespace DolocTown.GameData;

[Color("8c7ca0")]
public abstract class RoutineNode : HorizontalLinkedNode
{
	public override int maxInConnections => -1;

	public override int maxOutConnections => -1;

	public sealed override Type outConnectionType => typeof(TransactionScheduleConnection);

	public override bool allowAsPrime => false;

	public override bool canSelfConnect => false;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;
}
