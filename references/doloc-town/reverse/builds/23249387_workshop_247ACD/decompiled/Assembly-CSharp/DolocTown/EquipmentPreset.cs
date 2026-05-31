using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class EquipmentPreset : ObjectPreset
{
	[SerializeField]
	public string equipmentName;

	[SerializeField]
	public bool autoAttach;

	private Sprite _sprite;

	[SerializeField]
	public CountItemListConfig inventory;

	private EquipmentInfo equipmentData => DolocConfig.Tables.TbEquipment.GetOrDefault(equipmentName);

	public override Sprite Sprite => _sprite;

	public override Vector2Int GridSize => equipmentData?.CoverSize ?? Vector2Int.one;

	public override string PresetObjectName => equipmentData?.Id ?? "未知设备";

	private bool isContainer => equipmentData.Function is EquipmentFuncCase;

	public override bool CreatePreset(out RoomPresetObjectSO preset)
	{
		if (equipmentData == null)
		{
			preset = null;
			return false;
		}
		string id = equipmentData.Id;
		Vector2Int localGridPosition = LocalGridPosition;
		CountItemListConfig countItemListConfig = inventory;
		bool flag = autoAttach;
		preset = new RoomPresetObjectSO(RoomPresetObjectType.Equipment, id, localGridPosition, 0, disableRefresh: false, countItemListConfig, default(PlatformPositionInfo), flag);
		return true;
	}
}
