using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDroneWeapon : Item, IDroneComponentItem
{
	public readonly DroneWeaponInfo DroneWeaponProto;

	public override bool invalid
	{
		get
		{
			if (!base.invalid)
			{
				return DroneWeaponProto == null;
			}
			return true;
		}
	}

	public ComponentType ComponentType => ComponentType.Weapon;

	public bool IsDroneComponentValid => DroneWeaponProto != null;

	public Sprite ComponentSprite => DroneWeaponProto.Sprite.Asset;

	public string ComponentId => base.proto.Id;

	public string SkillId => DroneWeaponProto?.WeaponSkill;

	public Vector2Int ComponentPivot => DroneWeaponProto.SpritePivot;

	public string ComponentTitle => title;

	public string ComponentDescription => description;

	public ItemDroneWeapon(ItemInfo item, int count)
		: base(item, count)
	{
		DroneWeaponProto = DolocConfig.Tables.TbDroneWeapon.GetOrDefault(item.Id);
	}

	[JsonConstructor]
	public ItemDroneWeapon(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		DroneWeaponProto = DolocConfig.Tables.TbDroneWeapon.GetOrDefault(itemName);
	}

	public override string GetExtraInfo1()
	{
		Color tEXTCOLOR_STD = DolocUiColor.TEXTCOLOR_STD;
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemDroneWeaponValueFormat, DroneWeaponProto.AttackSpeed.ToString("F1").Colored(tEXTCOLOR_STD), DroneWeaponProto.ClipCapacity.ToString().Colored(tEXTCOLOR_STD), DroneWeaponProto.Attack.ToString("F1").Colored(tEXTCOLOR_STD), DroneWeaponProto.CriticalRate.ToString("P0").Colored(tEXTCOLOR_STD));
	}
}
