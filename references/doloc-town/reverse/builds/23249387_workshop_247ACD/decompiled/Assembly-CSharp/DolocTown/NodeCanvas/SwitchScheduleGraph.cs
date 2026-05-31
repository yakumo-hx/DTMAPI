using System;
using DolocTown.GameData;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
[CreateAssetMenu(menuName = "多洛可小镇[城镇]/开关计划表")]
public class SwitchScheduleGraph : Graph, ISwitchSchedule
{
	public override Type baseNodeType => typeof(SwitchScheduleNode);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => true;

	public override bool isTree => true;

	public override bool allowBlackboardOverrides => false;

	public override bool canAcceptVariableDrops => true;

	public bool IsTrue(SwitchScheduleParams param)
	{
		if (base.primeNode == null)
		{
			return false;
		}
		foreach (Connection outConnection in base.primeNode.outConnections)
		{
			Node targetNode = outConnection.targetNode;
			bool result;
			if (!(targetNode is ISwitchScheduleNodeFilter filter))
			{
				if (targetNode is ISwitchScheduleNodeTarget { ShouldLight: var shouldLight })
				{
					return shouldLight;
				}
			}
			else if (CheckFilter(param, filter, out result))
			{
				return result;
			}
		}
		return false;
	}

	private static bool CheckFilter(SwitchScheduleParams param, ISwitchScheduleNodeFilter filter, out bool result)
	{
		result = false;
		if (!filter.IsMatch(param))
		{
			return false;
		}
		foreach (ISwitchScheduleNode child in filter.Children)
		{
			if (!(child is ISwitchScheduleNodeFilter filter2))
			{
				if (child is ISwitchScheduleNodeTarget switchScheduleNodeTarget)
				{
					result = switchScheduleNodeTarget.ShouldLight;
					return true;
				}
			}
			else if (CheckFilter(param, filter2, out result))
			{
				return true;
			}
		}
		return false;
	}
}
