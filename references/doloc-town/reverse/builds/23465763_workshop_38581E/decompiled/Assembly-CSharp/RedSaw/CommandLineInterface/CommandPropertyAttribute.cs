using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class CommandPropertyAttribute : Attribute
{
	public readonly string Name;

	public readonly string Tag;

	public readonly string Desc;

	public CommandPropertyAttribute(string name)
	{
		Name = name;
		Desc = "property has no description";
		Tag = null;
	}

	public CommandPropertyAttribute()
	{
		Name = null;
		Desc = "property has no description";
		Tag = null;
	}
}
