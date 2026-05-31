using System.Collections.Generic;
using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Icon("Filter", false, "")]
[Color("56528b")]
public abstract class NpcScheduleNodeFilter : NpcScheduleNode, INpcScheduleNodeFilter, INpcScheduleNode
{
	public IEnumerable<INpcScheduleNode> Children
	{
		get
		{
			foreach (Connection outConnection in base.outConnections)
			{
				if (outConnection.targetNode is INpcScheduleNode npcScheduleNode)
				{
					yield return npcScheduleNode;
				}
			}
		}
	}

	public abstract bool IsMatch(NpcScheduleParams npcScheduleParams);
}
