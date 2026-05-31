using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class TerrainDataManager<T> : DataManager<T> where T : TerrainContent
{
	public TerrainDataManager()
	{
	}

	[JsonConstructor]
	protected TerrainDataManager(IndexList<T> datas)
		: base(datas)
	{
	}

	public void Shift(Vector2Int offset, Vector3 positionOffset)
	{
		foreach (T allData in base.AllDatas)
		{
			allData.ShiftTerrainContent(offset, positionOffset);
		}
	}
}
