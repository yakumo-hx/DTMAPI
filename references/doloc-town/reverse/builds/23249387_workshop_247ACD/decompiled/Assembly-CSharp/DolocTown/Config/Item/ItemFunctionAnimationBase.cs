using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionAnimationBase : ItemFunctionBase
{
	public string DialogueNode { get; private set; }

	public string ConfirmMessage { get; private set; }

	public string ConfirmMessage_l10n_key { get; }

	public ItemFunctionAnimationBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["dialogue_node"].IsString)
		{
			throw new SerializationException();
		}
		DialogueNode = _json["dialogue_node"];
		if (!_json["confirm_message"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ConfirmMessage_l10n_key = _json["confirm_message"]["key"];
		if (!_json["confirm_message"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ConfirmMessage = _json["confirm_message"]["text"];
	}

	public ItemFunctionAnimationBase(string dialogue_node, string confirm_message)
	{
		DialogueNode = dialogue_node;
		ConfirmMessage = confirm_message;
	}

	public static ItemFunctionAnimationBase DeserializeItemFunctionAnimationBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"ItemFunctionAnimation" => new ItemFunctionAnimation(_json), 
			"ItemFunctionSleepingBag" => new ItemFunctionSleepingBag(_json), 
			"ItemFunctionRescuePager" => new ItemFunctionRescuePager(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
		ConfirmMessage = translator(ConfirmMessage_l10n_key, ConfirmMessage);
	}

	public override string ToString()
	{
		return "{ DialogueNode:" + DialogueNode + ",ConfirmMessage:" + ConfirmMessage + ",}";
	}
}
