using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDroneAssist : Item, IDroneComponentItem
{
	private readonly DroneAssistInfo assistProto;

	public override bool invalid
	{
		get
		{
			if (!base.invalid)
			{
				return assistProto == null;
			}
			return true;
		}
	}

	public ComponentType ComponentType => ComponentType.Assist;

	public bool IsDroneComponentValid => assistProto != null;

	public Sprite ComponentSprite => assistProto.Sprite.Asset;

	public string ComponentId => base.proto.Id;

	public string SkillId => assistProto.SkillId;

	public Vector2Int ComponentPivot => assistProto.SpritePivot;

	public string ComponentTitle => title;

	public string ComponentDescription => description;

	public ItemDroneAssist(ItemInfo proto, int count)
		: base(proto, count)
	{
		assistProto = DolocConfig.Tables.TbDroneAssist.GetOrDefault(proto.Id);
	}

	[JsonConstructor]
	public ItemDroneAssist(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		assistProto = DolocConfig.Tables.TbDroneAssist.GetOrDefault(base.proto.Id);
	}
}
