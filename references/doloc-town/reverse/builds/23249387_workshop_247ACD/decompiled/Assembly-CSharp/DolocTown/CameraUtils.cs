using System;
using UnityEngine;

namespace DolocTown;

public static class CameraUtils
{
	public static Func<Vector2, Vector2> _GetCamConstraintFunc(Vector2 camSize, Vector2 camScenePosition, Vector2 camSceneSize)
	{
		Vector2 vector = camSize / 2f;
		Vector2 vector2 = camScenePosition + vector;
		Vector2 vector3 = camScenePosition + camSceneSize - vector;
		Vector2 xRange = new Vector2(vector2.x, vector3.x);
		Vector2 yRange = new Vector2(vector2.y, vector3.y);
		Vector2 defaultPosition = camScenePosition + vector;
		if (xRange.x <= yRange.x)
		{
			if (yRange.x <= yRange.y)
			{
				return (Vector2 pos) => defaultPosition;
			}
			return (Vector2 pos) => new Vector2(defaultPosition.x, Mathf.Clamp(pos.y, yRange.x, yRange.y));
		}
		if (yRange.x <= yRange.y)
		{
			return (Vector2 pos) => new Vector2(Mathf.Clamp(pos.x, xRange.x, xRange.y), defaultPosition.y);
		}
		return delegate(Vector2 pos)
		{
			float x = Mathf.Clamp(pos.x, xRange.x, xRange.y);
			float y = Mathf.Clamp(pos.y, yRange.x, yRange.y);
			return new Vector2(x, y);
		};
	}
}
