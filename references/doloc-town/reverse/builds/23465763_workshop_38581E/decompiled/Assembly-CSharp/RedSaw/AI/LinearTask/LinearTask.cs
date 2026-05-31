using System;
using System.Collections.Generic;

namespace RedSaw.AI.LinearTask;

public abstract class LinearTask
{
	private LinearTask rootMission;

	private LinearTask nextMission;

	private LinearTaskBreaker groupBreaker;

	private bool shouldInheritGroupBreaker;

	private LinearTaskBreaker breaker;

	public static LinearTask Defalut => WaitFrames(3);

	public static LinearTask Empty => new LinearTaskEmpty();

	public static LinearTask StartWith => new LinearTaskEmpty();

	public LinearTask Next => nextMission;

	public LinearTask Root => rootMission ?? this;

	public LinearTask Last
	{
		get
		{
			if (nextMission != null)
			{
				return nextMission.Last;
			}
			return this;
		}
	}

	public virtual bool SkipAfterSuccess => false;

	public LinearTaskBreaker ComposeBreaker
	{
		get
		{
			if (breaker == null)
			{
				return groupBreaker ?? LinearTaskBreaker.Empty;
			}
			if (groupBreaker == null)
			{
				return breaker ?? LinearTaskBreaker.Empty;
			}
			return breaker.LinkAfter(groupBreaker);
		}
	}

	public IEnumerable<LinearTask> AllMissionsFollowed
	{
		get
		{
			for (LinearTask mission = this; mission != null; mission = mission.nextMission)
			{
				yield return mission;
			}
		}
	}

	public static LinearTask DoAction(Action action)
	{
		return new LinearTaskDelegate(action);
	}

	public static LinearTask WaitSeconds(float duration)
	{
		return new LinearTaskWait(duration);
	}

	public static LinearTask WaitFrames(int duration)
	{
		return new LinearTaskWaitInt(duration);
	}

	public void ForEach(Action<LinearTask> handle)
	{
		foreach (LinearTask item in AllMissionsFollowed)
		{
			handle(item);
		}
	}

	public LinearTask(LinearTaskBreaker breaker = null)
	{
		this.breaker = breaker;
		rootMission = null;
		nextMission = null;
	}

	public TaskStatus OnExecute()
	{
		return OnExecute(1f);
	}

	public abstract TaskStatus OnExecute(float dt);

	protected TaskStatus EndExecuteAction(bool value)
	{
		if (!value)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}

	protected TaskStatus EndExecute(bool value)
	{
		if (!value)
		{
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}

	public virtual void OnBegin()
	{
	}

	public virtual void OnSuccess()
	{
	}

	public virtual void OnFailure()
	{
	}

	public virtual void OnBreak()
	{
	}

	private LinearTask _ThenSingleTask(LinearTask task)
	{
		if (shouldInheritGroupBreaker && groupBreaker != null)
		{
			task.BeginBreaker(groupBreaker);
		}
		task.rootMission = rootMission ?? this;
		nextMission = task;
		return task;
	}

	public LinearTask Then(LinearTask task)
	{
		LinearTask root = task.Root;
		if (shouldInheritGroupBreaker && groupBreaker != null)
		{
			root.InheritGroupBreaker(groupBreaker);
		}
		root._ResetRoot(rootMission ?? this);
		nextMission = root;
		return root.Last;
	}

	private void _ResetRoot(LinearTask newRoot)
	{
		rootMission = newRoot;
		nextMission?._ResetRoot(newRoot);
	}

	public LinearTask Then(Func<LinearTask> taskBuilder)
	{
		return Then(taskBuilder());
	}

	public LinearTask Do(Action cb)
	{
		return Then(new LinearTaskDelegate(cb));
	}

	public LinearTask Wait(float dur)
	{
		return Then(new LinearTaskWait(dur));
	}

	public LinearTask WaitFrm(int dur)
	{
		return Then(new LinearTaskWaitInt(dur));
	}

	private void InheritGroupBreaker(LinearTaskBreaker breaker)
	{
		groupBreaker = groupBreaker?.LinkAfter(breaker) ?? breaker;
		shouldInheritGroupBreaker = true;
		nextMission?.InheritGroupBreaker(breaker);
	}

	private LinearTask BeginBreaker(LinearTaskBreaker breaker)
	{
		groupBreaker = groupBreaker?.LinkAfter(breaker) ?? breaker;
		shouldInheritGroupBreaker = true;
		return this;
	}

	public LinearTask BeginBreaker(Func<bool> conditionFunc)
	{
		return BeginBreaker(new LinearTaskBreakerDelegate(conditionFunc));
	}

	public LinearTask EndBreaker()
	{
		shouldInheritGroupBreaker = false;
		return this;
	}

	public LinearTask BreakIf(LinearTaskBreaker nextBreaker)
	{
		if (breaker == null)
		{
			breaker = nextBreaker;
		}
		else
		{
			breaker = breaker.LinkAfter(nextBreaker);
		}
		return this;
	}

	public LinearTask BreakIf(Func<bool> conditionFunc)
	{
		return BreakIf(new LinearTaskBreakerDelegate(conditionFunc));
	}
}
