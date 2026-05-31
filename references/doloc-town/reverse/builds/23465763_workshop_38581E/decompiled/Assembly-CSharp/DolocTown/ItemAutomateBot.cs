using DolocTown.Config;
using DolocTown.Config.Automate;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemAutomateBot : Item
{
	public AutomateBotInfo AutomateBotInfo => DolocConfig.Tables.TbAutomateBot.GetOrDefault(base.proto.Id);

	public Sprite AutomateBotSprite
	{
		get
		{
			if (!DolocConfig.Tables.TbAutomateBot.DataMap.TryGetValue(base.proto.Id, out var value))
			{
				return null;
			}
			return value.sprite;
		}
	}

	public ItemAutomateBot(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemAutomateBot(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}
}
