using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DropItem : DropItemBase
{
	[JsonProperty]
	private string itemName;

	public override Sprite SceneSprite => DolocAPI.GetItemSprite(itemName);

	public override Sprite SubscriptSprite => null;

	public override bool IsItem => true;

	public override string ItemName => itemName;

	public override bool Disposable
	{
		get
		{
			if (!DolocAPI.DisableDisposeItem && DolocAPI.QueryItemProto(itemName, out var proto))
			{
				return proto.Disposable;
			}
			return false;
		}
	}

	public DropItem(string itemName, Vector2 position, bool shouldSendMsg)
		: base(position, shouldSendMsg)
	{
		this.itemName = itemName;
	}

	[JsonConstructor]
	protected DropItem(int index, Vector2 position, string itemName, bool shouldSendMsg)
		: base(index, position, shouldSendMsg)
	{
		this.itemName = itemName;
	}

	protected override bool ValidateDeserialization()
	{
		ItemInfo proto;
		return DolocAPI.QueryItemProto(itemName, out proto);
	}

	public override void OnTouch()
	{
		if (DolocAPI.PlaceItem(itemName, 1, DolocAPI.userSettings.autoUseBox, useFade: true) == null)
		{
			string itemTitle = DolocAPI.GetItemTitle(itemName);
			DolocAPI.RaiseSpriteFadeUp(base.Renderer.position, SceneSprite);
			DolocAPI.RaiseItemObtainTip(itemName, SceneSprite, itemTitle);
			InvokeAchievement();
			base.Host.RemoveDropItem(this);
		}
		else
		{
			base.Renderer.SetShieldCollector(shield: true);
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
		}
		if (DolocAPI.uiSystem.inventoryQuick.isVisible)
		{
			DolocAPI.QuickSelectCurrentItem();
		}
	}
}
