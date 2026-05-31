using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CommandValueParserAttribute : Attribute
{
	public readonly Type type;

	public string Alias { get; set; }

	public CommandValueParserAttribute(Type type)
	{
		this.type = type;
		Alias = null;
	}
}
