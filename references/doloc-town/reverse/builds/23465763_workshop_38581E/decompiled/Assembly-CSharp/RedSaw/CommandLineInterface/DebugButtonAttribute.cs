using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class DebugButtonAttribute : Attribute
{
	public readonly string title;

	public DebugButtonType ButtonType { get; set; }

	public DebugButtonAttribute(string title)
	{
		this.title = title;
	}

	public DebugButtonAttribute()
	{
		title = null;
	}
}
