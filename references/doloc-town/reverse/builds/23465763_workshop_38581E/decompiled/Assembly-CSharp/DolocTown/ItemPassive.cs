using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemPassive : Item
{
	public ItemPassive(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemPassive(string itemName, int itemCount)
		: base(itemName, itemCount)
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

	private void Use()
	{
		if (DolocAPI.EquipPassiveItem(this, out var oldItem))
		{
			CostSelf();
			DolocAPI.PlaceItem(oldItem);
		}
	}

	public override string GetExtraInfo1()
	{
		return string.Format(DolocConfig.StaticTexts.EquipmentSkillPrefix, ((ItemFunctionPassiveBase)base.proto.Function).Skill_Ref?.GearEntry ?? string.Empty);
	}
}
