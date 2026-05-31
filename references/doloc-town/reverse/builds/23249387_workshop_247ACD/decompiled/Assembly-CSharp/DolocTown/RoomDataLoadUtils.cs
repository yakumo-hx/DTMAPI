using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public static class RoomDataLoadUtils
{
	public static void __AfterLoadAllTerrain(this Room room)
	{
		if (room == null)
		{
			return;
		}
		if (room == null)
		{
			return;
		}
		List<IDecal> list = new List<IDecal>();
		foreach (Equipment allEquipment in ((IEquipmentHost)room).DM_equipment.AllEquipments)
		{
			if (allEquipment.proto.isDecal)
			{
				list.Add(allEquipment);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		IDecalHost[] allCachedDecalHosts = ((IBaseHost)room).DM_terrain.GetAllCachedDecalHosts();
		Dictionary<(string, int), IDecalHost> dictionary = new Dictionary<(string, int), IDecalHost>();
		IDecalHost[] array = allCachedDecalHosts;
		foreach (IDecalHost decalHost in array)
		{
			dictionary.TryAdd((decalHost.GetType().Name, decalHost.index), decalHost);
		}
		foreach (IDecal item in list)
		{
			if (dictionary.TryGetValue((item.DecalInfo.DecalHostType, item.DecalInfo.DecalHostIndex), out var value))
			{
				item.SetDecalHost(value);
				value.AttachedDecals[item.DecalSlotIndex] = item;
				item.RefreshPosition();
				((IBaseHost)room).DM_terrain.FillContent(item as Equipment);
			}
			else
			{
				Debug.LogError($"贴纸{item.Name}没有找到对应的宿主 Type<{item.DecalInfo.DecalHostType}> Index<{item.DecalInfo.DecalHostIndex}>");
				((IEquipmentHost)room).RemoveEquipment(item as Equipment, putInBackpack: false, retrieveItem: true, includeTerrain: true);
			}
		}
	}
}
