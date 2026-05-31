using DolocTown.Config.Equipment;
using DolocTown.GameData;

namespace DolocTown.Config;

public static class Converter
{
	public static DecalSlot[] ConvertDecalSlotData(this DecalSlotData[] datas)
	{
		if (datas.Length == 0)
		{
			return null;
		}
		DecalSlot[] array = new DecalSlot[datas.Length];
		for (int i = 0; i < datas.Length; i++)
		{
			array[i] = new DecalSlot(i, datas[i].SlotType, datas[i].PixelPivot);
		}
		return array;
	}
}
