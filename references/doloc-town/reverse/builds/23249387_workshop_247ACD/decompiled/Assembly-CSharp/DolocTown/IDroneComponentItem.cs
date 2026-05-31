using System.Collections.Generic;
using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public interface IDroneComponentItem
{
	private static Dictionary<string, Sprite> _spriteCache;

	string ComponentId { get; }

	bool IsDroneComponentValid { get; }

	ComponentType ComponentType { get; }

	string SkillId { get; }

	Sprite ComponentSprite { get; }

	Vector2Int ComponentPivot { get; }

	string ComponentTitle { get; }

	string ComponentDescription { get; }

	Sprite PaddingSprite => GetComponentSprite(this);

	static Sprite GetComponentSprite(IDroneComponentItem componentItem)
	{
		if (componentItem == null)
		{
			return null;
		}
		if (_spriteCache.TryGetValue(componentItem.ComponentId, out var value))
		{
			return value;
		}
		Sprite sprite = TextureUtils.PaddingSprite(componentItem.ComponentSprite, 1);
		_spriteCache[componentItem.ComponentId] = sprite;
		return sprite;
	}

	static IDroneComponentItem()
	{
		_spriteCache = new Dictionary<string, Sprite>();
	}
}
