using System;

namespace RedSaw;

public abstract class ActionTask
{
	public readonly IBlackboard blackboard;

	public ActionStatus Status { get; private set; }

	public virtual bool IsEntrance => false;

	public event Action<Type> OnActionEnded;

	public virtual void Init()
	{
	}

	public virtual void Begin()
	{
	}

	public virtual void Execute()
	{
	}

	protected void EndAction(Type type)
	{
		if (typeof(ActionTask).IsAssignableFrom(type))
		{
			Status = ActionStatus.Ended;
			this.OnActionEnded?.Invoke(type);
		}
	}

	protected void EndAction<T>() where T : ActionTask
	{
		Status = ActionStatus.Ended;
		this.OnActionEnded?.Invoke(typeof(T));
	}

	public void EndAction()
	{
		Status = ActionStatus.Ended;
		this.OnActionEnded?.Invoke(null);
	}
}
