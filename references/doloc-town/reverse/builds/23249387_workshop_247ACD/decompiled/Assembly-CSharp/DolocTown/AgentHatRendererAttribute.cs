using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class)]
public class AgentHatRendererAttribute : Attribute
{
	public readonly string PrefabName;

	public AgentHatRendererAttribute(string prefabName)
	{
		PrefabName = prefabName;
	}
}
