using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotPerformanceInfo : BeanBase
{
	public const int __ID__ = 1214038405;

	public string Id { get; private set; }

	public int Speed { get; private set; }

	public int PowerCapacity { get; private set; }

	public int InventoryCapacity { get; private set; }

	public AutomateBotPerformanceInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["speed"].IsNumber)
		{
			throw new SerializationException();
		}
		Speed = _json["speed"];
		if (!_json["power_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCapacity = _json["power_capacity"];
		if (!_json["inventory_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		InventoryCapacity = _json["inventory_capacity"];
	}

	public AutomateBotPerformanceInfo(string id, int speed, int power_capacity, int inventory_capacity)
	{
		Id = id;
		Speed = speed;
		PowerCapacity = power_capacity;
		InventoryCapacity = inventory_capacity;
	}

	public static AutomateBotPerformanceInfo DeserializeAutomateBotPerformanceInfo(JSONNode _json)
	{
		return new AutomateBotPerformanceInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1214038405;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Speed:" + Speed + ",PowerCapacity:" + PowerCapacity + ",InventoryCapacity:" + InventoryCapacity + ",}";
	}
}
