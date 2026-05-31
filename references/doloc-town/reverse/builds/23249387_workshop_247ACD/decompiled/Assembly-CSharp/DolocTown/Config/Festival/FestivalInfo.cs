using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Room;
using SimpleJSON;

namespace DolocTown.Config.Festival;

public sealed class FestivalInfo : BeanBase
{
	public const int __ID__ = -128168196;

	public string Id { get; private set; }

	public bool HideDrone { get; private set; }

	public bool DisableNpcActing { get; private set; }

	public Dictionary<string, NpcFestivalInfo> NpcInfos { get; private set; }

	public bool UseGateWhitelist { get; private set; }

	public string DisableGateTip { get; private set; }

	public string DisableGateTip_l10n_key { get; }

	public string[] GateWhitelist { get; private set; }

	public PortalInfo[] GateWhitelist_Ref { get; private set; }

	public FestivalInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["hide_drone"].IsBoolean)
		{
			throw new SerializationException();
		}
		HideDrone = _json["hide_drone"];
		if (!_json["disable_npc_acting"].IsBoolean)
		{
			throw new SerializationException();
		}
		DisableNpcActing = _json["disable_npc_acting"];
		JSONNode jSONNode = _json["npc_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		NpcInfos = new Dictionary<string, NpcFestivalInfo>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child[0].IsString)
			{
				throw new SerializationException();
			}
			string key = child[0];
			if (!child[1].IsObject)
			{
				throw new SerializationException();
			}
			NpcFestivalInfo value = NpcFestivalInfo.DeserializeNpcFestivalInfo(child[1]);
			NpcInfos.Add(key, value);
		}
		if (!_json["use_gate_whitelist"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseGateWhitelist = _json["use_gate_whitelist"];
		if (!_json["disable_gate_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DisableGateTip_l10n_key = _json["disable_gate_tip"]["key"];
		if (!_json["disable_gate_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DisableGateTip = _json["disable_gate_tip"]["text"];
		JSONNode jSONNode2 = _json["gate_whitelist"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode2.Count;
		GateWhitelist = new string[count];
		int num = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string text = child2;
			GateWhitelist[num++] = text;
		}
	}

	public FestivalInfo(string id, bool hide_drone, bool disable_npc_acting, Dictionary<string, NpcFestivalInfo> npc_infos, bool use_gate_whitelist, string disable_gate_tip, string[] gate_whitelist)
	{
		Id = id;
		HideDrone = hide_drone;
		DisableNpcActing = disable_npc_acting;
		NpcInfos = npc_infos;
		UseGateWhitelist = use_gate_whitelist;
		DisableGateTip = disable_gate_tip;
		GateWhitelist = gate_whitelist;
	}

	public static FestivalInfo DeserializeFestivalInfo(JSONNode _json)
	{
		return new FestivalInfo(_json);
	}

	public override int GetTypeId()
	{
		return -128168196;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (NpcFestivalInfo value in NpcInfos.Values)
		{
			value?.Resolve(_tables);
		}
		int num = GateWhitelist.Length;
		TbPortal tbPortal = (TbPortal)_tables["Room.TbPortal"];
		GateWhitelist_Ref = new PortalInfo[num];
		for (int i = 0; i < num; i++)
		{
			GateWhitelist_Ref[i] = tbPortal.GetOrDefault(GateWhitelist[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (NpcFestivalInfo value in NpcInfos.Values)
		{
			value?.TranslateText(translator);
		}
		DisableGateTip = translator(DisableGateTip_l10n_key, DisableGateTip);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",HideDrone:" + HideDrone + ",DisableNpcActing:" + DisableNpcActing + ",NpcInfos:" + StringUtil.CollectionToString(NpcInfos) + ",UseGateWhitelist:" + UseGateWhitelist + ",DisableGateTip:" + DisableGateTip + ",GateWhitelist:" + StringUtil.CollectionToString(GateWhitelist) + ",}";
	}
}
