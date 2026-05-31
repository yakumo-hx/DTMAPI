using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotFunctionGathering : AutomateBotFunction
{
	public const int __ID__ = 1714822614;

	public AutomateBotFunctionGathering(JSONNode _json)
		: base(_json)
	{
	}

	public AutomateBotFunctionGathering()
	{
	}

	public static AutomateBotFunctionGathering DeserializeAutomateBotFunctionGathering(JSONNode _json)
	{
		return new AutomateBotFunctionGathering(_json);
	}

	public override int GetTypeId()
	{
		return 1714822614;
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
		return "{ }";
	}
}
