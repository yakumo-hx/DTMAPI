using System;
using DolocTown.Config.TechTree;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct TechPointConfig
{
	[SerializeField]
	public TechPointType type;

	[SerializeField]
	public int count;
}
