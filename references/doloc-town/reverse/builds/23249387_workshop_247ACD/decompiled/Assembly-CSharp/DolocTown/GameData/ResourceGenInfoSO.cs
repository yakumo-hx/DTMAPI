using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public class ResourceGenInfoSO
{
	[SerializeField]
	public TerrainConstraintSO terrainConstraint;

	[SerializeField]
	public bool shouldGen;

	public ResourceGenInfoSO(TerrainConstraintSO terrainConstraint)
		: this(terrainConstraint, shouldGen: true)
	{
	}

	public ResourceGenInfoSO(TerrainConstraintSO terrainConstraint, bool shouldGen)
	{
		this.terrainConstraint = terrainConstraint;
		this.shouldGen = shouldGen;
	}

	public ResourceGenInfoProto CreateProto()
	{
		return new ResourceGenInfoProto(terrainConstraint.Proto, shouldGen);
	}
}
