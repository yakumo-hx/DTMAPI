using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Building;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public static class CellarExtension
{
	public static void RebuildCellarBuildingGates(this ISceneHandle sceneHandle, Room room)
	{
		if (room is TemplateRoomInHouse templateRoomInHouse && templateRoomInHouse.Building.IsUnique)
		{
			sceneHandle._RebuildCellarBuildingGates();
		}
	}

	public static bool TryGetCellar(out TemplateRoomInHouse cellarRoom)
	{
		cellarRoom = null;
		if (DolocAPI.archiveHandle == null)
		{
			return false;
		}
		foreach (Building building in DolocAPI.archiveHandle.MainFarm.DM_building.Buildings)
		{
			if (!building.IsNotCellar())
			{
				TemplateRoomInHouse room = building.room;
				if (room != null)
				{
					cellarRoom = room;
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsCellar(this Building building)
	{
		return building?.BuildingName.Contains("cellar") ?? false;
	}

	public static bool IsNotCellar(this Building building)
	{
		return !building.IsCellar();
	}

	public static bool IsConnectedToCellar(this Building building, out string reason)
	{
		reason = string.Empty;
		if (building == null)
		{
			reason = "建筑为空";
			return false;
		}
		if (building.IsCellar())
		{
			reason = "建筑为地窖";
			return false;
		}
		Vector2Int size = new Vector2Int(building.proto.CoverSize.x, 1);
		Vector2Int anchor = new Vector2Int(building.Anchor.x, building.Anchor.y - 1);
		Vector2Int[] source = size.IterateGrid(anchor).ToArray();
		Terrain terrain = building.room.RootRoom.DM_terrain;
		if (source.Any((Vector2Int pos) => terrain.IsFilled(pos, TerrainLayerName.Building) && (terrain.GetContent<Building>(pos)?.IsCellar() ?? false)))
		{
			return true;
		}
		if (!building.IsOverCellarBorder)
		{
			reason = "建筑未越过地窖边界线";
			return false;
		}
		bool num = source.All((Vector2Int pos) => !terrain.IsEmpty(pos) && terrain.IsFilled(pos, TerrainLayerName.Ground));
		if (!num)
		{
			reason = "建筑不完全位于地面之上";
		}
		return num;
	}

	public static void RenderAllLinkGatesForCellar(this TemplateRoomInHouse room)
	{
		Dictionary<BuildingLinkGateId, BuildingLinkGate> dictionary = room.LoadLinkGatesInScene();
		BuildingLinkInfo[] array = room.Building.GenLinkMapForCellar();
		foreach (BuildingLinkInfo buildingLinkInfo in array)
		{
			BuildingLinkGateId key = new BuildingLinkGateId(buildingLinkInfo.linkType, buildingLinkInfo.index);
			if (dictionary.TryGetValue(key, out var value))
			{
				value.Setup(buildingLinkInfo);
			}
		}
	}

	public static void RenderAllLinkGatesToCellar(this TemplateRoomInHouse room)
	{
		Dictionary<BuildingLinkGateId, BuildingLinkGate> dictionary = room.LoadLinkGatesInScene();
		HashSet<BuildingLinkGateId> hashSet = new HashSet<BuildingLinkGateId>();
		BuildingLinkInfoForCellar[] array = room.Building.GenLinkMapToCellarGlobalEx();
		foreach (BuildingLinkInfoForCellar buildingLinkInfoForCellar in array)
		{
			if (buildingLinkInfoForCellar.IsValid)
			{
				BuildingLinkGateId buildingLinkGateId = new BuildingLinkGateId(buildingLinkInfoForCellar.linkType, buildingLinkInfoForCellar.index);
				if (hashSet.Add(buildingLinkGateId) && dictionary.TryGetValue(buildingLinkGateId, out var value))
				{
					value.Setup(buildingLinkInfoForCellar);
				}
			}
		}
	}

	public static BuildingLinkInfo[] GenLinkMapForCellar(this Building cellarBuilding)
	{
		if (cellarBuilding.IsNotCellar())
		{
			return Array.Empty<BuildingLinkInfo>();
		}
		BuildingLinkInfoForCellar[] array = cellarBuilding.GenLinkMapToCellarGlobalEx(ignoreHostBuilding: true);
		List<BuildingLinkInfo> list = new List<BuildingLinkInfo>();
		BuildingLinkInfoForCellar[] array2 = array;
		foreach (BuildingLinkInfoForCellar buildingLinkInfoForCellar in array2)
		{
			BuildingLinkInfo buildingLinkInfo = new BuildingLinkInfo(buildingLinkInfoForCellar.hostBuilding, buildingLinkInfoForCellar.linkType.Invert(), 0);
			buildingLinkInfo.__ResetIndex(buildingLinkInfoForCellar.remoteIndex, buildingLinkInfoForCellar.remoteIndex);
			buildingLinkInfo.__RecalcRemotePosition(buildingLinkInfoForCellar.hostPosition);
			list.Add(buildingLinkInfo);
		}
		return list.ToArray();
	}

	[Command("debug_link_gate", Desc = "检查当前建筑内的联通门状态")]
	public static void DEBUG_Building()
	{
		if (!(DolocAPI.CurrentRoom is TemplateRoomInHouse templateRoomInHouse))
		{
			Debug.Log("<color=red>当前不在房屋内</color>");
			return;
		}
		if (!TryGetCellar(out var cellarRoom))
		{
			Debug.Log("<color=red>未找到地窖房间</color>");
			return;
		}
		Building building = templateRoomInHouse.Building;
		if (!building.IsConnectedToCellar(out var reason))
		{
			Debug.Log("当前建筑不通向地窖:" + reason);
			return;
		}
		BuildingLinkInfoForCellar[] array = building._GenLinkMapToCellar(cellarRoom);
		Debug.Log("共找到联通门数量：" + array.Length);
		Debug.Log("==== 联通门信息 ====");
		BuildingLinkInfoForCellar[] array2 = array;
		foreach (BuildingLinkInfoForCellar buildingLinkInfoForCellar in array2)
		{
			Debug.Log("找到联通门：" + buildingLinkInfoForCellar.hostBuilding.BuildingName + " " + $"HostIndex:{buildingLinkInfoForCellar.index} RemoteIndex:{buildingLinkInfoForCellar.remoteIndex} " + $"HostPos:{buildingLinkInfoForCellar.hostPosition} RemotePos:{buildingLinkInfoForCellar.remotePosition} Dst:{buildingLinkInfoForCellar.dst}");
		}
	}

	private static BuildingLinkInfoForCellar[] GenLinkMapToCellarGlobalEx(this Building building, bool ignoreHostBuilding = false)
	{
		if (!TryGetCellar(out var cellarRoom))
		{
			return Array.Empty<BuildingLinkInfoForCellar>();
		}
		string reason;
		List<Building> list = cellarRoom.RootRoom.DM_building.Buildings.Where((Building B) => B.IsConnectedToCellar(out reason)).ToList();
		list.Sort((Building B1, Building B2) => B2.Anchor.x - B1.Anchor.x);
		Dictionary<Building, BuildingLinkInfoForCellar> dictionary = new Dictionary<Building, BuildingLinkInfoForCellar>();
		HashSet<int> usedRemoteIndices = new HashSet<int>();
		foreach (Building item in list)
		{
			BuildingLinkInfoForCellar[] array = item._GenLinkMapToCellar(cellarRoom);
			if (!array.Any((BuildingLinkInfoForCellar x) => !usedRemoteIndices.Contains(x.remoteIndex)))
			{
				continue;
			}
			BuildingLinkInfoForCellar buildingLinkInfoForCellar = array.First((BuildingLinkInfoForCellar x) => !usedRemoteIndices.Contains(x.remoteIndex));
			BuildingLinkInfoForCellar[] array2 = array;
			foreach (BuildingLinkInfoForCellar buildingLinkInfoForCellar2 in array2)
			{
				if (!usedRemoteIndices.Contains(buildingLinkInfoForCellar2.remoteIndex) && buildingLinkInfoForCellar2.remoteIndex > buildingLinkInfoForCellar.remoteIndex)
				{
					buildingLinkInfoForCellar = buildingLinkInfoForCellar2;
				}
			}
			dictionary.Add(item, buildingLinkInfoForCellar);
			usedRemoteIndices.Add(buildingLinkInfoForCellar.remoteIndex);
		}
		return dictionary.Values.Where((BuildingLinkInfoForCellar x) => ignoreHostBuilding || x.hostBuilding == building).ToArray();
	}

	private static BuildingLinkInfoForCellar[] GenLinkMapToCellarGlobal(this Building building, bool ignoreHostBuilding = false)
	{
		if (!TryGetCellar(out var cellarRoom))
		{
			return Array.Empty<BuildingLinkInfoForCellar>();
		}
		string reason;
		List<Building> list = cellarRoom.RootRoom.DM_building.Buildings.Where((Building B) => B.IsConnectedToCellar(out reason)).ToList();
		Dictionary<BuildingLinkGateId, BuildingLinkInfoForCellar> dictionary = new Dictionary<BuildingLinkGateId, BuildingLinkInfoForCellar>();
		foreach (Building item in list)
		{
			BuildingLinkInfoForCellar[] array = item._GenLinkMapToCellar(cellarRoom);
			foreach (BuildingLinkInfoForCellar buildingLinkInfoForCellar in array)
			{
				BuildingLinkGateId key = new BuildingLinkGateId(buildingLinkInfoForCellar.linkType, buildingLinkInfoForCellar.remoteIndex);
				if (!dictionary.TryAdd(key, buildingLinkInfoForCellar) && buildingLinkInfoForCellar.dst < dictionary[key].dst)
				{
					dictionary[key] = buildingLinkInfoForCellar;
				}
			}
		}
		Dictionary<Building, BuildingLinkInfoForCellar> dictionary2 = new Dictionary<Building, BuildingLinkInfoForCellar>();
		foreach (BuildingLinkInfoForCellar value in dictionary.Values)
		{
			if (!dictionary2.TryAdd(value.hostBuilding, value) && value.dst < dictionary2[value.hostBuilding].dst)
			{
				dictionary2[value.hostBuilding] = value;
			}
		}
		return dictionary2.Values.Where((BuildingLinkInfoForCellar x) => ignoreHostBuilding || x.hostBuilding == building).ToArray();
	}

	private static BuildingLinkInfoForCellar[] _GenLinkMapToCellar(this Building building, TemplateRoomInHouse cellarRoom)
	{
		List<float> list = new List<float>();
		for (int i = 0; i < 3; i++)
		{
			if (building.proto.TryGetLinkGatePosition(building.level, BuildingLinkType.Bottom, i, out var position))
			{
				float item = WorldXToCellarX(cellarRoom, building.BuildingToWorld(position).x);
				list.Add(item);
			}
		}
		List<BuildingLinkInfoForCellar> list2 = new List<BuildingLinkInfoForCellar>();
		BuildingInfo proto = cellarRoom.Building.proto;
		for (int j = 0; j < list.Count; j++)
		{
			float position2 = list[j];
			if (proto.GetNearestLinkGatePosition(building.level, BuildingLinkType.Top, position2, out var output, out var index, out var minDst) && !(minDst > DolocAPI.GlobalParameter.CellarLinkGateClipThreshold) && building.proto.TryGetLinkGatePosition(building.level, BuildingLinkType.Bottom, j, out var position3))
			{
				BuildingLinkInfoForCellar buildingLinkInfoForCellar = new BuildingLinkInfoForCellar(cellarRoom.Building, BuildingLinkType.Bottom, j, building, position3, index, minDst);
				buildingLinkInfoForCellar.__ResetIndex(j, j);
				buildingLinkInfoForCellar.__RecalcRemotePosition(output);
				list2.Add(buildingLinkInfoForCellar);
			}
		}
		return list2.ToArray();
	}

	public static void _RebuildCellarBuildingGates(this ISceneHandle sceneHandle)
	{
		if (sceneHandle == null || DolocAPI.archiveHandle == null)
		{
			return;
		}
		HideAllOriginGates(sceneHandle);
		IEnumerable<Building> enumerable = DolocAPI.archiveHandle.MainFarm.DM_building.Buildings.Where((Building b) => b.IsUnique);
		Dictionary<int, BuildingGate> dictionary = new Dictionary<int, BuildingGate>();
		foreach (Building item in enumerable)
		{
			int num = Mathf.RoundToInt(item.GetCellarGatePosition());
			if (dictionary.TryGetValue(num, out var value))
			{
				value.UseCacheBuilding = true;
				continue;
			}
			BuildingGate buildingGate = DolocAPI.EntitySystem.Next<BuildingGate>("cellar_gate");
			buildingGate.UseCacheBuilding = false;
			buildingGate.TargetBuilding = item;
			buildingGate.position2d = new Vector2(num, item.room.RoomPosition.y + 1.5f);
			dictionary.Add(num, buildingGate);
		}
	}

	private static void HideAllOriginGates(ISceneHandle handle)
	{
		BuildingGate[] componentsInScene = handle.GetComponentsInScene<BuildingGate>();
		for (int i = 0; i < componentsInScene.Length; i++)
		{
			componentsInScene[i].SetVisible(value: false);
		}
	}

	public static IEnumerable<(Building, Vector2)> GetAllQuitPositionsPair(this TemplateRoomInHouse room)
	{
		if (!room.IsUniqueBuilding)
		{
			yield return (room.Building, room.Geometry.DefaultEntryPosition);
		}
		foreach (Building building in room.Buildings)
		{
			float cellarGatePosition = building.GetCellarGatePosition();
			Vector2 item = new Vector2(cellarGatePosition, room.RoomPosition.y + 1.5f);
			yield return (building, item);
		}
	}

	public static bool IsOverlappedCellarBuilding(this Building building)
	{
		IEnumerable<Building> enumerable = building.room.RootRoom.DM_building.Buildings.Where((Building B) => B.IsUnique);
		int num = Mathf.RoundToInt(building.GetCellarGatePosition());
		foreach (Building item in enumerable)
		{
			if (item != building && Mathf.RoundToInt(item.GetCellarGatePosition()) == num)
			{
				return true;
			}
		}
		return false;
	}

	public static Vector2 GetCellarGatePosition2(this Building building, float paddingDst = 1.5f)
	{
		float x = building.EntryPosition.x;
		float value = WorldXToCellarX(building.room, x);
		float min = building.room.RoomPosition.x + paddingDst;
		float max = building.room.RoomPosition.x + building.room.RoomSize.x - paddingDst;
		return new Vector2(Mathf.Clamp(value, min, max), building.room.RoomPosition.y + 1.5f);
	}

	public static float GetCellarGatePosition(this Building building, float paddingDst = 1.5f)
	{
		float x = building.EntryPosition.x;
		float value = WorldXToCellarX(building.room, x);
		float min = building.room.RoomPosition.x + paddingDst;
		float max = building.room.RoomPosition.x + building.room.RoomSize.x - paddingDst;
		return Mathf.Clamp(value, min, max);
	}

	public static float WorldXToCellarX(Room cellarRoom, float worldX)
	{
		float cellarRightBorderWorldPositionX = GetCellarRightBorderWorldPositionX();
		float num = worldX - cellarRightBorderWorldPositionX;
		return cellarRoom.RoomPosition.x + cellarRoom.RoomSize.x + num;
	}

	public static float GetCellarRightBorderWorldPositionX()
	{
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.MainFarm;
		Vector2 scenePosition = mainFarm.ScenePosition;
		Vector2 sceneSize = mainFarm.SceneSize;
		return scenePosition.x + sceneSize.x - DolocAPI.GlobalParameter.CellarDistanceToFarmRight;
	}
}
