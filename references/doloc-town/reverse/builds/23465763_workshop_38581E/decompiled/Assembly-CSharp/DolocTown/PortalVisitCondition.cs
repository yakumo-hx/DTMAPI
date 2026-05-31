using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public class PortalVisitCondition : ICondition
{
	[SerializeField]
	private string portalId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (portalId.IsNullOrEmpty())
		{
			return true;
		}
		return DolocAPI.archiveHandle.farmData.mapManager.IsPortalVisited(portalId) != reverseCondition;
	}

	public override string ToString()
	{
		return "使用过传送门<" + portalId + ">";
	}
}
