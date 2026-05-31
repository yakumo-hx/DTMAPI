using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
public class BuildingLinkInfoForCellar : BuildingLinkInfo
{
	public readonly Building hostBuilding;

	[DebugInfo("所在位置")]
	public readonly Vector2 hostPosition;

	[DebugInfo("目标门索引")]
	public readonly int remoteIndex;

	[DebugInfo("横轴距离")]
	public readonly float dst;

	[DebugInfo("所在建筑")]
	public string hostBuildingTitle => hostBuilding.room.Title;

	public BuildingLinkInfoForCellar(Building targetBuilding, BuildingLinkType linkType, int diff, Building hostBuilding, Vector2 hostPosition, int remoteIndex, float dst)
		: base(targetBuilding, linkType, diff)
	{
		this.hostPosition = hostPosition;
		this.hostBuilding = hostBuilding;
		this.remoteIndex = remoteIndex;
		this.dst = dst;
	}
}
