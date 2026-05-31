using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class TerrainContentAttribute : Attribute
{
	public readonly TerrainLayerName layerName;

	public TerrainContentAttribute(TerrainLayerName layerName)
	{
		this.layerName = layerName;
	}
}
