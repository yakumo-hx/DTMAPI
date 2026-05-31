using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct ExpressDronePerformance
{
	[SerializeField]
	public Vector2 maxSpeed;

	[SerializeField]
	public Vector2 acceleration;

	[SerializeField]
	public float maxRotation;

	private void OnMaxSpeedChanged()
	{
		maxSpeed.x = Mathf.Max(0.01f, maxSpeed.x);
		maxSpeed.y = Mathf.Max(0.01f, maxSpeed.y);
	}
}
