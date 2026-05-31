using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class TbAgentEquipmentSkill
{
	private readonly Dictionary<string, AgentEquipmentSkillInfo> _dataMap;

	private readonly List<AgentEquipmentSkillInfo> _dataList;

	public Dictionary<string, AgentEquipmentSkillInfo> DataMap => _dataMap;

	public List<AgentEquipmentSkillInfo> DataList => _dataList;

	public AgentEquipmentSkillInfo this[string key] => _dataMap[key];

	public TbAgentEquipmentSkill(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AgentEquipmentSkillInfo>();
		_dataList = new List<AgentEquipmentSkillInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AgentEquipmentSkillInfo agentEquipmentSkillInfo = AgentEquipmentSkillInfo.DeserializeAgentEquipmentSkillInfo(child);
			if (_dataMap.TryAdd(agentEquipmentSkillInfo.Id, agentEquipmentSkillInfo))
			{
				_dataList.Add(agentEquipmentSkillInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + agentEquipmentSkillInfo.Id + " in table: TbAgentEquipmentSkill");
			}
		}
	}

	public AgentEquipmentSkillInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AgentEquipmentSkillInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AgentEquipmentSkillInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AgentEquipmentSkillInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
