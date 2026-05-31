using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public class MonsterGenInfoSO
{
	[SerializeField]
	protected Vector2PositionList[] airPoints;

	[SerializeField]
	protected Vector2PositionList[] groundPoints;

	[SerializeField]
	protected bool shouldGen;

	public static MonsterGenInfoSO Empty => new MonsterGenInfoSO(null, null, shouldGen: false);

	public MonsterGenInfoProto Proto => new MonsterGenInfoProto(airPoints, groundPoints, shouldGen);

	public MonsterGenInfoSO(Vector2PositionList[] airPoints, Vector2PositionList[] groundPoints, bool shouldGen)
	{
		this.airPoints = airPoints;
		this.groundPoints = groundPoints;
		this.shouldGen = shouldGen;
	}
}
