using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct WaterHeightData
{
	[SerializeField]
	[Tooltip("高度为0则不生成水体")]
	[Range(0f, 1f)]
	public float waterBias;

	[SerializeField]
	public bool enableAirWall;
}
