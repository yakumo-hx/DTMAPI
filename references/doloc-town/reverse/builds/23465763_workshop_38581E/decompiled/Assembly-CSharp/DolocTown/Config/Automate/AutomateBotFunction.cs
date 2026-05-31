using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public abstract class AutomateBotFunction : BeanBase
{
	public AutomateBotFunction(JSONNode _json)
	{
	}

	public AutomateBotFunction()
	{
	}

	public static AutomateBotFunction DeserializeAutomateBotFunction(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"AutomateBotFunctionFarming" => new AutomateBotFunctionFarming(_json), 
			"AutomateBotFunctionLogistics" => new AutomateBotFunctionLogistics(_json), 
			"AutomateBotFunctionGathering" => new AutomateBotFunctionGathering(_json), 
			"AutomateBotFunctionFilling" => new AutomateBotFunctionFilling(_json), 
			"AutomateBotFunctionProcessing" => new AutomateBotFunctionProcessing(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
