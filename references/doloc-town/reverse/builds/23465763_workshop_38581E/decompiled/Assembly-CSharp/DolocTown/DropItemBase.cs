using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public abstract class DropItemBase : WorldContent
{
	[JsonProperty]
	protected bool shouldSendMsg;

	public IDropItemHost Host { get; set; }

	public DropItemRenderer Renderer => (DropItemRenderer)base.BaseRenderer;

	public abstract Sprite SubscriptSprite { get; }

	public abstract bool IsItem { get; }

	public abstract string ItemName { get; }

	public abstract bool Disposable { get; }

	public bool shieldCollector { get; private set; }

	public bool ShouldSendMsg => shouldSendMsg;

	public bool IsRemoved => base.index < 0;

	protected DropItemBase(Vector2 position, bool shouldSendMsg)
		: base(position)
	{
		this.shouldSendMsg = shouldSendMsg;
	}

	[JsonConstructor]
	protected DropItemBase(int index, Vector2 position, bool shouldSendMsg)
		: base(index, position)
	{
		this.shouldSendMsg = shouldSendMsg;
	}

	protected virtual void InvokeAchievement()
	{
		if (shouldSendMsg && DolocAPI.QueryItemProto(ItemName, out var proto))
		{
			DolocAPI.BroadcastString(GameEventType.OBTAIN_ITEM, proto.Id);
			string text = proto.Function.GetType().Name.Replace("Function", "");
			DolocAPI.BroadcastString(GameEventType.OBTAIN_FUNCTION_ITEM, text.ToLower());
		}
	}

	public void SetShieldCollector(bool shield = true)
	{
		shieldCollector = shield;
	}
}
