using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Color("4876bb")]
public abstract class SwitchScheduleNodeFilter : SwitchScheduleNode, ISwitchScheduleNodeFilter, ISwitchScheduleNode
{
	public IEnumerable<ISwitchScheduleNode> Children => base.outConnections.Select((Connection conn) => conn.targetNode as ISwitchScheduleNode);

	public abstract bool IsMatch(SwitchScheduleParams param);
}
