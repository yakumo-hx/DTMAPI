using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public abstract class ItemBase
{
	[JsonProperty("itemCount")]
	[DebugInfo("数量", AllowEdit = true)]
	public int count;

	[JsonProperty("itemName")]
	public abstract string name { get; }

	public abstract Sprite uiSprite { get; }

	protected ItemBase()
	{
		count = 0;
	}

	protected ItemBase(int count)
	{
		this.count = count;
	}
}
