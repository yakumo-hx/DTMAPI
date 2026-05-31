using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MissionNodeInfo : BeanBase
{
	public const int __ID__ = 972531418;

	public string Id { get; private set; }

	public SpriteAsset PreviewImage { get; private set; }

	public string Tip { get; private set; }

	public string Tip_l10n_key { get; }

	public string DescriptionAppend { get; private set; }

	public string DescriptionAppend_l10n_key { get; }

	public MapMissionTip[] MapTips { get; private set; }

	public MissionNodeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["preview_image"].IsObject)
		{
			throw new SerializationException();
		}
		PreviewImage = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["preview_image"]));
		if (!_json["tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Tip_l10n_key = _json["tip"]["key"];
		if (!_json["tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Tip = _json["tip"]["text"];
		if (!_json["description_append"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionAppend_l10n_key = _json["description_append"]["key"];
		if (!_json["description_append"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionAppend = _json["description_append"]["text"];
		JSONNode jSONNode = _json["map_tips"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		MapTips = new MapMissionTip[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			MapMissionTip mapMissionTip = MapMissionTip.DeserializeMapMissionTip(child);
			MapTips[num++] = mapMissionTip;
		}
	}

	public MissionNodeInfo(string id, SpriteAsset preview_image, string tip, string description_append, MapMissionTip[] map_tips)
	{
		Id = id;
		PreviewImage = preview_image;
		Tip = tip;
		DescriptionAppend = description_append;
		MapTips = map_tips;
	}

	public static MissionNodeInfo DeserializeMissionNodeInfo(JSONNode _json)
	{
		return new MissionNodeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 972531418;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MapMissionTip[] mapTips = MapTips;
		for (int i = 0; i < mapTips.Length; i++)
		{
			mapTips[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Tip = translator(Tip_l10n_key, Tip);
		DescriptionAppend = translator(DescriptionAppend_l10n_key, DescriptionAppend);
		MapMissionTip[] mapTips = MapTips;
		for (int i = 0; i < mapTips.Length; i++)
		{
			mapTips[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",PreviewImage:" + PreviewImage?.ToString() + ",Tip:" + Tip + ",DescriptionAppend:" + DescriptionAppend + ",MapTips:" + StringUtil.CollectionToString(MapTips) + ",}";
	}
}
