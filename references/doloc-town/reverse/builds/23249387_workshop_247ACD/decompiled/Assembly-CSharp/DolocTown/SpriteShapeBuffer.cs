using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class SpriteShapeBuffer
{
	protected static SpriteShapeBuffer instance;

	private Dictionary<string, Vector2[]> resourceShapes = new Dictionary<string, Vector2[]>();

	protected static SpriteShapeBuffer Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new SpriteShapeBuffer();
			}
			return instance;
		}
	}

	public static Vector2[] _GetSpriteShape(Sprite sprite)
	{
		return Instance.GetSpriteShape(sprite);
	}

	public Vector2[] GetSpriteShape(Sprite sprite)
	{
		if (sprite == null)
		{
			return null;
		}
		if (resourceShapes.TryGetValue(sprite.name, out var value))
		{
			return value;
		}
		if (sprite.GetPhysicsShapeCount() == 0)
		{
			return null;
		}
		List<Vector2> list = new List<Vector2>();
		sprite.GetPhysicsShape(0, list);
		value = list.ToArray();
		resourceShapes.Add(sprite.name, value);
		return value;
	}

	public void FlushSpriteShapeBuffer()
	{
		resourceShapes.Clear();
	}
}
