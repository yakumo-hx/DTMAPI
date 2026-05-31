using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using DolocTown.Config.Localization;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDroneEngine : Item, IDroneComponentItem
{
	public readonly DroneEngineInfo engineProto;

	public ComponentType ComponentType => ComponentType.Engine;

	public bool IsDroneComponentValid => engineProto != null;

	public Sprite ComponentSprite => engineProto.Sprite.Asset;

	public string ComponentId => base.proto.Id;

	public string SkillId => engineProto.SkillId;

	public Vector2Int ComponentPivot => engineProto.SpritePivot;

	public string ComponentTitle => title;

	public string ComponentDescription => description;

	public ItemDroneEngine(ItemInfo proto, int count)
		: base(proto, count)
	{
		engineProto = DolocConfig.Tables.TbDroneEngine.GetOrDefault(proto.Id);
	}

	[JsonConstructor]
	public ItemDroneEngine(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		engineProto = DolocConfig.Tables.TbDroneEngine.GetOrDefault(itemName);
	}

	public override string GetExtraInfo1()
	{
		List<string> list = new List<string>();
		TbStaticText staticTexts = DolocConfig.StaticTexts;
		Color tEXTCOLOR_STD = DolocUiColor.TEXTCOLOR_STD;
		if (engineProto.MoveSpeedIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemEngineMoveSpeedIncrease, engineProto.MoveSpeedIncrease.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		if (engineProto.PowerCapacityIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemEnginePowerCapacityIncrease, engineProto.PowerCapacityIncrease.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		if (engineProto.PowerRecvIncrease > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemEnginePowerRecvIncrease, engineProto.PowerRecvIncrease.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		return string.Join("\n", list);
	}
}
