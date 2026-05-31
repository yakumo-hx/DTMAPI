using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct WaterConditionChecker
{
	[SerializeField]
	public ConditionGroupChecker conditionChecker;

	[SerializeField]
	public WaterHeightData heightData;
}
