using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class AgentEquipmentFunction
{
	private static readonly Dictionary<string, Type> functionTypes = InitializeFunctionTypeCache();

	protected readonly Item item;

	protected readonly AgentEquipmentManager _manager;

	protected readonly AgentEquipmentSkillInfo skill;

	protected readonly AgentEquipmentFuncProto proto;

	public string SkillId => skill.Id;

	public virtual bool IsChomperMimicryForbidden => false;

	public virtual float CriticalRateIncrease => 0f;

	private static Dictionary<string, Type> InitializeFunctionTypeCache()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(AgentEquipmentFunction).GetSubTypes();
		foreach (Type type in subTypes)
		{
			dictionary[type.Name] = type;
		}
		return dictionary;
	}

	public static bool CreateAgentEquipmentFunction(Item item, AgentEquipmentManager manager, string skillId, out AgentEquipmentFunction function)
	{
		function = null;
		if (skillId.IsNullOrEmpty())
		{
			return false;
		}
		AgentEquipmentSkillInfo orDefault = DolocConfig.Tables.TbAgentEquipmentSkill.GetOrDefault(skillId);
		if (orDefault == null)
		{
			Debug.LogWarning("未找到名称为\"" + skillId + "\"的主角装备技能");
			return false;
		}
		function = _CreateAgentEquipmentFunction(item, manager, orDefault);
		return function != null;
	}

	private static AgentEquipmentFunction _CreateAgentEquipmentFunction(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
	{
		string text = skill.Function.GetType().Name.Replace("FuncProto", "Function");
		if (!functionTypes.TryGetValue(text, out var value))
		{
			Debug.LogError("未找到名称为\"" + text + "\"的主角技能类型");
			return null;
		}
		return (AgentEquipmentFunction)Activator.CreateInstance(value, item, manager, skill);
	}

	protected AgentEquipmentFunction(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
	{
		this.item = item;
		_manager = manager;
		this.skill = skill;
		proto = skill.Function;
	}

	public virtual AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		return agentEquipmentParams;
	}

	public virtual void OnReceiveMessage(GameMessage message)
	{
	}

	public virtual void UpdatePerTu()
	{
	}

	public virtual void UpdatePerTuNoRender()
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual bool TryResistFaint(bool shouldRender)
	{
		return false;
	}

	public virtual bool IsShepherdActive(string name, out int moodIncrease)
	{
		moodIncrease = 0;
		return false;
	}

	public virtual void SetFishingPoolName(string poolName)
	{
	}

	public virtual void AfterEnterRoom(Room room)
	{
	}
}
