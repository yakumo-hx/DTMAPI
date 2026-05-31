using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("目标传送点", 0)]
[Color("60b37e")]
public class NpcScheduleNodeTargetScene : NpcScheduleNodeResult
{
	private enum PortalRange
	{
		Npc,
		Scene,
		All
	}

	[SerializeField]
	private PortalRange portalRange;

	[SerializeField]
	private string targetSceneName = "";

	[SerializeField]
	[ExposeField]
	private string portalName = "";

	[SerializeField]
	private string portalSceneName = "";

	public override string MarkPointName => portalName;

	public override NpcScheduleWork Work
	{
		get
		{
			if (base.outConnections.Count == 0)
			{
				return new NpcScheduleWorkStreet();
			}
			foreach (Connection outConnection in base.outConnections)
			{
				if (outConnection.targetNode is NpcScheduleNodeTargetWork npcScheduleNodeTargetWork)
				{
					return npcScheduleNodeTargetWork.Work;
				}
			}
			return new NpcScheduleWorkStreet();
		}
	}
}
