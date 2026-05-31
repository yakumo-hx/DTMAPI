using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

internal abstract class InputBehaviourState
{
	protected readonly InputBehaviourStateMachine stateMachine;

	public bool ShouldCollapse { get; protected set; }

	public bool AfterAssign { get; protected set; }

	public InputBehaviourState(InputBehaviourStateMachine stateMachine, bool afterAssign = false)
	{
		this.stateMachine = stateMachine;
		AfterAssign = afterAssign;
	}

	public abstract InputBehaviourState StepForward(char c);

	public abstract bool StepBackward();

	public virtual SuggestionQuery GetSuggestion()
	{
		return SuggestionQuery.None;
	}

	protected IBS_Wait Wait()
	{
		return new IBS_Wait(stateMachine);
	}

	protected IBS_Unknown Unknown()
	{
		return new IBS_Unknown(stateMachine);
	}

	protected IBS_Variable Variable()
	{
		return new IBS_Variable(stateMachine);
	}

	protected InputBehaviourState VariableMember(string typeName)
	{
		return stateMachine.TryGetVariableMemberState(typeName);
	}

	protected InputBehaviourState CommandMember(string commandName)
	{
		return stateMachine.TryGetCallableMemberState(commandName);
	}

	protected InputBehaviourState Member(Type type, string name)
	{
		MemberInfo defaultMember = type.GetDefaultMember(name);
		if (defaultMember == null)
		{
			return new IBS_Unknown(stateMachine);
		}
		return defaultMember.MemberType switch
		{
			MemberTypes.Field => new IBS_Member(stateMachine, ((FieldInfo)defaultMember).FieldType), 
			MemberTypes.Property => new IBS_Member(stateMachine, ((PropertyInfo)defaultMember).PropertyType), 
			_ => new IBS_Unknown(stateMachine), 
		};
	}

	protected IBS_Command Callable(char firstChar, bool shouldCollapse = false, bool afterAssign = false)
	{
		return new IBS_Command(stateMachine, firstChar, shouldCollapse, afterAssign);
	}

	public override string ToString()
	{
		return "<State>";
	}
}
