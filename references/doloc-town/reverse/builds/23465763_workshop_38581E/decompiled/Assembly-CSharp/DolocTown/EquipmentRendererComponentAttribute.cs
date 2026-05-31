using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EquipmentRendererComponentAttribute : Attribute
{
	public Type rendererType;

	public string containerName;

	public EquipmentRendererComponentAttribute(Type type, string containerName)
	{
		rendererType = type;
		this.containerName = containerName;
	}
}
