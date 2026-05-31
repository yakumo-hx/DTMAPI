using System;
using System.Collections.Generic;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourManager<T> where T : Enum
{
	private readonly CDCounter<T> counter = new CDCounter<T>();

	private readonly Dictionary<T, MonsterAttackBehaviour> attackBehaviours = new Dictionary<T, MonsterAttackBehaviour>();

	private MonsterAttackBehaviour _currentBehaviour;

	private Action _callback;

	public bool IsAttacking => _currentBehaviour != null;

	public bool IsAttackingOfType<TSKill>() where TSKill : IMonsterAttackBehaviour
	{
		MonsterAttackBehaviour currentBehaviour = _currentBehaviour;
		if (currentBehaviour != null)
		{
			return currentBehaviour._proto is TSKill;
		}
		return false;
	}

	public bool TryGetFirstAvailableAttackBehaviour(out T type)
	{
		foreach (KeyValuePair<T, MonsterAttackBehaviour> attackBehaviour in attackBehaviours)
		{
			if (counter.IsAvailable(attackBehaviour.Key))
			{
				type = attackBehaviour.Key;
				return true;
			}
		}
		type = default(T);
		return false;
	}

	public bool Update(float dt)
	{
		if (_currentBehaviour == null)
		{
			return true;
		}
		if (!_currentBehaviour.OnUpdate(dt))
		{
			return false;
		}
		_currentBehaviour = null;
		_callback?.Invoke();
		_callback = null;
		return true;
	}

	public void StopAttackBehaviour()
	{
		if (_currentBehaviour != null)
		{
			_currentBehaviour = null;
			_callback?.Invoke();
			_callback = null;
		}
	}

	public void UpdateCDCounter(float dt)
	{
		counter.Update(dt);
	}

	public void ReduceCD(T type, float dt)
	{
		counter.ReduceCD(type, dt);
	}

	public bool TryGetAttackBehaviour(T type, out MonsterAttackBehaviour behaviour)
	{
		return attackBehaviours.TryGetValue(type, out behaviour);
	}

	public bool RegisterAttackBehaviour(T type, MonsterAttackBehaviour behaviour)
	{
		if (behaviour == null)
		{
			return false;
		}
		attackBehaviours[type] = behaviour;
		counter.ResetCounter(type, behaviour.CDDuration);
		if (behaviour.CDOnStart)
		{
			counter.Use(type);
		}
		return true;
	}

	public bool IsAvailable(T type)
	{
		return counter.IsAvailable(type);
	}

	public void Attack(T type, Transform target, Action callback)
	{
		if (!attackBehaviours.TryGetValue(type, out _currentBehaviour))
		{
			_currentBehaviour = null;
			callback?.Invoke();
			return;
		}
		_callback = callback;
		_currentBehaviour.SetAttackTarget(target);
		_currentBehaviour.BeforeInvoke();
		_currentBehaviour.Invoke();
		counter.Use(type);
	}

	public void Dispose(BattleSystem bs)
	{
		_currentBehaviour = null;
		foreach (MonsterAttackBehaviour value in attackBehaviours.Values)
		{
			value.Dispose(bs);
		}
	}

	public void ResetCD(T type)
	{
		counter.Use(type);
	}
}
