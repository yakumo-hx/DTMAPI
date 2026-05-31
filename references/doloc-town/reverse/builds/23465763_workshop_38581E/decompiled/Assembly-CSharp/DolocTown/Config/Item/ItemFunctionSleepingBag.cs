using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionSleepingBag : ItemFunctionAnimationBase
{
	public const int __ID__ = 1597650807;

	public ItemFunctionSleepingBag(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionSleepingBag(string dialogue_node, string confirm_message)
		: base(dialogue_node, confirm_message)
	{
	}

	public static ItemFunctionSleepingBag DeserializeItemFunctionSleepingBag(JSONNode _json)
	{
		return new ItemFunctionSleepingBag(_json);
	}

	public override int GetTypeId()
	{
		return 1597650807;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ DialogueNode:" + base.DialogueNode + ",ConfirmMessage:" + base.ConfirmMessage + ",}";
	}
}
