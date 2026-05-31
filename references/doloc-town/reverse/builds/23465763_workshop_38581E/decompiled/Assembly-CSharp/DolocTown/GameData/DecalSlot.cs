using UnityEngine;

namespace DolocTown.GameData;

public class DecalSlot
{
	public readonly int Index;

	public readonly DecalSlotType SlotType;

	public readonly Vector2Int PixelPivot;

	public DecalSlot(int index, DecalSlotType slotType, Vector2Int pixelPivot)
	{
		Index = index;
		SlotType = slotType;
		PixelPivot = pixelPivot;
	}
}
