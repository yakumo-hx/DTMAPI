using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterStateManager
{
	public abstract class MonsterState
	{
		protected readonly MonsterStateManager _manager;

		protected readonly MonsterController _controller;

		public MonsterAI _monsterAI => _controller.MonsterAI;

		public T Module<T>() where T : MonsterAI
		{
			return (T)_monsterAI;
		}

		public void BindData(MonsterController controller, MonsterStateManager _manager)
		{
			this.WriteReadonlyField("_controller", controller);
			this.WriteReadonlyField("_manager", _manager);
		}

		protected T GetState<T>() where T : MonsterState
		{
			return _manager.GetState<T>();
		}

		protected MonsterState GetState(Type type)
		{
			return _manager.GetState(type);
		}

		public virtual void OnEnter()
		{
		}

		public virtual void OnExit()
		{
		}

		public virtual void OnFixedUpdate(float dt)
		{
		}

		public abstract MonsterState NextState(float dt);
	}

	public abstract class MonsterState<T> : MonsterState where T : MonsterAI
	{
		public new T Module => (T)base._monsterAI;
	}

	public abstract class MoveState<T> : MonsterState<T> where T : MonsterAI
	{
		protected bool isMoving;

		public override void OnFixedUpdate(float dt)
		{
			if (isMoving && _controller.mover.Move(dt))
			{
				isMoving = false;
			}
		}
	}

	public abstract class AttackState<T> : MonsterState<T> where T : MonsterAI
	{
		protected bool IsAttacking => _controller.attackBehaviourManager.IsAttacking;

		public override void OnFixedUpdate(float dt)
		{
			_controller.attackBehaviourManager.Update(dt);
		}
	}

	private MonsterState __current_state;

	private readonly MonsterController _controller;

	private readonly Dictionary<Type, MonsterState> _states = new Dictionary<Type, MonsterState>();

	public Type _defaultStateType { get; private set; }

	public MonsterState CurrentState => __current_state;

	public MonsterStateManager(MonsterController controller)
	{
		_controller = controller;
	}

	public void SetDefaultState<T>() where T : MonsterState
	{
		_defaultStateType = typeof(T);
	}

	public void SetDefaultState(Type type)
	{
		_defaultStateType = type;
	}

	public T GetState<T>() where T : MonsterState
	{
		return (T)_states[typeof(T)];
	}

	public T EnterState<T>(bool allowSelfTransition = true) where T : MonsterState
	{
		T state = GetState<T>();
		if (state == null)
		{
			return null;
		}
		if (!allowSelfTransition && __current_state == state)
		{
			return null;
		}
		__current_state?.OnExit();
		__current_state = GetState<T>();
		__current_state.OnEnter();
		return (T)__current_state;
	}

	private MonsterState GetState(Type type)
	{
		return _states.GetValueOrDefault(type);
	}

	public MonsterState AddState(Type type)
	{
		if (_states.ContainsKey(type))
		{
			throw new Exception("State \"" + type.Name + "\" already exists");
		}
		MonsterState monsterState = (MonsterState)Activator.CreateInstance(type);
		monsterState.BindData(_controller, this);
		_states.Add(type, monsterState);
		if (_defaultStateType == null)
		{
			SetDefaultState(type);
		}
		return monsterState;
	}

	public T AddState<T>() where T : MonsterState
	{
		return (T)AddState(typeof(T));
	}

	public void OnStart()
	{
		if ((object)_defaultStateType == null)
		{
			Debug.LogError("\"" + GetType().Name + "\" has not set default state");
			__current_state = null;
		}
		else
		{
			__current_state = _states[_defaultStateType];
			__current_state.OnEnter();
		}
	}

	public virtual void OnUpdate(float dt)
	{
		MonsterState monsterState = __current_state?.NextState(dt);
		if (monsterState != null)
		{
			__current_state.OnExit();
			__current_state = monsterState;
			__current_state.OnEnter();
		}
	}

	public virtual void OnFixedUpdate(float dt)
	{
		__current_state?.OnFixedUpdate(dt);
	}
}
