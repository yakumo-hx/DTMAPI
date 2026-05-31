using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DebugGroupAttribute : Attribute
{
	public readonly string groupId;

	public DebugGroupAttribute(string groupId)
	{
		this.groupId = groupId;
	}
}
