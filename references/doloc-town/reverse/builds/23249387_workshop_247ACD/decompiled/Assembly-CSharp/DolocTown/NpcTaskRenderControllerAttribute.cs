using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NpcTaskRenderControllerAttribute : Attribute
{
	public readonly string npcName;

	public NpcTaskRenderControllerAttribute(string npcName)
	{
		this.npcName = npcName;
	}
}
