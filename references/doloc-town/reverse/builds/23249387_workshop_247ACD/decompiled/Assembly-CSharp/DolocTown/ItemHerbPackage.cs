using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ItemHerbPackage : ItemPassive
{
	private readonly ItemFunctionHerbPackage _func;

	[JsonProperty]
	[DebugInfo(AllowEdit = true)]
	public bool isFilled { get; private set; }

	public override Sprite uiSprite
	{
		get
		{
			if (isFilled)
			{
				return _func.FullSprite.Asset ?? base.proto.UiSpriteAsset.Asset;
			}
			return base.proto.UiSpriteAsset.Asset;
		}
	}

	public ItemHerbPackage(ItemInfo proto, int count)
		: base(proto, count)
	{
		_func = (ItemFunctionHerbPackage)proto.Function;
		isFilled = true;
	}

	[JsonConstructor]
	protected ItemHerbPackage(string itemName, int itemCount, bool isFilled)
		: base(itemName, itemCount)
	{
		this.isFilled = isFilled;
		if (base.proto != null)
		{
			_func = (ItemFunctionHerbPackage)base.proto.Function;
		}
	}

	public void SetFilled(bool value)
	{
		isFilled = value;
	}

	public override Item Clone(int count)
	{
		Item item = base.Clone(count);
		((ItemHerbPackage)item).isFilled = isFilled;
		return item;
	}
}
