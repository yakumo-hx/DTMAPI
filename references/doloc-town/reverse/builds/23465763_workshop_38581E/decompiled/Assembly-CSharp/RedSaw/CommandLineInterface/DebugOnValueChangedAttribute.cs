using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DebugOnValueChangedAttribute : Attribute
{
	public readonly string callbackName;

	public DebugOnValueChangedAttribute(string callbackName)
	{
		this.callbackName = callbackName;
	}
}
