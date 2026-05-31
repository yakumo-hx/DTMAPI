using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemFishingRod : Item
{
	private readonly ItemFunctionFishingRod _function;

	public readonly Color lineColor;

	public ItemFishingRod(ItemInfo proto, int count)
		: base(proto, count)
	{
		if (proto != null)
		{
			_function = (ItemFunctionFishingRod)proto.Function;
			lineColor = GetLineColor();
		}
	}

	[JsonConstructor]
	public ItemFishingRod(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
		if (base.proto != null)
		{
			_function = (ItemFunctionFishingRod)base.proto.Function;
			lineColor = GetLineColor();
		}
	}

	private Color GetLineColor()
	{
		if (_function == null || _function.LineColor.IsNullOrEmpty())
		{
			return Color.white;
		}
		if (!ColorUtility.TryParseHtmlString(_function.LineColor, out var color))
		{
			return Color.white;
		}
		return color;
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
		DolocAPI.agent.UseFishRod(this);
	}
}
