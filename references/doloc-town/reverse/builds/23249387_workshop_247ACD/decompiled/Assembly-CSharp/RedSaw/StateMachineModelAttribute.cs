using System;

namespace RedSaw;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class StateMachineModelAttribute : Attribute
{
	public readonly string modelName;

	public StateMachineModelAttribute(string modelName)
	{
		this.modelName = modelName;
	}
}
