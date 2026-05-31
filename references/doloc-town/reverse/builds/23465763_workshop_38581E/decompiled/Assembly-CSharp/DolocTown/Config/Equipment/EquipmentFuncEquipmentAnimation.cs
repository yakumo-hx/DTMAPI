using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncEquipmentAnimation : EquipmentFuncEquipmentAnimationBase
{
	public const int __ID__ = 75169288;

	public EquipmentFuncEquipmentAnimation(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncEquipmentAnimation(string dialogue_node, string tip_text)
		: base(dialogue_node, tip_text)
	{
	}

	public static EquipmentFuncEquipmentAnimation DeserializeEquipmentFuncEquipmentAnimation(JSONNode _json)
	{
		return new EquipmentFuncEquipmentAnimation(_json);
	}

	public override int GetTypeId()
	{
		return 75169288;
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
		return "{ DialogueNode:" + base.DialogueNode + ",TipText:" + base.TipText + ",}";
	}
}
