using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DebugCrops : MonoBehaviour
{
	[SerializeField]
	private Sprite badSprite;

	[SerializeField]
	private Vector2 intervalRange = new Vector2(0.3f, 0.75f);

	private Dictionary<SpriteRenderer, Sprite> originSprites;

	private SpriteRenderer[] _loadedSrs;

	private bool isPlaying;

	private int index;

	private RSTimer timer;

	private SpriteRenderer[] _spriteRenderers
	{
		get
		{
			SpriteRenderer[] componentsInChildren = GetComponentsInChildren<SpriteRenderer>();
			if (originSprites == null)
			{
				originSprites = new Dictionary<SpriteRenderer, Sprite>();
				SpriteRenderer[] array = componentsInChildren;
				foreach (SpriteRenderer spriteRenderer in array)
				{
					if (!(spriteRenderer.sprite == null))
					{
						originSprites.Add(spriteRenderer, spriteRenderer.sprite);
					}
				}
			}
			Array.Sort(componentsInChildren, (SpriteRenderer a, SpriteRenderer b) => a.transform.position.x.CompareTo(b.transform.position.x));
			return componentsInChildren;
		}
	}

	public void StartWait(bool value)
	{
		SpriteRenderer[] array = _spriteRenderers;
		if (!value)
		{
			array = array.Reverse().ToArray();
		}
		_loadedSrs = array;
		isPlaying = true;
		timer = new RSTimer(intervalRange.Random());
		index = 0;
		foreach (KeyValuePair<SpriteRenderer, Sprite> originSprite in originSprites)
		{
			originSprite.Deconstruct(out var key, out var value2);
			SpriteRenderer spriteRenderer = key;
			Sprite sprite = value2;
			spriteRenderer.sprite = sprite;
		}
	}

	public void Update()
	{
		if (!isPlaying)
		{
			return;
		}
		if (index < _loadedSrs.Length)
		{
			if (timer.Tick(Time.fixedDeltaTime))
			{
				_loadedSrs[index++].GrowToNext(badSprite);
				timer.SetInterval(intervalRange.Random());
			}
		}
		else
		{
			isPlaying = false;
		}
	}
}
