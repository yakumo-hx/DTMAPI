using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionAnimation : ItemFunctionAnimationBase
{
	public const int __ID__ = -1759340770;

	public ItemFunctionAnimation(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionAnimation(string dialogue_node, string confirm_message)
		: base(dialogue_node, confirm_message)
	{
	}

	public static ItemFunctionAnimation DeserializeItemFunctionAnimation(JSONNode _json)
	{
		return new ItemFunctionAnimation(_json);
	}

	public override int GetTypeId()
	{
		return -1759340770;
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
