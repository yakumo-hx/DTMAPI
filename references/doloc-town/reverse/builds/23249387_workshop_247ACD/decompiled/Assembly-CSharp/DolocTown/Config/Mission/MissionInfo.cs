using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.NPC;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MissionInfo : BeanBase
{
	public readonly Dictionary<string, MissionNodeInfo> NodeInfos_Index = new Dictionary<string, MissionNodeInfo>();

	public const int __ID__ = 1797483192;

	public string Id { get; private set; }

	public string MissionType { get; private set; }

	public MissionTypeInfo MissionType_Ref { get; private set; }

	public int OrderInType { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public string Sender { get; private set; }

	public NpcInfo Sender_Ref { get; private set; }

	public MissionNodeInfo[] NodeInfos { get; private set; }

	public string FirstTip
	{
		get
		{
			if (NodeInfos.Length == 0)
			{
				return string.Empty;
			}
			return NodeInfos.First().Tip;
		}
	}

	public MissionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["mission_type"].IsString)
		{
			throw new SerializationException();
		}
		MissionType = _json["mission_type"];
		if (!_json["order_in_type"].IsNumber)
		{
			throw new SerializationException();
		}
		OrderInType = _json["order_in_type"];
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["sender"].IsString)
		{
			throw new SerializationException();
		}
		Sender = _json["sender"];
		JSONNode jSONNode = _json["node_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		NodeInfos = new MissionNodeInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			MissionNodeInfo missionNodeInfo = MissionNodeInfo.DeserializeMissionNodeInfo(child);
			NodeInfos[num++] = missionNodeInfo;
		}
		MissionNodeInfo[] nodeInfos = NodeInfos;
		foreach (MissionNodeInfo missionNodeInfo2 in nodeInfos)
		{
			NodeInfos_Index.Add(missionNodeInfo2.Id, missionNodeInfo2);
		}
	}

	public MissionInfo(string id, string mission_type, int order_in_type, string title, string description, string sender, MissionNodeInfo[] node_infos)
	{
		Id = id;
		MissionType = mission_type;
		OrderInType = order_in_type;
		Title = title;
		Description = description;
		Sender = sender;
		NodeInfos = node_infos;
		MissionNodeInfo[] nodeInfos = NodeInfos;
		foreach (MissionNodeInfo missionNodeInfo in nodeInfos)
		{
			NodeInfos_Index.Add(missionNodeInfo.Id, missionNodeInfo);
		}
	}

	public static MissionInfo DeserializeMissionInfo(JSONNode _json)
	{
		return new MissionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1797483192;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MissionType_Ref = (_tables["Mission.TbMissionType"] as TbMissionType).GetOrDefault(MissionType);
		Sender_Ref = (_tables["NPC.TbNpc"] as TbNpc).GetOrDefault(Sender);
		MissionNodeInfo[] nodeInfos = NodeInfos;
		for (int i = 0; i < nodeInfos.Length; i++)
		{
			nodeInfos[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		MissionNodeInfo[] nodeInfos = NodeInfos;
		for (int i = 0; i < nodeInfos.Length; i++)
		{
			nodeInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MissionType:" + MissionType + ",OrderInType:" + OrderInType + ",Title:" + Title + ",Description:" + Description + ",Sender:" + Sender + ",NodeInfos:" + StringUtil.CollectionToString(NodeInfos) + ",}";
	}
}
