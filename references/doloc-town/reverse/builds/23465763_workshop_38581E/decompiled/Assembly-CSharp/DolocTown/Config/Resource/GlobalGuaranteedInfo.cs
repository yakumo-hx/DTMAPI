using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class GlobalGuaranteedInfo : BeanBase
{
	public const int __ID__ = -1167391219;

	public GuaranteedType Type { get; private set; }

	public string SpawnId { get; private set; }

	public ItemInfo SpawnId_Ref { get; private set; }

	public int GlobalLimit { get; private set; }

	public bool DefaultLocked { get; private set; }

	public bool Viewable { get; private set; }

	public int ActiveCount { get; private set; }

	public bool ShouldConsumeGuarantee { get; private set; }

	public int GuaranteeThreshold { get; private set; }

	public GlobalGuaranteedInfo(JSONNode _json)
	{
		if (!_json["type"].IsNumber)
		{
			throw new SerializationException();
		}
		Type = (GuaranteedType)_json["type"].AsInt;
		if (!_json["spawn_id"].IsString)
		{
			throw new SerializationException();
		}
		SpawnId = _json["spawn_id"];
		if (!_json["global_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		GlobalLimit = _json["global_limit"];
		if (!_json["default_locked"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultLocked = _json["default_locked"];
		if (!_json["viewable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Viewable = _json["viewable"];
		if (!_json["active_count"].IsNumber)
		{
			throw new SerializationException();
		}
		ActiveCount = _json["active_count"];
		if (!_json["should_consume_guarantee"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShouldConsumeGuarantee = _json["should_consume_guarantee"];
		if (!_json["guarantee_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		GuaranteeThreshold = _json["guarantee_threshold"];
	}

	public GlobalGuaranteedInfo(GuaranteedType type, string spawn_id, int global_limit, bool default_locked, bool viewable, int active_count, bool should_consume_guarantee, int guarantee_threshold)
	{
		Type = type;
		SpawnId = spawn_id;
		GlobalLimit = global_limit;
		DefaultLocked = default_locked;
		Viewable = viewable;
		ActiveCount = active_count;
		ShouldConsumeGuarantee = should_consume_guarantee;
		GuaranteeThreshold = guarantee_threshold;
	}

	public static GlobalGuaranteedInfo DeserializeGlobalGuaranteedInfo(JSONNode _json)
	{
		return new GlobalGuaranteedInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1167391219;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpawnId_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(SpawnId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Type:" + Type.ToString() + ",SpawnId:" + SpawnId + ",GlobalLimit:" + GlobalLimit + ",DefaultLocked:" + DefaultLocked + ",Viewable:" + Viewable + ",ActiveCount:" + ActiveCount + ",ShouldConsumeGuarantee:" + ShouldConsumeGuarantee + ",GuaranteeThreshold:" + GuaranteeThreshold + ",}";
	}
}
