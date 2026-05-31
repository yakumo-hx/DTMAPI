using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw;

public class StateMachine
{
	private static readonly FieldInfo BlackboardSetter = typeof(ActionTask).GetField("blackboard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private readonly IBlackboard blackboard;

	private readonly Dictionary<Type, ActionTask> tasks = new Dictionary<Type, ActionTask>();

	private ActionTask currentTask;

	private bool isRunning;

	private static void SetBlackboard(ActionTask task, IBlackboard bb)
	{
		BlackboardSetter.SetValue(task, bb);
	}

	public StateMachine(IBlackboard blackboard, Type[] types)
	{
		this.blackboard = blackboard;
		foreach (Type type in types)
		{
			ActionTask actionTask = AddTask(type);
			if (actionTask != null && actionTask.IsEntrance)
			{
				currentTask = actionTask;
			}
		}
	}

	private void OnActionEnded(Type t)
	{
		isRunning = false;
		if (!(t == null))
		{
			currentTask = GetTask(t);
			if (currentTask != null)
			{
				isRunning = true;
				currentTask.Begin();
			}
		}
	}

	private ActionTask AddTask(Type type)
	{
		if (!typeof(ActionTask).IsAssignableFrom(type) || type.IsAbstract)
		{
			return null;
		}
		if (tasks.ContainsKey(type))
		{
			return null;
		}
		ActionTask actionTask = (ActionTask)Activator.CreateInstance(type);
		SetBlackboard(actionTask, blackboard);
		actionTask.Init();
		actionTask.OnActionEnded += OnActionEnded;
		tasks.Add(type, actionTask);
		if (actionTask.IsEntrance)
		{
			currentTask = actionTask;
		}
		return actionTask;
	}

	public ActionTask GetTask(Type type)
	{
		if (tasks.ContainsKey(type))
		{
			return tasks[type];
		}
		return null;
	}

	public ActionTask GetTask<T>() where T : ActionTask
	{
		return GetTask(typeof(T));
	}

	public void Begin<T>() where T : ActionTask
	{
		currentTask = GetTask<T>();
		if (currentTask != null)
		{
			isRunning = true;
			currentTask.Begin();
		}
		else
		{
			isRunning = false;
		}
	}

	public void Begin()
	{
		if (currentTask != null)
		{
			currentTask.Begin();
			isRunning = true;
		}
		else
		{
			isRunning = false;
		}
	}

	public void Update()
	{
		if (isRunning)
		{
			currentTask.Execute();
		}
	}
}
