using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class BoardMissionInfo : BeanBase
{
	public const int __ID__ = 359388662;

	public string Id { get; private set; }

	public string Level { get; private set; }

	public string MissionInfo { get; private set; }

	public MissionInfo MissionInfo_Ref { get; private set; }

	public string MissionLabel { get; private set; }

	public BoardMissionTypeInfo MissionLabel_Ref { get; private set; }

	public int TimeLimit { get; private set; }

	public int[] Season { get; private set; }

	public MissionContent Content { get; private set; }

	public int Favorability { get; private set; }

	public RewardProto[] Rewards { get; private set; }

	public bool ShouldSendEmail { get; private set; }

	public BoardMissionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["level"].IsString)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["mission_info"].IsString)
		{
			throw new SerializationException();
		}
		MissionInfo = _json["mission_info"];
		if (!_json["mission_label"].IsString)
		{
			throw new SerializationException();
		}
		MissionLabel = _json["mission_label"];
		if (!_json["time_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeLimit = _json["time_limit"];
		JSONNode jSONNode = _json["season"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Season = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			Season[num++] = num2;
		}
		if (!_json["content"].IsObject)
		{
			throw new SerializationException();
		}
		Content = MissionContent.DeserializeMissionContent(_json["content"]);
		if (!_json["favorability"].IsNumber)
		{
			throw new SerializationException();
		}
		Favorability = _json["favorability"];
		JSONNode jSONNode2 = _json["rewards"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		Rewards = new RewardProto[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			RewardProto rewardProto = ExternalTypeUtil.RewardProtoConverter(CfgRewardProto.DeserializeCfgRewardProto(child2));
			Rewards[num3++] = rewardProto;
		}
		if (!_json["should_send_email"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShouldSendEmail = _json["should_send_email"];
	}

	public BoardMissionInfo(string id, string level, string mission_info, string mission_label, int time_limit, int[] season, MissionContent content, int favorability, RewardProto[] rewards, bool should_send_email)
	{
		Id = id;
		Level = level;
		MissionInfo = mission_info;
		MissionLabel = mission_label;
		TimeLimit = time_limit;
		Season = season;
		Content = content;
		Favorability = favorability;
		Rewards = rewards;
		ShouldSendEmail = should_send_email;
	}

	public static BoardMissionInfo DeserializeBoardMissionInfo(JSONNode _json)
	{
		return new BoardMissionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 359388662;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MissionInfo_Ref = (_tables["Mission.TbMission"] as TbMission).GetOrDefault(MissionInfo);
		MissionLabel_Ref = (_tables["Mission.TbBoardMissionType"] as TbBoardMissionType).GetOrDefault(MissionLabel);
		Content?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Content?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Level:" + Level + ",MissionInfo:" + MissionInfo + ",MissionLabel:" + MissionLabel + ",TimeLimit:" + TimeLimit + ",Season:" + StringUtil.CollectionToString(Season) + ",Content:" + Content?.ToString() + ",Favorability:" + Favorability + ",Rewards:" + StringUtil.CollectionToString(Rewards) + ",ShouldSendEmail:" + ShouldSendEmail + ",}";
	}
}
