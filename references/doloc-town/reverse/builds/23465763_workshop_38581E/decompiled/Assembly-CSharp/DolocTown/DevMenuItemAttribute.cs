using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DevMenuItemAttribute : Attribute
{
	public readonly string title;

	public DevMenuItemAttribute(string title)
	{
		this.title = title;
	}

	public DevMenuItemAttribute()
	{
		title = null;
	}
}
