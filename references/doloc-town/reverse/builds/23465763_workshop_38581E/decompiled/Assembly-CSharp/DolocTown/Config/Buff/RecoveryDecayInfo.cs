using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Buff;

public sealed class RecoveryDecayInfo : BeanBase
{
	public const int __ID__ = -1067849064;

	public int TimeSinceAwake { get; private set; }

	public string BuffId { get; private set; }

	public BuffInfo BuffId_Ref { get; private set; }

	public RecoveryDecayInfo(JSONNode _json)
	{
		if (!_json["time_since_awake"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeSinceAwake = _json["time_since_awake"];
		if (!_json["buff_id"].IsString)
		{
			throw new SerializationException();
		}
		BuffId = _json["buff_id"];
	}

	public RecoveryDecayInfo(int time_since_awake, string buff_id)
	{
		TimeSinceAwake = time_since_awake;
		BuffId = buff_id;
	}

	public static RecoveryDecayInfo DeserializeRecoveryDecayInfo(JSONNode _json)
	{
		return new RecoveryDecayInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1067849064;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BuffId_Ref = (_tables["Buff.TbBuff"] as TbBuff).GetOrDefault(BuffId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ TimeSinceAwake:" + TimeSinceAwake + ",BuffId:" + BuffId + ",}";
	}
}
