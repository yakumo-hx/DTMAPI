using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("计划表入口", 0)]
[ParadoxNotion.Design.Icon("MacroIn", false, "")]
[Color("4876bb")]
public class NpcScheduleNodeEntrance : NpcScheduleNode
{
	[SerializeField]
	[ExposeField]
	private List<string> portalNames = new List<string>();

	public override int maxInConnections => 0;

	public override bool allowAsPrime => true;

	public string[] PortalNames => portalNames.ToArray();

	protected override bool CanConnectToTarget(Node targetNode)
	{
		if (!(targetNode is NpcScheduleNodeFilter))
		{
			return targetNode is NpcScheduleNodeTargetScene;
		}
		return true;
	}
}
