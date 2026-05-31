using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StoreSeasonData : BeanBase
{
	public const int __ID__ = -1024618079;

	public RangeInt RandomSlotsRange { get; private set; }

	public StoreSeasonData(JSONNode _json)
	{
		if (!_json["random_slots_range"].IsObject)
		{
			throw new SerializationException();
		}
		RandomSlotsRange = RangeInt.DeserializeRangeInt(_json["random_slots_range"]);
	}

	public StoreSeasonData(RangeInt random_slots_range)
	{
		RandomSlotsRange = random_slots_range;
	}

	public static StoreSeasonData DeserializeStoreSeasonData(JSONNode _json)
	{
		return new StoreSeasonData(_json);
	}

	public override int GetTypeId()
	{
		return -1024618079;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		RandomSlotsRange?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		RandomSlotsRange?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RandomSlotsRange:" + RandomSlotsRange?.ToString() + ",}";
	}
}
