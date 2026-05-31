using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotFunctionLogistics : AutomateBotFunction
{
	public const int __ID__ = 1285997538;

	public AutomateBotFunctionLogistics(JSONNode _json)
		: base(_json)
	{
	}

	public AutomateBotFunctionLogistics()
	{
	}

	public static AutomateBotFunctionLogistics DeserializeAutomateBotFunctionLogistics(JSONNode _json)
	{
		return new AutomateBotFunctionLogistics(_json);
	}

	public override int GetTypeId()
	{
		return 1285997538;
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
