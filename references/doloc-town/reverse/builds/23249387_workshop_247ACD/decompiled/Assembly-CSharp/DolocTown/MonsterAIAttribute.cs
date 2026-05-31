using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MonsterAIAttribute : Attribute
{
	public readonly string name;

	public MonsterAIAttribute(string name)
	{
		this.name = name;
	}
}
