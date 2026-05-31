using System;
using DolocTown.Config.Resource;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class)]
public class EnvObjectAttribute : Attribute
{
	public readonly EnvObjectType type;

	public EnvObjectAttribute(EnvObjectType type)
	{
		this.type = type;
	}
}
