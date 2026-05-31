using System.Collections.Generic;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionItemManager
{
	[JsonProperty]
	private readonly IndexList<MissionItem> items = new IndexList<MissionItem>();

	public IEnumerable<MissionItem> allItems => items;

	public MissionItemManager()
	{
	}

	[JsonConstructor]
	public MissionItemManager(IndexList<MissionItem> items)
	{
		this.items = items;
	}

	public MissionItem CreateMissionItem(MissionItemInfo proto, Vector2 pos)
	{
		MissionItem missionItem = new MissionItem(proto, pos);
		items.Add(missionItem);
		return missionItem;
	}

	public bool RemoveMissionItem(MissionItem item)
	{
		return items.Remove(item);
	}
}
