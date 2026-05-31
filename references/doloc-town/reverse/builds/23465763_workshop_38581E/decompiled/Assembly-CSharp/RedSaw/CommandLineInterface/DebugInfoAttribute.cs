using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DebugInfoAttribute : Attribute
{
	public string Key { get; protected set; }

	public string Color { get; set; }

	public bool AllowEdit { get; set; }

	public DebugInfoAttribute(string key)
	{
		Key = key;
		Color = null;
		AllowEdit = false;
	}

	public DebugInfoAttribute()
	{
		Key = null;
		Color = null;
		AllowEdit = false;
	}
}
