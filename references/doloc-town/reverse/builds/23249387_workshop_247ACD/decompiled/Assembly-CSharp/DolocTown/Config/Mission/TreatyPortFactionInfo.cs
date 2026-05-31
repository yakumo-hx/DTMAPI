using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.NPC;
using DolocTown.Config.Time;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TreatyPortFactionInfo : BeanBase
{
	public const int __ID__ = 1053222120;

	public string Id { get; private set; }

	public FactionType FactionType { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Contact { get; private set; }

	public NpcInfo Contact_Ref { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset NpcSpriteAsset { get; private set; }

	public string Dialogue { get; private set; }

	public WeekDay? VisitingTime { get; private set; }

	public int SettledReputation { get; private set; }

	public int MedalReputation { get; private set; }

	public SpriteAsset Medal { get; private set; }

	public Color ThemeColor { get; private set; }

	public bool UnlockInDemo { get; private set; }

	public TreatyPortFactionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["faction_type"].IsNumber)
		{
			throw new SerializationException();
		}
		FactionType = (FactionType)_json["faction_type"].AsInt;
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["contact"].IsString)
		{
			throw new SerializationException();
		}
		Contact = _json["contact"];
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["npc_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		NpcSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["npc_sprite_asset"]));
		if (!_json["dialogue"].IsString)
		{
			throw new SerializationException();
		}
		Dialogue = _json["dialogue"];
		JSONNode jSONNode = _json["visiting_time"];
		if (jSONNode.Tag != JSONNodeType.None && jSONNode.Tag != JSONNodeType.NullValue)
		{
			if (!jSONNode.IsNumber)
			{
				throw new SerializationException();
			}
			VisitingTime = (WeekDay)jSONNode.AsInt;
		}
		else
		{
			VisitingTime = null;
		}
		if (!_json["settled_reputation"].IsNumber)
		{
			throw new SerializationException();
		}
		SettledReputation = _json["settled_reputation"];
		if (!_json["medal_reputation"].IsNumber)
		{
			throw new SerializationException();
		}
		MedalReputation = _json["medal_reputation"];
		if (!_json["medal"].IsObject)
		{
			throw new SerializationException();
		}
		Medal = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["medal"]));
		if (!_json["theme_color"].IsObject)
		{
			throw new SerializationException();
		}
		ThemeColor = ExternalTypeUtil.ColorConverter(CfgColor.DeserializeCfgColor(_json["theme_color"]));
		if (!_json["unlock_in_demo"].IsBoolean)
		{
			throw new SerializationException();
		}
		UnlockInDemo = _json["unlock_in_demo"];
	}

	public TreatyPortFactionInfo(string id, FactionType faction_type, string title, string contact, SpriteAsset ui_sprite_asset, SpriteAsset npc_sprite_asset, string dialogue, WeekDay? visiting_time, int settled_reputation, int medal_reputation, SpriteAsset medal, Color theme_color, bool unlock_in_demo)
	{
		Id = id;
		FactionType = faction_type;
		Title = title;
		Contact = contact;
		UiSpriteAsset = ui_sprite_asset;
		NpcSpriteAsset = npc_sprite_asset;
		Dialogue = dialogue;
		VisitingTime = visiting_time;
		SettledReputation = settled_reputation;
		MedalReputation = medal_reputation;
		Medal = medal;
		ThemeColor = theme_color;
		UnlockInDemo = unlock_in_demo;
	}

	public static TreatyPortFactionInfo DeserializeTreatyPortFactionInfo(JSONNode _json)
	{
		return new TreatyPortFactionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1053222120;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Contact_Ref = (_tables["NPC.TbNpc"] as TbNpc).GetOrDefault(Contact);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",FactionType:" + FactionType.ToString() + ",Title:" + Title + ",Contact:" + Contact + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",NpcSpriteAsset:" + NpcSpriteAsset?.ToString() + ",Dialogue:" + Dialogue + ",VisitingTime:" + VisitingTime.ToString() + ",SettledReputation:" + SettledReputation + ",MedalReputation:" + MedalReputation + ",Medal:" + Medal?.ToString() + ",ThemeColor:" + ThemeColor.ToString() + ",UnlockInDemo:" + UnlockInDemo + ",}";
	}
}
