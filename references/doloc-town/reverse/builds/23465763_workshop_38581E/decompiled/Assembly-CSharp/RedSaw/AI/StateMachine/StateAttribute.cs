using System;

namespace RedSaw.AI.StateMachine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class StateAttribute : Attribute
{
	public readonly string aiName;

	public readonly bool isDefault;

	public StateAttribute(string aiName, bool isDefault = false)
	{
		this.aiName = aiName;
		this.isDefault = isDefault;
	}
}
