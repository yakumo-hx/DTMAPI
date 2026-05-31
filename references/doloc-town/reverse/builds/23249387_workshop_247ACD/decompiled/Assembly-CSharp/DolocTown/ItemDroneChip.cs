using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using DolocTown.Config.Localization;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDroneChip : Item, IDroneComponentItem
{
	public readonly DroneChipInfo chipProto;

	public override bool invalid
	{
		get
		{
			if (!base.invalid)
			{
				return chipProto == null;
			}
			return true;
		}
	}

	public ComponentType ComponentType => ComponentType.Chip;

	public bool IsDroneComponentValid => chipProto != null;

	public Sprite ComponentSprite => null;

	public string SkillId => null;

	public string ComponentId => name;

	public Vector2Int ComponentPivot => Vector2Int.zero;

	public string ComponentTitle => title;

	public string ComponentDescription => description;

	public ItemDroneChip(ItemInfo item, int count)
		: base(item, count)
	{
		chipProto = DolocConfig.Tables.TbDroneChip.GetOrDefault(item.Id);
	}

	[JsonConstructor]
	public ItemDroneChip(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		chipProto = DolocConfig.Tables.TbDroneChip.GetOrDefault(itemName);
	}

	public override string GetExtraInfo1()
	{
		List<string> list = new List<string>();
		TbStaticText staticTexts = DolocConfig.StaticTexts;
		Color tEXTCOLOR_STD = DolocUiColor.TEXTCOLOR_STD;
		if (chipProto.AttackIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipAttackIncrease, chipProto.AttackIncrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.AttackIncreaseFixed > 0)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipAttackIncreaseFixed, chipProto.AttackIncreaseFixed.ToString().Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.CriticalRateIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipCriticalRateIncrease, chipProto.CriticalRateIncrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.AttackSpeedIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipAttackSpeedIncrease, chipProto.AttackSpeedIncrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.AccuracyIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipAccuracyIncrease, chipProto.AccuracyIncrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.PowerCostDecrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipPowerCostDecrease, chipProto.PowerCostDecrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.AttackDistanceIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipAttackDistanceIncrease, chipProto.AttackDistanceIncrease.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.MoveSpeedIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipMoveSpeedIncrease, chipProto.MoveSpeedIncrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.ClipCapacityAddition > 0)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipClipCapacityAddition, chipProto.ClipCapacityAddition.ToString().Colored(tEXTCOLOR_STD)));
		}
		if (chipProto.ReloadDurationDecrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemChipReloadDurationDecrease, chipProto.ReloadDurationDecrease.ToString("P0").Colored(tEXTCOLOR_STD)));
		}
		return string.Join("\n", list);
	}
}
