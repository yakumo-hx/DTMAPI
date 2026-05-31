using System.Text;
using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemHat : Item
{
	public ItemFunctionHatBase function => base.proto.Function as ItemFunctionHatBase;

	public ItemHat(ItemInfo proto, int count = 1)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	public ItemHat(string itemName, int count)
		: base(itemName, count)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	protected virtual void Use()
	{
		if (DolocAPI.EquipHat(name, out var oldHat))
		{
			CostSelf();
			DolocAPI.PlaceItem(oldHat);
			Debug.Log("道具<" + name + ">装备成功");
		}
		else
		{
			Debug.LogError("道具<" + name + ">装备失败");
		}
	}

	public override string GetExtraInfo1()
	{
		if (function.HatId_Ref == null)
		{
			return base.GetExtraInfo1();
		}
		string text = function?.HatId_Ref.Skill_Ref?.GearEntry ?? string.Empty;
		int? num = function?.HatId_Ref.Defense;
		StringBuilder stringBuilder = new StringBuilder();
		if (!text.IsNullOrEmpty())
		{
			stringBuilder.AppendLine(string.Format(DolocConfig.StaticTexts.EquipmentSkillPrefix, text));
		}
		if (num > 0)
		{
			stringBuilder.AppendLine(string.Format(DolocConfig.StaticTexts.EquipmentDefensePrefix, num));
		}
		if (stringBuilder.Length == 0)
		{
			return string.Empty;
		}
		return stringBuilder.ToString().Trim('\n');
	}
}
