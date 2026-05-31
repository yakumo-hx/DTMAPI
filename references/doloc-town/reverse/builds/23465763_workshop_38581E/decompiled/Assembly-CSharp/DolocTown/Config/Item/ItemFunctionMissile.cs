using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionMissile : ItemFunctionBase
{
	public const int __ID__ = -1691083968;

	public string Name { get; private set; }

	public string InvokeMethod { get; private set; }

	public int Damage { get; private set; }

	public float CriticalRate { get; private set; }

	public ItemFunctionMissile(JSONNode _json)
		: base(_json)
	{
		if (!_json["name"].IsString)
		{
			throw new SerializationException();
		}
		Name = _json["name"];
		if (!_json["invoke_method"].IsString)
		{
			throw new SerializationException();
		}
		InvokeMethod = _json["invoke_method"];
		if (!_json["damage"].IsNumber)
		{
			throw new SerializationException();
		}
		Damage = _json["damage"];
		if (!_json["critical_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		CriticalRate = _json["critical_rate"];
	}

	public ItemFunctionMissile(string name, string invoke_method, int damage, float critical_rate)
	{
		Name = name;
		InvokeMethod = invoke_method;
		Damage = damage;
		CriticalRate = critical_rate;
	}

	public static ItemFunctionMissile DeserializeItemFunctionMissile(JSONNode _json)
	{
		return new ItemFunctionMissile(_json);
	}

	public override int GetTypeId()
	{
		return -1691083968;
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
		return "{ Name:" + Name + ",InvokeMethod:" + InvokeMethod + ",Damage:" + Damage + ",CriticalRate:" + CriticalRate + ",}";
	}
}
