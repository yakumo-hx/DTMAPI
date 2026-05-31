using System;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public abstract class GameProcessTask
{
	[SerializeField]
	private Graph _graph;

	private string _name;

	public virtual string SummaryInfo => "Game Process Task";

	public virtual string ExecutionInfo => "Game Process Task";

	protected bool IsExecuting { get; private set; }

	public string Name
	{
		get
		{
			if (!string.IsNullOrEmpty(_name))
			{
				return _name;
			}
			Type type = GetType();
			_name = (type.IsDefined(typeof(NameAttribute), inherit: false) ? ((NameAttribute)type.GetCustomAttributes(typeof(NameAttribute), inherit: false)[0]).name : type.Name);
			return _name;
		}
	}

	protected GameProcessTask(Graph _graph)
	{
		this._graph = _graph;
	}

	public abstract TaskStatus OnExecute(float dt);

	public virtual void OnBegin()
	{
		IsExecuting = true;
	}

	public virtual void OnEnd()
	{
		IsExecuting = false;
	}
}
