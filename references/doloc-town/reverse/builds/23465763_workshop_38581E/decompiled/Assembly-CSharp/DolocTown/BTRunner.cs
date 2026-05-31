using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(BehaviourTreeOwner))]
public abstract class BTRunner : GameEntity
{
	protected BehaviourTreeOwner _btOwner;

	protected Blackboard _blackboard;

	protected override void __Init()
	{
		base.__Init();
		_btOwner = GetComponent<BehaviourTreeOwner>();
		_blackboard = GetComponent<Blackboard>();
	}

	public void AddBBVariable<T>(string name, T value)
	{
		IBlackboardExtensions.AddVariable(_blackboard, typeof(T).Name, value);
	}

	public void SetBBVariableGlobal<T>(string name, T value)
	{
		_btOwner.behaviour.blackboard.SetVariableValue(name, value);
	}

	public void SetBBVariableGlobalParent<T>(string name, T value)
	{
		_btOwner.behaviour.blackboard.parent.SetVariableValue(name, value);
	}

	public void SetBBVariableLocal<T>(string name, T value)
	{
		_blackboard.SetVariableValue(name, value);
	}

	protected void StartBT(BehaviourTree bt)
	{
		_btOwner.StartBehaviour(bt, Graph.UpdateMode.Manual);
	}

	protected void StartBTWithBoundGraph()
	{
		if (!(_btOwner.behaviour == null))
		{
			_btOwner.StartBehaviour(_btOwner.behaviour, Graph.UpdateMode.Manual);
		}
	}

	protected void UpdateBT(float deltaTime)
	{
		_btOwner.UpdateBehaviourPatchGivenDelta(deltaTime);
	}

	public void PauseBT()
	{
		_btOwner.PauseBehaviour();
	}

	public void ResumeBT()
	{
		_btOwner.RestartBehaviour();
	}

	protected void StopBT()
	{
		_btOwner.StopBehaviour();
	}
}
