using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Player;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionHatBase : ItemFunctionAgentEquipment
{
	public string HatId { get; private set; }

	public HatInfo HatId_Ref { get; private set; }

	public ItemFunctionHatBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["hat_id"].IsString)
		{
			throw new SerializationException();
		}
		HatId = _json["hat_id"];
	}

	public ItemFunctionHatBase(string hat_id)
	{
		HatId = hat_id;
	}

	public static ItemFunctionHatBase DeserializeItemFunctionHatBase(JSONNode _json)
	{
		string text = _json["$type"];
		if (!(text == "ItemFunctionHat"))
		{
			if (text == "ItemFunctionHatShield")
			{
				return new ItemFunctionHatShield(_json);
			}
			throw new SerializationException();
		}
		return new ItemFunctionHat(_json);
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		HatId_Ref = (_tables["Player.TbHat"] as TbHat).GetOrDefault(HatId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ HatId:" + HatId + ",}";
	}
}
