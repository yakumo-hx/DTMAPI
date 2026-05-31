using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public abstract class VegetationFuncBase : BeanBase
{
	public VegetationFuncBase(JSONNode _json)
	{
	}

	public VegetationFuncBase()
	{
	}

	public static VegetationFuncBase DeserializeVegetationFuncBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"VegetationFuncBerryThicket" => new VegetationFuncBerryThicket(_json), 
			"VegetationFuncCrop" => new VegetationFuncCrop(_json), 
			"VegetationFuncGrowLuminous" => new VegetationFuncGrowLuminous(_json), 
			"VegetationFuncGrow" => new VegetationFuncGrow(_json), 
			"VegetationFuncDandelion" => new VegetationFuncDandelion(_json), 
			"VegetationFuncDecorator" => new VegetationFuncDecorator(_json), 
			"VegetationFuncLuminous" => new VegetationFuncLuminous(_json), 
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
