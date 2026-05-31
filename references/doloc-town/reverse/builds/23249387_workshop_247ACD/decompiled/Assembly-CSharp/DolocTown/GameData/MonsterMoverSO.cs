using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public abstract class MonsterMoverSO
{
	[SerializeField]
	protected PathFinderSO _pathFinder = new PathFinderSOAir();

	[SerializeField]
	protected bool isDirectional = true;

	[SerializeField]
	[Range(0f, 30f)]
	protected int _aroundRange = 9;

	private IEnumerable<Type> AvailablePathFinders => typeof(PathFinderSO).GetSubTypes();

	public IPathFinderProto PathFinderProto => _pathFinder;

	public bool IsDirectional => isDirectional;

	public int AroundRange => _aroundRange;

	public abstract bool CreateProto(out MonsterMoverProto proto);
}
