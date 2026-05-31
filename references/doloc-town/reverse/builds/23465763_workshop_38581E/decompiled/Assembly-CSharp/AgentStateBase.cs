using System;
using DolocTown;

public abstract class AgentStateBase
{
	protected readonly BodyController body;

	protected readonly AgentPhysicalStatus status;

	protected readonly AgentStateManager parent;

	public virtual bool SupportJump => parent.GetState<AgentStateJump>().jumpTimes > 0;

	public virtual bool SupportDash => true;

	public virtual bool SupportUseItem => false;

	public virtual bool SupportInteract => false;

	public virtual bool SupportScrollQuickInventoryUI => true;

	protected AgentStateBase NextStateOnGround
	{
		get
		{
			if (status.HorizontalMoveFactor != 0f)
			{
				return parent.GetState<AgentStateMove>();
			}
			if (!body.ShouldEnterToolState)
			{
				return parent.GetState<AgentStateIdle>();
			}
			body.LoadCurrentTool();
			return parent.GetState<AgentStateTool>();
		}
	}

	protected AgentStateBase NextStateUniversal
	{
		get
		{
			if (status.IsGrounded)
			{
				return NextStateOnGround;
			}
			if (!status.IsDrop)
			{
				return this;
			}
			return parent.GetState<AgentStateDrop>();
		}
	}

	public float animationNormalizedTime => body.GetAnimationNormalizedTime();

	protected AgentStateBase(AgentStateManager parent, BodyController body)
	{
		this.parent = parent;
		this.body = body;
		status = body.Status;
	}

	public T GetState<T>() where T : AgentStateBase
	{
		return parent.GetState<T>();
	}

	public T GetState<T>(Action<T> beforeEnter) where T : AgentStateBase
	{
		T state = GetState<T>();
		beforeEnter?.Invoke(state);
		return state;
	}

	public bool Check<T>() where T : AgentStateBase
	{
		return parent.CheckState<T>();
	}

	public bool Check<T>(Func<T, bool> condition) where T : AgentStateBase
	{
		if (Check<T>())
		{
			return condition((T)this);
		}
		return false;
	}

	protected bool IsAnimationDone(string name)
	{
		float process;
		return IsAnimationDone(name, out process);
	}

	protected bool IsAnimationDone(string name, out float process)
	{
		return body.IsAnimationDone(name, out process);
	}

	public void Update()
	{
		AgentStateBase agentStateBase = NextState();
		if (!parent.IsFixed && agentStateBase != this)
		{
			OnExit();
			agentStateBase.OnEnter();
			parent.__SetState(agentStateBase);
		}
	}

	protected abstract AgentStateBase NextState();

	public virtual void OnEnter()
	{
	}

	public virtual void OnExit()
	{
	}

	public virtual void OnPlay()
	{
	}
}
