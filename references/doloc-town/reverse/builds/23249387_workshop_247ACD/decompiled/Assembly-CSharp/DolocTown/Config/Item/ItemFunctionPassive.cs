using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionPassive : ItemFunctionPassiveBase
{
	public const int __ID__ = 742394177;

	public ItemFunctionPassive(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionPassive(string skill)
		: base(skill)
	{
	}

	public static ItemFunctionPassive DeserializeItemFunctionPassive(JSONNode _json)
	{
		return new ItemFunctionPassive(_json);
	}

	public override int GetTypeId()
	{
		return 742394177;
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
		return "{ Skill:" + base.Skill + ",}";
	}
}
