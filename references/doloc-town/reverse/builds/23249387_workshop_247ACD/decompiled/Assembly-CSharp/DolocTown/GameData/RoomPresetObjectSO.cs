using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public class RoomPresetObjectSO
{
	[SerializeField]
	public RoomPresetObjectType presetType;

	[SerializeField]
	public string name;

	[SerializeField]
	public Vector2Int pos;

	[SerializeField]
	public int initGrowthLevel;

	[SerializeField]
	public bool disableRefresh;

	[SerializeField]
	private CountItemListConfig inventory;

	[SerializeField]
	public PlatformPositionInfo platformInfo;

	[SerializeField]
	public bool autoAttach;

	private bool isResource => presetType == RoomPresetObjectType.Resource;

	private bool hasInventory => !inventory.isEmpty;

	private bool isPlatform => presetType == RoomPresetObjectType.Platform;

	private bool isEquipment => presetType == RoomPresetObjectType.Equipment;

	public virtual RoomPresetObjectProto Proto => new RoomPresetObjectProto(presetType, name, pos, initGrowthLevel, disableRefresh, inventory.countItems, platformInfo, autoAttach);

	public RoomPresetObjectSO(RoomPresetObjectType presetType, string name, Vector2Int pos, int initGrowthLevel = 0, bool disableRefresh = false, CountItemListConfig inventory = default(CountItemListConfig), PlatformPositionInfo platformInfo = default(PlatformPositionInfo), bool autoAttach = false)
	{
		this.presetType = presetType;
		this.name = name;
		this.pos = pos;
		this.initGrowthLevel = initGrowthLevel;
		this.disableRefresh = disableRefresh;
		this.inventory = inventory;
		this.platformInfo = platformInfo;
		this.autoAttach = autoAttach;
	}
}
