using System;

namespace RedSaw.CommandLineInterface;

public class CharAutomaton
{
	private readonly InputBehaviourStateMachine stateMachine;

	private string lastInput = string.Empty;

	public string CurrentStatus => stateMachine.CurrentStatus;

	public CharAutomaton(Func<string, Type> getVariableType, Func<string, Type> getCallableType)
	{
		stateMachine = new InputBehaviourStateMachine(getVariableType, getCallableType);
	}

	public SuggestionQuery Input(string newInput)
	{
		if (newInput == null || newInput.Length == 0)
		{
			stateMachine.Reset();
			lastInput = string.Empty;
			return SuggestionQuery.None;
		}
		if (newInput.Length == lastInput.Length)
		{
			if (newInput == lastInput)
			{
				return stateMachine.CurrentSuggestionQuery;
			}
			Rehandle(newInput);
			return stateMachine.CurrentSuggestionQuery;
		}
		if (newInput.Length > lastInput.Length)
		{
			if (newInput.StartsWith(lastInput))
			{
				string text = newInput[lastInput.Length..];
				foreach (char c in text)
				{
					stateMachine.StepForward(c);
				}
				lastInput = newInput;
				return stateMachine.CurrentSuggestionQuery;
			}
			Rehandle(newInput);
			return stateMachine.CurrentSuggestionQuery;
		}
		if (newInput.Length < lastInput.Length)
		{
			if (lastInput.StartsWith(newInput))
			{
				string text = lastInput[newInput.Length..];
				for (int i = 0; i < text.Length; i++)
				{
					_ = text[i];
					stateMachine.StepBackward();
				}
				lastInput = newInput;
				return stateMachine.CurrentSuggestionQuery;
			}
			Rehandle(newInput);
			return stateMachine.CurrentSuggestionQuery;
		}
		return stateMachine.CurrentSuggestionQuery;
	}

	private void Rehandle(string input)
	{
		lastInput = input;
		stateMachine.Reset(input);
	}
}
