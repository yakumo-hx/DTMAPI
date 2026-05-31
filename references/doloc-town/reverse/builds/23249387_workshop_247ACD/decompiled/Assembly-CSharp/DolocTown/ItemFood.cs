using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemFood : Item, IEatable
{
	private ItemFunctionFood _func;

	public Item eatableItem => this;

	public EatingEffectInfo effectProto => _func.EatingEffect_Ref;

	public ItemFood(ItemInfo item, int count)
		: base(item, count)
	{
		_func = item.Function as ItemFunctionFood;
	}

	[JsonConstructor]
	public ItemFood(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		_func = base.proto.Function as ItemFunctionFood;
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		if (((IEatable)this).isValid)
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.ItemConfirmUse, title), Use);
		}
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		((IEatable)this).Eat();
	}
}
