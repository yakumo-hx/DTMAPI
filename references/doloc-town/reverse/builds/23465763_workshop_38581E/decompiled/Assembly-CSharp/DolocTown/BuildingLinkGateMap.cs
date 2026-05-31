using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DolocTown;

public class BuildingLinkGateMap
{
	private readonly BuildingLinkInfo[] allLinkInfos;

	private readonly BuildingLinkInfo[] leftLinks;

	private readonly BuildingLinkInfo[] rightLinks;

	private readonly BuildingLinkInfo[] topLinks;

	private readonly BuildingLinkInfo[] bottomLinks;

	public IEnumerable<BuildingLinkInfo> AllLinkInfos => allLinkInfos;

	public static BuildingLinkGateMap GenLinkGateMap(Building current, IEnumerable<Building> buildings, bool shouldCalcRemotePosition = true)
	{
		List<BuildingLinkInfo> list = new List<BuildingLinkInfo>();
		foreach (Building building in buildings)
		{
			if (building != current && !building.IsUnique && IsBuildingLinked(current, building, out var linkInfo))
			{
				list.Add(linkInfo);
			}
		}
		return new BuildingLinkGateMap(current, list.ToArray(), shouldCalcRemotePosition);
	}

	private static bool ConstraintCheck(Building target, Building other)
	{
		return true;
	}

	private static bool IsBuildingLinked(Building target, Building other, out BuildingLinkInfo linkInfo)
	{
		RectInt rectInt = new RectInt(other.Anchor, new Vector2Int(other.proto.NoOverlayWidth, other.proto.Height));
		int buildingLinkThresholdLeft = DolocAPI.GlobalParameter.BuildingLinkThresholdLeft;
		Vector2Int size = new Vector2Int(buildingLinkThresholdLeft, target.proto.Height);
		Vector2Int position = target.Anchor + new Vector2Int(-buildingLinkThresholdLeft, 0);
		RectInt other2 = new RectInt(position, size);
		if (rectInt.Overlaps(other2) && ConstraintCheck(target, other))
		{
			int diff = other.Anchor.y - target.Anchor.y;
			linkInfo = new BuildingLinkInfo(other, BuildingLinkType.Left, diff);
			return true;
		}
		int buildingLinkThresholdRight = DolocAPI.GlobalParameter.BuildingLinkThresholdRight;
		Vector2Int size2 = new Vector2Int(buildingLinkThresholdRight, target.proto.Height);
		Vector2Int position2 = target.Anchor + new Vector2Int(target.proto.NoOverlayWidth, 0);
		RectInt other3 = new RectInt(position2, size2);
		if (rectInt.Overlaps(other3) && ConstraintCheck(target, other))
		{
			int diff2 = other.Anchor.y - target.Anchor.y;
			linkInfo = new BuildingLinkInfo(other, BuildingLinkType.Right, diff2);
			return true;
		}
		int buildingLinkThresholdTop = DolocAPI.GlobalParameter.BuildingLinkThresholdTop;
		Vector2Int size3 = new Vector2Int(target.proto.NoOverlayWidth, buildingLinkThresholdTop);
		Vector2Int position3 = target.Anchor + new Vector2Int(0, target.proto.Height);
		RectInt other4 = new RectInt(position3, size3);
		if (rectInt.Overlaps(other4))
		{
			int diff3 = other.Anchor.x - target.Anchor.x;
			linkInfo = new BuildingLinkInfo(other, BuildingLinkType.Top, diff3);
			return true;
		}
		int buildingLinkThresholdBottom = DolocAPI.GlobalParameter.BuildingLinkThresholdBottom;
		Vector2Int size4 = new Vector2Int(target.proto.NoOverlayWidth, buildingLinkThresholdBottom);
		Vector2Int position4 = target.Anchor + new Vector2Int(0, -buildingLinkThresholdBottom);
		RectInt other5 = new RectInt(position4, size4);
		if (rectInt.Overlaps(other5))
		{
			int diff4 = other.Anchor.x - target.Anchor.x;
			linkInfo = new BuildingLinkInfo(other, BuildingLinkType.Bottom, diff4);
			return true;
		}
		linkInfo = null;
		return false;
	}

