using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Archives;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.NPC;

public sealed class NpcDocumentInfo : BeanBase
{
	public readonly Dictionary<string, DocumentNodeInfo> DocumentInfos_Index = new Dictionary<string, DocumentNodeInfo>();

	public readonly Dictionary<string, DocumentMissionTip> MissionTips_Index = new Dictionary<string, DocumentMissionTip>();

	public const int __ID__ = -592413539;

	public string Id { get; private set; }

	public NpcInfo Id_Ref { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset SceneSpriteAsset { get; private set; }

	public string Address { get; private set; }

	public string Address_l10n_key { get; }

	public DocumentNodeInfo[] DocumentInfos { get; private set; }

	public DocumentMissionTip[] MissionTips { get; private set; }

	public bool Display { get; private set; }

	public NpcDocumentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["scene_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SceneSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_sprite_asset"]));
		if (!_json["address"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Address_l10n_key = _json["address"]["key"];
		if (!_json["address"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Address = _json["address"]["text"];
		JSONNode jSONNode = _json["document_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		DocumentInfos = new DocumentNodeInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			DocumentNodeInfo documentNodeInfo = DocumentNodeInfo.DeserializeDocumentNodeInfo(child);
			DocumentInfos[num++] = documentNodeInfo;
		}
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo2 in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo2.Id, documentNodeInfo2);
		}
		JSONNode jSONNode2 = _json["mission_tips"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		MissionTips = new DocumentMissionTip[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			DocumentMissionTip documentMissionTip = DocumentMissionTip.DeserializeDocumentMissionTip(child2);
			MissionTips[num2++] = documentMissionTip;
		}
		DocumentMissionTip[] missionTips = MissionTips;
		foreach (DocumentMissionTip documentMissionTip2 in missionTips)
		{
			MissionTips_Index.Add(documentMissionTip2.Id, documentMissionTip2);
		}
		if (!_json["display"].IsBoolean)
		{
			throw new SerializationException();
		}
		Display = _json["display"];
	}

	public NpcDocumentInfo(string id, SpriteAsset ui_sprite_asset, SpriteAsset scene_sprite_asset, string address, DocumentNodeInfo[] document_infos, DocumentMissionTip[] mission_tips, bool display)
	{
		Id = id;
		UiSpriteAsset = ui_sprite_asset;
		SceneSpriteAsset = scene_sprite_asset;
		Address = address;
		DocumentInfos = document_infos;
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo.Id, documentNodeInfo);
		}
		MissionTips = mission_tips;
		DocumentMissionTip[] missionTips = MissionTips;
		foreach (DocumentMissionTip documentMissionTip in missionTips)
		{
			MissionTips_Index.Add(documentMissionTip.Id, documentMissionTip);
		}
		Display = display;
	}

	public static NpcDocumentInfo DeserializeNpcDocumentInfo(JSONNode _json)
	{
		return new NpcDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return -592413539;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["NPC.TbNpc"] as TbNpc).GetOrDefault(Id);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.Resolve(_tables);
		}
		DocumentMissionTip[] missionTips = MissionTips;
		for (int i = 0; i < missionTips.Length; i++)
		{
			missionTips[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Address = translator(Address_l10n_key, Address);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.TranslateText(translator);
		}
		DocumentMissionTip[] missionTips = MissionTips;
		for (int i = 0; i < missionTips.Length; i++)
		{
			missionTips[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",SceneSpriteAsset:" + SceneSpriteAsset?.ToString() + ",Address:" + Address + ",DocumentInfos:" + StringUtil.CollectionToString(DocumentInfos) + ",MissionTips:" + StringUtil.CollectionToString(MissionTips) + ",Display:" + Display + ",}";
	}
}
