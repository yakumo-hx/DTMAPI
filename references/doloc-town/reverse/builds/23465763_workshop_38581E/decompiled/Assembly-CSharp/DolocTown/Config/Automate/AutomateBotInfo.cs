using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotInfo : BeanBase
{
	public const int __ID__ = 1184367239;

	private static Dictionary<Type, Type> _paramTypesCache;

	private static Dictionary<Type, Type> _controllerTypesCache;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public string Performace { get; private set; }

	public AutomateBotPerformanceInfo Performace_Ref { get; private set; }

	public string Appearance { get; private set; }

	public AutomateBotAppearanceInfo Appearance_Ref { get; private set; }

	public AutomateBotFunction Function { get; private set; }

	public int speed => Performace_Ref.Speed;

	public int powerCapacity => Performace_Ref.PowerCapacity;

	public int inventorySize => Mathf.Max(1, Performace_Ref.InventoryCapacity);

	public int powerSlot => Appearance_Ref.PowerSlot;

	public Sprite sprite => Appearance_Ref.Sprite.Asset;

	public RuntimeAnimatorController animator => Appearance_Ref.Animator.Asset;

	public bool HasAnimator => !Appearance_Ref.Animator.AssetUrl.IsNullOrEmpty();

	public Type ParamType
	{
		get
		{
			if (_paramTypesCache == null)
			{
				_paramTypesCache = BuildParamTypeCache();
			}
			if (!_paramTypesCache.TryGetValue(Function.GetType(), out var value))
			{
				return typeof(AutomateParamEmpty);
			}
			return value;
		}
	}

	public Type ControllerType
	{
		get
		{
			if (_controllerTypesCache == null)
			{
				_controllerTypesCache = BuildControllerTypeCache();
			}
			if (!_controllerTypesCache.TryGetValue(Function.GetType(), out var value))
			{
				return typeof(AutomateBotDecisionMakerEmpty);
			}
			return value;
		}
	}

	public AutomateBotInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["performace"].IsString)
		{
			throw new SerializationException();
		}
		Performace = _json["performace"];
		if (!_json["appearance"].IsString)
		{
			throw new SerializationException();
		}
		Appearance = _json["appearance"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = AutomateBotFunction.DeserializeAutomateBotFunction(_json["function"]);
	}

	public AutomateBotInfo(string id, string performace, string appearance, AutomateBotFunction function)
	{
		Id = id;
		Performace = performace;
		Appearance = appearance;
		Function = function;
	}

	public static AutomateBotInfo DeserializeAutomateBotInfo(JSONNode _json)
	{
		return new AutomateBotInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1184367239;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		Performace_Ref = (_tables["Automate.TbAutomateBotPerformance"] as TbAutomateBotPerformance).GetOrDefault(Performace);
		Appearance_Ref = (_tables["Automate.TbAutomateBotAppearance"] as TbAutomateBotAppearance).GetOrDefault(Appearance);
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Performace:" + Performace + ",Appearance:" + Appearance + ",Function:" + Function?.ToString() + ",}";
	}

	private static Dictionary<Type, Type> BuildParamTypeCache()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(AutomateParam).GetSubTypes("DolocTown");
		foreach (Type type in subTypes)
		{
			string key = type.Name.Replace("AutomateParam", string.Empty);
			dictionary.Add(key, type);
		}
		Dictionary<Type, Type> dictionary2 = new Dictionary<Type, Type>();
		subTypes = typeof(AutomateBotFunction).GetSubTypes("DolocTown");
		foreach (Type type2 in subTypes)
		{
			string key2 = type2.Name.Replace("AutomateBotFunction", string.Empty);
			if (dictionary.TryGetValue(key2, out var value))
			{
				dictionary2.Add(type2, value);
			}
		}
		return dictionary2;
	}

	private static Dictionary<Type, Type> BuildControllerTypeCache()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(AutomateBotDecisionMaker).GetSubTypes("DolocTown");
		foreach (Type type in subTypes)
		{
			string key = type.Name.Replace("AutomateBotDecisionMaker", string.Empty);
			dictionary.Add(key, type);
		}
		Dictionary<Type, Type> dictionary2 = new Dictionary<Type, Type>();
		subTypes = typeof(AutomateBotFunction).GetSubTypes("DolocTown");
		foreach (Type type2 in subTypes)
		{
			string key2 = type2.Name.Replace("AutomateBotFunction", string.Empty);
			if (dictionary.TryGetValue(key2, out var value))
			{
				dictionary2.Add(type2, value);
			}
		}
		return dictionary2;
	}
}
