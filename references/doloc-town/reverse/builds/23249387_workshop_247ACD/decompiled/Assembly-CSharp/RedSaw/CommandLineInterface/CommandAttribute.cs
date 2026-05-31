using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CommandAttribute : Attribute
{
	public string Name { get; private set; }

	public string Desc { get; set; }

	public string Tag { get; set; }

	public CommandAttribute(string name)
	{
		Name = name;
		Tag = null;
		Desc = "command has no description";
	}

	public CommandAttribute()
	{
		Name = null;
		Tag = null;
		Desc = "command has no description";
	}
}