	private static int GetLinkGateIdx(Building current, Building remote, BuildingLinkType type, int fixedIndex, int linkCount)
	{
		if (type == BuildingLinkType.Bottom || type == BuildingLinkType.Top)
		{
			int x = remote.Anchor.x;
			int num = remote.Anchor.x + Mathf.RoundToInt((float)remote.proto.FrontFloorWidth * 0.5f);
			int x2 = current.Anchor.x;
			int num2 = current.Anchor.x + Mathf.RoundToInt((float)current.proto.FrontFloorWidth * 0.5f);
			int num3 = current.Anchor.x + current.proto.FrontFloorWidth;
			switch (linkCount)
			{
			case 1:
				if (x < num2)
				{
					return 0;
				}
				if (num < num3 && num > x2)
				{
					return 1;
				}
				return 2;
			case 2:
				if (fixedIndex == 0)
				{
					return 0;
				}
				if (num < num3 && num > x2)
				{
					return 1;
				}
				return 2;
			case 3:
				return fixedIndex;
			default:
				return -1;
			}
		}
		int y = remote.Anchor.y;
		int num4 = remote.Anchor.y + GetHalfHeight(remote.proto.Height);
		int y2 = current.Anchor.y;
		int num5 = current.Anchor.y + GetHalfHeight(current.proto.Height);
		int num6 = current.Anchor.y + current.proto.Height;
		switch (linkCount)
		{
		case 1:
			if (y < num5)
			{
				return 0;
			}
			if (num4 < num6 && num4 > y2)
			{
				return 1;
			}
			return 2;
		case 2:
			if (fixedIndex == 0)
			{
				return 0;
			}
			if (num4 < num6 && num4 > y2)
			{
				return 1;
			}
			return 2;
		case 3:
			return fixedIndex;
		default:
			return -1;
		}
	}

	private static int GetHalfHeight(int height)
	{
		if (height % 2 == 0)
		{
			return height / 2;
		}
		return height / 2 + 1;
	}

	private static bool _TryGetLinkInArray(BuildingLinkInfo[] array, int index, out BuildingLinkInfo linkInfo)
	{
		if (index < 0 || index >= array.Length)
		{
			linkInfo = null;
			return false;
		}
		linkInfo = array[index];
		return true;
	}

	private BuildingLinkGateMap(Building building, BuildingLinkInfo[] linkInfos = null, bool shouldCalcRemotePosition = true)
	{
		allLinkInfos = linkInfos;
		if (linkInfos == null || linkInfos.Length == 0)
		{
			leftLinks = Array.Empty<BuildingLinkInfo>();
			rightLinks = Array.Empty<BuildingLinkInfo>();
			topLinks = Array.Empty<BuildingLinkInfo>();
			bottomLinks = Array.Empty<BuildingLinkInfo>();
		}
		else
		{
			leftLinks = GroupLinks(building, BuildingLinkType.Left, shouldCalcRemotePosition);
			rightLinks = GroupLinks(building, BuildingLinkType.Right, shouldCalcRemotePosition);
			topLinks = GroupLinks(building, BuildingLinkType.Top, shouldCalcRemotePosition);
			bottomLinks = GroupLinks(building, BuildingLinkType.Bottom, shouldCalcRemotePosition);
		}
	}

	private BuildingLinkInfo[] GroupLinks(Building B, BuildingLinkType type, bool calcRemotePosition)
	{
		BuildingLinkInfo[] array = (from x in allLinkInfos
			where x.linkType == type
			orderby x.diff
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			BuildingLinkInfo buildingLinkInfo = array[i];
			int linkGateIdx = GetLinkGateIdx(B, buildingLinkInfo.targetBuilding, buildingLinkInfo.linkType, i, array.Length);
			buildingLinkInfo.__ResetIndex(linkGateIdx, i);
			if (calcRemotePosition)
			{
				buildingLinkInfo.__RecalcRemotePosition(B);
			}
		}
		return array;
	}

	public bool TryGetLinkTo(Building target, out BuildingLinkInfo linkInfo)
	{
		if (allLinkInfos == null || allLinkInfos.Length == 0)
		{
			linkInfo = null;
			return false;
		}
		BuildingLinkInfo[] array = allLinkInfos;
		foreach (BuildingLinkInfo buildingLinkInfo in array)
		{
			if (buildingLinkInfo.targetBuilding == target)
			{
				linkInfo = buildingLinkInfo;
				return true;
			}
		}
		linkInfo = null;
		return false;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BuildingLinkGateMap:");
		if (allLinkInfos == null || allLinkInfos.Length == 0)
		{
			stringBuilder.AppendLine("  No Link Info");
			return stringBuilder.ToString();
		}
		BuildingLinkInfo[] array = allLinkInfos;
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendLine("  " + array[i]);
		}
		return stringBuilder.ToString();
	}
}
