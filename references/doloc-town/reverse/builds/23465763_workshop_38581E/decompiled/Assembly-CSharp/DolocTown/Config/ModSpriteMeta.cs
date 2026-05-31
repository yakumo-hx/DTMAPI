using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.Config;

[JsonObject(MemberSerialization.OptIn)]
public class ModSpriteMeta
{
	[JsonProperty("pixels_per_unit")]
	public float pixelsPerUnit = 8f;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	public Vector2Int pivot;

	[JsonConstructor]
	public ModSpriteMeta(Vector2Int pivot, float pixelsPerUnit)
	{
		this.pivot = pivot;
		this.pixelsPerUnit = pixelsPerUnit;
	}

	public Vector2 CalNormalizedPivot(int width, int height)
	{
		if (width == 0 || height == 0)
		{
			return Vector2.zero;
		}
		return new Vector2((float)pivot.x / (float)width, (float)pivot.y / (float)height);
	}
}
