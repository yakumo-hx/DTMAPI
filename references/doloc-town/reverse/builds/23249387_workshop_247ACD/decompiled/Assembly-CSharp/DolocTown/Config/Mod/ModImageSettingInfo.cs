using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mod;

public sealed class ModImageSettingInfo : BeanBase
{
	public const int __ID__ = 1334723769;

	public string Id { get; private set; }

	public bool UseBottomCenterAsPivot { get; private set; }

	public Vector2 DefaultPivot { get; private set; }

	public ModImageSettingInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["use_bottom_center_as_pivot"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseBottomCenterAsPivot = _json["use_bottom_center_as_pivot"];
		if (!_json["default_pivot"].IsObject)
		{
			throw new SerializationException();
		}
		DefaultPivot = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["default_pivot"]));
	}

	public ModImageSettingInfo(string id, bool use_bottom_center_as_pivot, Vector2 default_pivot)
	{
		Id = id;
		UseBottomCenterAsPivot = use_bottom_center_as_pivot;
		DefaultPivot = default_pivot;
	}

	public static ModImageSettingInfo DeserializeModImageSettingInfo(JSONNode _json)
	{
		return new ModImageSettingInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1334723769;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",UseBottomCenterAsPivot:" + UseBottomCenterAsPivot + ",DefaultPivot:" + DefaultPivot.ToString() + ",}";
	}
}
