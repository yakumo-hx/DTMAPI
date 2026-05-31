using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Building;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class BuildingManager
{
	private readonly Dictionary<string, Building> buildings = new Dictionary<string, Building>();

	private readonly HashSet<Room> updateCache = new HashSet<Room>();

	public IEnumerable<Building> Buildings => buildings.Values;

	public int Count => buildings.Count;

	public Building FirstBuilding
	{
		get
		{
			if (buildings.Count == 0)
			{
				return null;
			}
			return buildings.First().Value;
		}
	}

	[JsonProperty("buildings")]
	private Building[] BuildingsArray => buildings.Values.ToArray();

	public BuildingManager()
	{
	}

	[JsonConstructor]
	protected BuildingManager(Building[] buildings)
	{
		if (buildings != null)
		{
			foreach (Building building in buildings)
			{
				this.buildings.Add(building.TargetRoomGuid, building);
			}
		}
	}

	public void UpdateNoRender()
	{
		updateCache.Clear();
		foreach (Building value in buildings.Values)
		{
			if (updateCache.Add(value.room))
			{
				value.room.UpdateNoRender();
			}
		}
	}

	public void UpdateSpec()
	{
		updateCache.Clear();
		foreach (Building value in buildings.Values)
		{
			if (updateCache.Add(value.room))
			{
				if (value.room.isRenderNow)
				{
					value.room.Update();
				}
				else
				{
					value.room.UpdateNoRender();
				}
			}
		}
	}

	public bool QueryBuildingByRoomTitle(string guid, out Building building)
	{
		return buildings.TryGetValue(guid, out building);
	}

	public bool QueryBuildingRoom(string guid, out TemplateRoom room)
	{
		if (buildings.TryGetValue(guid, out var value))
		{
			room = value.room;
			return true;
		}
		room = null;
		return false;
	}

	public bool ContainsBuilding(Building entity)
	{
		return buildings.ContainsKey(entity.TargetRoomGuid);
	}

	public Building GetBuilding(string guid)
	{
		if (guid == null || !buildings.ContainsKey(guid))
		{
			return null;
		}
		return buildings[guid];
	}

	public Building GetRandomBuilding()
	{
		if (buildings.Count == 0)
		{
			return null;
		}
		return buildings.ElementAt(Random.Range(0, buildings.Count)).Value;
	}

	public bool CreateBuilding(Room parent, Vector2Int anchor, Vector3 wp, BuildingInfo proto, out Building output)
	{
		output = null;
		if (proto == null)
		{
			return false;
		}
		if (!DolocAPI.assets.rooms.QueryData(proto.DefaultLevelData.TemplateRoomName, out var data) || !data.isInHouse)
		{
			return false;
		}
		output = new Building(parent, proto, data, anchor, wp);
		buildings.Add(output.TargetRoomGuid, output);
		return true;
	}

	public Building CreateBuildingFromDirty(Room parent, Vector2Int anchor, Vector3 wp, Building dirty)
	{
		if (dirty == null)
		{
			return null;
		}
		if (!DolocAPI.assets.rooms.QueryData(dirty.templateRoomName, out var data) || !data.isInHouse)
		{
			return null;
		}
		dirty.MoveTerrainContent(anchor, wp);
		dirty.room.SetParent(parent);
		buildings.Add(dirty.TargetRoomGuid, dirty);
		return dirty;
	}

	public bool RemoveBuilding(Building building)
	{
		if (buildings.ContainsKey(building.TargetRoomGuid))
		{
			buildings.Remove(building.TargetRoomGuid);
			return true;
		}
		return false;
	}
}
