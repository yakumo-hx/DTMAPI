using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotFunctionProcessing : AutomateBotFunction
{
	public const int __ID__ = -1076134108;

	public AutomateBotFunctionProcessing(JSONNode _json)
		: base(_json)
	{
	}

	public AutomateBotFunctionProcessing()
	{
	}

	public static AutomateBotFunctionProcessing DeserializeAutomateBotFunctionProcessing(JSONNode _json)
	{
		return new AutomateBotFunctionProcessing(_json);
	}

	public override int GetTypeId()
	{
		return -1076134108;
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
