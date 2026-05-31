using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Fishing;

public sealed class FarmFishInfo : BeanBase
{
	public const int __ID__ = -205698400;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public int EnergyCost { get; private set; }

	public int MetabolismIncrease { get; private set; }

	public int IncubateDuration { get; private set; }

	public int GrowDuration { get; private set; }

	public ItemSpawnEntry ProduceSpawnEntry { get; private set; }

	public int TechPoint { get; private set; }

	public string RoeItem { get; private set; }

	public ItemInfo RoeItem_Ref { get; private set; }

	public FarmFishInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["energy_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyCost = _json["energy_cost"];
		if (!_json["metabolism_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismIncrease = _json["metabolism_increase"];
		if (!_json["incubate_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		IncubateDuration = _json["incubate_duration"];
		if (!_json["grow_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowDuration = _json["grow_duration"];
		if (!_json["produce_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		ProduceSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["produce_spawn_entry"]);
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
		if (!_json["roe_item"].IsString)
		{
			throw new SerializationException();
		}
		RoeItem = _json["roe_item"];
	}

	public FarmFishInfo(string id, int energy_cost, int metabolism_increase, int incubate_duration, int grow_duration, ItemSpawnEntry produce_spawn_entry, int tech_point, string roe_item)
	{
		Id = id;
		EnergyCost = energy_cost;
		MetabolismIncrease = metabolism_increase;
		IncubateDuration = incubate_duration;
		GrowDuration = grow_duration;
		ProduceSpawnEntry = produce_spawn_entry;
		TechPoint = tech_point;
		RoeItem = roe_item;
	}

	public static FarmFishInfo DeserializeFarmFishInfo(JSONNode _json)
	{
		return new FarmFishInfo(_json);
	}

	public override int GetTypeId()
	{
		return -205698400;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		ProduceSpawnEntry?.Resolve(_tables);
		RoeItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(RoeItem);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ProduceSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",EnergyCost:" + EnergyCost + ",MetabolismIncrease:" + MetabolismIncrease + ",IncubateDuration:" + IncubateDuration + ",GrowDuration:" + GrowDuration + ",ProduceSpawnEntry:" + ProduceSpawnEntry?.ToString() + ",TechPoint:" + TechPoint + ",RoeItem:" + RoeItem + ",}";
	}
}
