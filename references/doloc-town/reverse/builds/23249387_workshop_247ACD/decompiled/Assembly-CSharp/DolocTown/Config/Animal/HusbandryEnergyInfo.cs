using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Animal;

public sealed class HusbandryEnergyInfo : BeanBase
{
	public const int __ID__ = 1168657862;

	public string Id { get; private set; }

	public int Energy { get; private set; }

	public int Contribution { get; private set; }

	public HusbandryEnergyInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["energy"].IsNumber)
		{
			throw new SerializationException();
		}
		Energy = _json["energy"];
		if (!_json["contribution"].IsNumber)
		{
			throw new SerializationException();
		}
		Contribution = _json["contribution"];
	}

	public HusbandryEnergyInfo(string id, int energy, int contribution)
	{
		Id = id;
		Energy = energy;
		Contribution = contribution;
	}

	public static HusbandryEnergyInfo DeserializeHusbandryEnergyInfo(JSONNode _json)
	{
		return new HusbandryEnergyInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1168657862;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Energy:" + Energy + ",Contribution:" + Contribution + ",}";
	}
}
