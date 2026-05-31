using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class FastButtonAttribute : Attribute
{
	public readonly string title;

	public FastButtonAttribute(string title)
	{
		this.title = title;
	}

	public FastButtonAttribute()
	{
		title = null;
	}
}
