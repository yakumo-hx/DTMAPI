using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class BuildingPreset : ObjectPreset
{
	[SerializeField]
	protected string buildingName;

	[SerializeField]
	[HideInInspector]
	protected Vector2Int gridSize;

	private Sprite _sprite;

	private BuildingInfo buildingData => DolocConfig.Tables.TbBuilding.GetOrDefault(buildingName);

	public override Sprite Sprite => _sprite;

	public override Vector2Int GridSize => gridSize;

	public override string PresetObjectName => buildingData?.Id ?? "未知建筑";

	public override bool CreatePreset(out RoomPresetObjectSO preset)
	{
		if (buildingName == null)
		{
			preset = null;
			return false;
		}
		preset = new RoomPresetObjectSO(RoomPresetObjectType.Building, buildingName, LocalGridPosition);
		return true;
	}
}
