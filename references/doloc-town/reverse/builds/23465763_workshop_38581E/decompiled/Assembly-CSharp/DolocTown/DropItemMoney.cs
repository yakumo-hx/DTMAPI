using DolocTown.Config;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DropItemMoney : DropItemBase
{
	[JsonProperty]
	protected int count;

	public override Sprite SceneSprite => LocSprites.UI_ICON_GOLD28X;

	public int Count => count;

	public override string ItemName => "money";

	public override bool Disposable => false;

	public override Sprite SubscriptSprite => null;

	public override bool IsItem => false;

	public DropItemMoney(int count, Vector2 position, bool shouldSendMsg)
		: base(position, shouldSendMsg)
	{
		this.count = count;
	}

	[JsonConstructor]
	protected DropItemMoney(int index, Vector2 position, int count)
		: base(index, position, shouldSendMsg: false)
	{
		this.count = count;
	}

	protected override bool ValidateDeserialization()
	{
		return count > 0;
	}

	public override void OnTouch()
	{
		DolocAPI.RaiseItemObtainTip(ItemName, LocSprites.UI_ICON_GOLD28X, DolocConfig.StaticTexts.ItemMoneyTitle, count);
		DolocAPI.RaiseSpriteFadeUp(base.Renderer.position, SceneSprite);
		DolocAPI.archiveHandle.SetCurrentMoney(DolocAPI.archiveHandle.CurrentMoney + count, out var _);
		InvokeAchievement();
		base.Host.RemoveDropItem(this);
	}

	protected override void InvokeAchievement()
	{
		if (shouldSendMsg)
		{
			DolocAPI.Broadcast(GameEventType.MAKE_MONEY, new GameEventArgsInt(count));
		}
	}
}
