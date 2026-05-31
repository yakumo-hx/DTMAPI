namespace RedSaw.CommandLineInterface;

internal abstract class InputBehaviourStateWithInput : InputBehaviourState
{
	public string CurrentInput { get; protected set; } = string.Empty;


	public InputBehaviourStateWithInput(InputBehaviourStateMachine stateMachine, string input = "", bool afterAssign = false)
		: base(stateMachine, afterAssign)
	{
		CurrentInput = input;
	}

	public override string ToString()
	{
		return "<State: " + CurrentInput + ">";
	}
}
