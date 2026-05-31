using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncEquipmentAnimationBase : EquipmentFuncEquipment
{
	public string DialogueNode { get; private set; }

	public string TipText { get; private set; }

	public string TipText_l10n_key { get; }

	public EquipmentFuncEquipmentAnimationBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["dialogue_node"].IsString)
		{
			throw new SerializationException();
		}
		DialogueNode = _json["dialogue_node"];
		if (!_json["tip_text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TipText_l10n_key = _json["tip_text"]["key"];
		if (!_json["tip_text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TipText = _json["tip_text"]["text"];
	}

	public EquipmentFuncEquipmentAnimationBase(string dialogue_node, string tip_text)
	{
		DialogueNode = dialogue_node;
		TipText = tip_text;
	}

	public static EquipmentFuncEquipmentAnimationBase DeserializeEquipmentFuncEquipmentAnimationBase(JSONNode _json)
	{
		if ((string)_json["$type"] == "EquipmentFuncEquipmentAnimation")
		{
			return new EquipmentFuncEquipmentAnimation(_json);
		}
		throw new SerializationException();
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
		TipText = translator(TipText_l10n_key, TipText);
	}

	public override string ToString()
	{
		return "{ DialogueNode:" + DialogueNode + ",TipText:" + TipText + ",}";
	}
}
