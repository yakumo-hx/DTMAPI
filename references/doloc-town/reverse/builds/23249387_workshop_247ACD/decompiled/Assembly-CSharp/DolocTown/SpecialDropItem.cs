using DolocTown.Config;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class SpecialDropItem : DropItemBase
{
	[JsonProperty("item")]
	private Item _dropItem;

	public override Sprite SceneSprite => _dropItem?.uiSprite;

	public override Sprite SubscriptSprite
	{
		get
		{
			if (!(_dropItem is IHasSubscript hasSubscript))
			{
				return null;
			}
			return hasSubscript.SubscriptSprite;
		}
	}

	public override bool IsItem => _dropItem != null;

	public override string ItemName => _dropItem?.name ?? string.Empty;

	public override bool Disposable => DolocAPI.IsItemDisposable(_dropItem);

	public Item DropItem => _dropItem;

	public SpecialDropItem(Item item, Vector2 position, bool shouldSendMsg)
		: base(position, shouldSendMsg)
	{
		_dropItem = item;
	}

	[JsonConstructor]
	protected SpecialDropItem(int index, Item item, Vector2 position, bool shouldSendMsg)
		: base(index, position, shouldSendMsg)
	{
		_dropItem = item.CheckValid();
	}

	protected override bool ValidateDeserialization()
	{
		return _dropItem != null;
	}

	public override void OnTouch()
	{
		if (_dropItem == null)
		{
			base.Host.RemoveDropItem(this);
			return;
		}
		int count = _dropItem.count;
		if (DolocAPI.PlaceItem(_dropItem, DolocAPI.userSettings.autoUseBox, useFade: true) == null)
		{
			DolocAPI.RaiseSpriteFadeUp(base.Renderer.position, SceneSprite);
			DolocAPI.RaiseItemObtainTip(_dropItem.name, SceneSprite, _dropItem.title, count);
			base.Host.RemoveDropItem(this);
			InvokeAchievement();
			_dropItem = null;
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
		}
		if (DolocAPI.uiSystem.inventoryQuick.isVisible)
		{
			DolocAPI.QuickSelectCurrentItem();
		}
	}
}
