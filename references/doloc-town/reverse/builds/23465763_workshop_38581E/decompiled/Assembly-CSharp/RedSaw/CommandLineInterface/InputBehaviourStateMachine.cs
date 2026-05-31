using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

internal class InputBehaviourStateMachine
{
	public readonly Stack<InputBehaviourState> stateStack;

	private readonly Func<string, Type> getVariableType;

	private readonly Func<string, Type> getCallableType;

	private InputBehaviourState currentState;

	public string CurrentStatus
	{
		get
		{
			if (currentState == null)
			{
				return "<No State>";
			}
			return currentState.ToString();
		}
	}

	public SuggestionQuery CurrentSuggestionQuery => currentState.GetSuggestion();

	public InputBehaviourStateMachine(Func<string, Type> getVariableType, Func<string, Type> getCallableType)
	{
		this.getVariableType = getVariableType;
		this.getCallableType = getCallableType;
		stateStack = new Stack<InputBehaviourState>();
		currentState = new IBS_Ready(this);
	}

	public InputBehaviourState TryGetVariableMemberState(string variableName)
	{
		Type type = getVariableType(variableName);
		if (type == null)
		{
			return new IBS_Unknown(this);
		}
		return new IBS_Member(this, type);
	}

	public InputBehaviourState TryGetCallableMemberState(string callableName)
	{
		Type type = getCallableType(callableName);
		if (type == null)
		{
			return new IBS_Unknown(this);
		}
		return new IBS_Member(this, type);
	}

	public void StepForward(char c)
	{
		InputBehaviourState inputBehaviourState = currentState.StepForward(c);
		if (inputBehaviourState != currentState)
		{
			if (inputBehaviourState.ShouldCollapse)
			{
				stateStack.Pop();
			}
			stateStack.Push(currentState);
			currentState = inputBehaviourState;
		}
	}

	public void StepBackward()
	{
		if (currentState.StepBackward())
		{
			currentState = stateStack.Pop();
		}
	}

	public void Reset()
	{
		stateStack.Clear();
		currentState = new IBS_Ready(this);
	}

	public void Reset(string input)
	{
		Reset();
		foreach (char c in input)
		{
			StepForward(c);
		}
	}
}
