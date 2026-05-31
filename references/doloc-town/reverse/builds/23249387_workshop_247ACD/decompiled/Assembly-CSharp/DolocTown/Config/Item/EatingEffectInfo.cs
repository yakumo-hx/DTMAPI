using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class EatingEffectInfo : BeanBase
{
	public const int __ID__ = -102027132;

	public string Id { get; private set; }

	public string SoundEvent { get; private set; }

	public CountItem[] OutputItems { get; private set; }

	public FoodEffect[] Effects { get; private set; }

	public EatingEffectInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sound_event"].IsString)
		{
			throw new SerializationException();
		}
		SoundEvent = _json["sound_event"];
		JSONNode jSONNode = _json["output_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		OutputItems = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			OutputItems[num++] = countItem;
		}
		JSONNode jSONNode2 = _json["effects"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		Effects = new FoodEffect[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			FoodEffect foodEffect = FoodEffect.DeserializeFoodEffect(child2);
			Effects[num2++] = foodEffect;
		}
	}

	public EatingEffectInfo(string id, string sound_event, CountItem[] output_items, FoodEffect[] effects)
	{
		Id = id;
		SoundEvent = sound_event;
		OutputItems = output_items;
		Effects = effects;
	}

	public static EatingEffectInfo DeserializeEatingEffectInfo(JSONNode _json)
	{
		return new EatingEffectInfo(_json);
	}

	public override int GetTypeId()
	{
		return -102027132;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		FoodEffect[] effects = Effects;
		for (int i = 0; i < effects.Length; i++)
		{
			effects[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		FoodEffect[] effects = Effects;
		for (int i = 0; i < effects.Length; i++)
		{
			effects[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SoundEvent:" + SoundEvent + ",OutputItems:" + StringUtil.CollectionToString(OutputItems) + ",Effects:" + StringUtil.CollectionToString(Effects) + ",}";
	}
}
