using System.Collections.Generic;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown;

public class AgentSkillManager
{
	public enum TriggerType
	{
		NONE,
		THROW
	}

	private readonly Dictionary<string, SkillManager> _skillManagers = new Dictionary<string, SkillManager>();

	~AgentSkillManager()
	{
		foreach (SkillManager value in _skillManagers.Values)
		{
			DolocAPI.battleSystem.RemoveSkillManager(value);
		}
		_skillManagers.Clear();
	}

	public Skill MakeSkillObject(string name)
	{
		if (_skillManagers.TryGetValue(name, out var value))
		{
			if (!value.Raise(out var skill))
			{
				return null;
			}
			return skill;
		}
		SkillManager skillManager = DolocAPI.battleSystem.CreateSkillManager(name);
		if (skillManager == null)
		{
			return null;
		}
		_skillManagers.Add(name, skillManager);
		if (!skillManager.Raise(out var skill2))
		{
			return null;
		}
		return skill2;
	}

	public void UseSkillFromItemName(string itemName, TriggerType type)
	{
		if (!DolocAPI.QueryItemProto(itemName, out var proto) || !(proto.Function is ItemFunctionMissile { Name: var name } itemFunctionMissile))
		{
			return;
		}
		Skill skill = DolocAPI.gameStateManager.agentController.skillManager.MakeSkillObject(name);
		if (skill == null)
		{
			return;
		}
		if (!(skill is TimeBomb timeBomb))
		{
			if (!(skill is Bomb bomb))
			{
				if (skill is Firecracker firecracker)
				{
					HandleFireCracker(itemFunctionMissile, firecracker, type);
				}
			}
			else
			{
				HandleBomb(itemFunctionMissile, bomb, type);
			}
		}
		else
		{
			HandleTimeBomb(itemFunctionMissile, timeBomb, type);
		}
	}

	private void HandleTimeBomb(ItemFunctionMissile itemFunction, TimeBomb timeBomb, TriggerType triggerType)
	{
		timeBomb.Damage = itemFunction.Damage;
		timeBomb.CriticalRate = itemFunction.CriticalRate;
		timeBomb.ShouldWorkOnMonster = true;
		timeBomb.IsItemBomb = true;
		timeBomb.position2d = DolocAPI.agent.PositionCenter;
	}

	private void HandleBomb(ItemFunctionMissile itemFunction, Bomb bomb, TriggerType triggerType)
	{
		bomb.SetVisible(value: false);
		bomb.position2d = DolocAPI.agent.PositionThrow;
		bomb.ShouldWorkOnMonster = true;
		bomb.Damage = itemFunction.Damage;
		bomb.CriticalRate = itemFunction.CriticalRate;
		if (triggerType == TriggerType.THROW && !DolocAPI.AgentController.IsRidingNow)
		{
			DolocAPI.agent._Throw(delegate(bool isFaceRight)
			{
				_Bomb(isFaceRight, bomb);
			});
		}
		else
		{
			_Bomb(DolocAPI.agent.IsFaceRight, bomb);
		}
	}

	private void _Bomb(bool isFaceRight, Bomb bomb)
	{
		bomb.SetVisible(value: true);
		bomb.position2d = DolocAPI.agent.PositionThrow;
		bomb.AddForce(new Vector2(isFaceRight ? 1 : (-1), 2f) * 16f);
	}

	private void HandleFireCracker(ItemFunctionMissile itemFunction, Firecracker firecracker, TriggerType triggerType)
	{
		firecracker.SetVisible(value: false);
		firecracker.position2d = DolocAPI.agent.PositionThrow;
		firecracker.Damage = itemFunction.Damage;
		firecracker.CriticalRate = itemFunction.CriticalRate;
		if (triggerType == TriggerType.THROW && !DolocAPI.AgentController.IsRidingNow)
		{
			DolocAPI.agent._Throw(delegate(bool isFaceRight)
			{
				_FireCracker(isFaceRight, firecracker);
			});
		}
		else
		{
			_FireCracker(DolocAPI.agent.IsFaceRight, firecracker);
		}
	}

	private void _FireCracker(bool isFaceRight, Firecracker firecracker)
	{
		firecracker.SetVisible(value: true);
		firecracker.position2d = DolocAPI.agent.PositionThrow;
		firecracker.AddForce(new Vector2(isFaceRight ? 1 : (-1), 1.5f) * 20f);
	}
}
