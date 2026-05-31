using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationDecorator : Vegetation
{
	[JsonProperty]
	private int skinIdx;

	private VegetationFuncDecorator Func => (VegetationFuncDecorator)proto.Function;

	public override Sprite CurrentSprite => Func.Skins[skinIdx].Asset;

	public VegetationDecorator(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
		skinIdx = Random.Range(0, Func.Skins.Length);
	}

	[JsonConstructor]
	public VegetationDecorator(int index, Vector2Int anchor, Vector3 position, string vegetationName, int skinIdx)
		: base(index, anchor, position, vegetationName)
	{
		this.skinIdx = Mathf.Clamp(skinIdx, 0, Func.Skins.Length - 1);
	}
}
