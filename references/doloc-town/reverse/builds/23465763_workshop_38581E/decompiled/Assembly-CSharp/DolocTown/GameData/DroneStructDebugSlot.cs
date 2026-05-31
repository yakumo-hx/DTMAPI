using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct DroneStructDebugSlot
{
	public DroneStructureVisibleSuitableType visibleSuitableType;

	public Vector2 visibleSuitableOffset;

	public Vector3 VisibleSuitableOffset => new Vector3(visibleSuitableOffset.x, visibleSuitableOffset.y, ZOffset(visibleSuitableType) - visibleSuitableOffset.y);

	private static float ZOffset(DroneStructureVisibleSuitableType type)
	{
		return type switch
		{
			DroneStructureVisibleSuitableType.Front => -0.0001f, 
			DroneStructureVisibleSuitableType.Back => 0.0001f, 
			_ => 0f, 
		};
	}

	public DroneStructDebugSlot(DroneStructureVisibleSuitableType visibleSuitableType, Vector3 visibleSuitableOffset)
	{
		this.visibleSuitableType = visibleSuitableType;
		this.visibleSuitableOffset = visibleSuitableOffset;
	}
}
