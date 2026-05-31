namespace RedSaw.AI.StateMachine;

public abstract class State
{
	private readonly StateMachine _sm;

	protected State(StateMachine machine)
	{
		_sm = machine;
	}

	protected State GetState<T>() where T : State
	{
		return _sm.GetState(typeof(T));
	}

	public void FixedUpdate(float dt)
	{
		OnFixedUpdate(dt);
	}

	public abstract State GetNextState(float dt);

	protected virtual void OnFixedUpdate(float dt)
	{
	}

	public virtual void OnExit()
	{
	}

	public virtual void OnEnter()
	{
	}
}
