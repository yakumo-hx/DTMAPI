using DolocTown.Config.Player;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class AgentEquipmentFunctionHerbPackage : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoHerbPackage _func;

	private readonly ItemHerbPackage _item;

	private bool IsFilled
	{
		get
		{
			if (_item == null)
			{
				return false;
			}
			return _item.isFilled;
		}
	}

	public AgentEquipmentFunctionHerbPackage(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoHerbPackage)skill.Function;
		if (item is ItemHerbPackage itemHerbPackage)
		{
			_item = itemHerbPackage;
		}
	}

	public override bool TryResistFaint(bool shouldRender)
	{
		Debug.Log("尝试对抗晕厥: 草药包");
		if (!IsFilled)
		{
			return false;
		}
		if (shouldRender)
		{
			DolocAPI.RaiseSpriteFadeUp(DolocAPI.AgentPosition, _item.uiSprite);
		}
		DolocAPI.AddSpirit(_func.RecoveryAmount);
		_item.SetFilled(value: false);
		return true;
	}
}
