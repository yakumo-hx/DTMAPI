using System;
using NodeCanvas.Framework;
using ParadoxNotion;

namespace DolocTown.NodeCanvas;

public abstract class GameProcessNode : Node
{
	public override int maxInConnections => -1;

	public override int maxOutConnections => -1;

	public sealed override Type outConnectionType => typeof(GameProcessConnection);

	public override bool allowAsPrime => true;

	public override bool canSelfConnect => false;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;
}
