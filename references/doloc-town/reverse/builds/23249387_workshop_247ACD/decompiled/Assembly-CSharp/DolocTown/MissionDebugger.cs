using DolocTown.GameData;
using DolocTown.NodeCanvas;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public static class MissionDebugger
{
	[Command("find_global_condition_missions")]
	public static void FindMissions()
	{
		foreach (MissionGraph totalValue in DolocAPI.assets.missionChains.totalValues)
		{
			foreach (MissionNode allMissionNode in totalValue.AllMissionNodes)
			{
				if (allMissionNode is MissionNodeListener node)
				{
					CheckConditionInNode(node);
				}
			}
		}
	}

	private static void CheckConditionInNode(MissionNodeListener node)
	{
		int num = 0;
		MissionRequire[] missionRequires = node.MissionRequires;
		foreach (MissionRequire obj in missionRequires)
		{
			num++;
			if (IsRequireMatching((MissionRequire_Graph)obj))
			{
				Debug.Log($"Found matching require in node {node.graph.name}/{node.MissionId}/{num}");
			}
		}
	}

	private static bool IsRequireMatching(MissionRequire_Graph require)
	{
		if (require.Condition == null)
		{
			return false;
		}
		if (require.Template is MissionRequireTemplateUniversal missionRequireTemplateUniversal)
		{
			return missionRequireTemplateUniversal.isGlobalCount;
		}
		return false;
	}
}
