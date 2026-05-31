using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionRescuePager : ItemFunctionAnimationBase
{
	public const int __ID__ = 1750203146;

	public ItemFunctionRescuePager(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionRescuePager(string dialogue_node, string confirm_message)
		: base(dialogue_node, confirm_message)
	{
	}

	public static ItemFunctionRescuePager DeserializeItemFunctionRescuePager(JSONNode _json)
	{
		return new ItemFunctionRescuePager(_json);
	}

	public override int GetTypeId()
	{
		return 1750203146;
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
