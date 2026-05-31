using UnityEngine;

namespace DolocTown.GameData;

public class RoomPresetObjectProto
{
	public readonly RoomPresetObjectType presetType;

	public readonly string name;

	public readonly Vector2Int pos;

	public readonly int initGrowthLevel;

	public bool disableRefresh;

	public readonly CountItem[] inventory;

	public readonly PlatformPositionInfo platformInfo;

	public readonly bool autoAttach;

	public RoomPresetObjectProto(RoomPresetObjectType presetType, string name, Vector2Int pos, int initGrowthLevel, bool disableRefresh, CountItem[] inventory, PlatformPositionInfo platformInfo, bool autoAttach)
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
