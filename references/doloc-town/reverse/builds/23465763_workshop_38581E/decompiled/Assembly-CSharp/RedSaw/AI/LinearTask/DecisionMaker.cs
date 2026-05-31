using System;

namespace RedSaw.AI.LinearTask;

public abstract class DecisionMaker
{
	private LinearTask _currentTask;

	private LinearTask _lastTask;

	private LinearTaskBreaker _currentBreaker;

	public Type CurrentTaskType => _currentTask?.GetType();

	public LinearTask CurrentTask
	{
		get
		{
			return _currentTask;
		}
		set
		{
			_lastTask = _currentTask;
			_currentTask = value;
		}
	}

	public Action<bool> DisposableCallbackOnActionDone { get; set; }

	public event Action<LinearTask> OnUpdated;

	public event Action<LinearTask> OnBegined;

	public event Action<LinearTask> OnFailure;

	public event Action<LinearTask> OnBreaked;

	public event Action<LinearTask> OnSuccessed;

	public bool CheckTaskType<T>() where T : LinearTask
	{
		return _currentTask is T;
	}

	public void UpdatePerSec()
	{
		Update(1f);
	}

	public void Update(float dt)
	{
		OnUpdate(dt);
		if (CurrentTask == null)
		{
			CurrentTask = MakeDecision();
			_currentBreaker = CurrentTask.ComposeBreaker;
			CurrentTask?.OnBegin();
			this.OnBegined?.Invoke(CurrentTask);
			return;
		}
		if (_currentBreaker.Check())
		{
			CurrentTask.OnBreak();
			this.OnBreaked?.Invoke(CurrentTask);
			CurrentTask = null;
			return;
		}
		switch (CurrentTask.OnExecute(dt))
		{
		case TaskStatus.Failure:
			CurrentTask.OnFailure();
			this.OnFailure?.Invoke(CurrentTask);
			CurrentTask = null;
			break;
		case TaskStatus.Executing:
			this.OnUpdated?.Invoke(CurrentTask);
			break;
		case TaskStatus.Success:
			CurrentTask.OnSuccess();
			this.OnSuccessed?.Invoke(CurrentTask);
			CurrentTask = CurrentTask.Next;
			if (CurrentTask != null)
			{
				_currentBreaker = CurrentTask.ComposeBreaker;
				CurrentTask?.OnBegin();
				this.OnBegined?.Invoke(CurrentTask);
				if (_lastTask.SkipAfterSuccess)
				{
					Update(dt);
				}
			}
			break;
		}
	}

	protected virtual void OnUpdate(float dt)
	{
	}

	public void ChangeTask(LinearTask task)
	{
		if (task != null)
		{
			_currentTask?.OnBreak();
			this.OnBreaked?.Invoke(_currentTask);
			_currentTask = task;
			_currentBreaker = _currentTask.ComposeBreaker;
			_currentTask?.OnBegin();
			this.OnBegined?.Invoke(_currentTask);
		}
	}

	public void StopTask()
	{
		if (_currentTask != null)
		{
			_currentTask.OnBreak();
			this.OnBreaked?.Invoke(_currentTask);
			_currentTask = null;
			_currentBreaker = null;
		}
	}

	protected abstract LinearTask MakeDecision();

	public virtual void RefreshWork()
	{
		ChangeTask(MakeDecision());
	}
}
