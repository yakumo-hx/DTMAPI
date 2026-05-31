using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MonsterEntranceStateAttribute : Attribute
{
	public readonly Type type;

	public MonsterEntranceStateAttribute(Type type)
	{
		this.type = type;
	}
}
