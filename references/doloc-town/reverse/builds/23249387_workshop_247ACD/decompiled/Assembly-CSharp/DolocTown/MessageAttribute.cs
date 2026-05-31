using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MessageAttribute : Attribute
{
	public readonly GameEventType eventType;

	public MessageAttribute(GameEventType eventType)
	{
		this.eventType = eventType;
	}
}
