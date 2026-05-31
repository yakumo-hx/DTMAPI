using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class EmotionManager
{
	private readonly NashObjectPoolEx<EmotionRenderer> pool;

	private readonly Dictionary<Transform, EmotionRenderer> renderers;

	private readonly Dictionary<Transform, float> lastRaisingTime = new Dictionary<Transform, float>();

	public Transform container { get; private set; }

	public EmotionManager(Transform container)
	{
		this.container = container;
		renderers = new Dictionary<Transform, EmotionRenderer>();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_EMOTIONRENDERER);
		pool = new NashObjectPoolEx<EmotionRenderer>(asset, container);
		pool.OnCreate = delegate(EmotionRenderer R)
		{
			R.recycle = pool.Recycle;
		};
		pool.OnRecycle = delegate(EmotionRenderer R)
		{
			if (renderers.ContainsKey(R.target))
			{
				renderers.Remove(R.target);
			}
		};
	}

	public void Clear(Transform target)
	{
		if (renderers.TryGetValue(target, out var value))
		{
			value.Clear();
		}
	}

	public void Raise(Transform transform, EmotionName emotion)
	{
		EmotionRenderer emotionRenderer;
		if (renderers.TryGetValue(transform, out var value))
		{
			emotionRenderer = value;
		}
		else
		{
			emotionRenderer = pool.Next;
			renderers.Add(transform, emotionRenderer);
		}
		Sprite emotion2 = DolocAPI.LoadSprite("ui_icon_emo" + emotion.ToString().ToLower());
		emotionRenderer.Raise(transform, emotion2);
	}

	public void Raise(Transform transform, EmotionName emotionName, float interval)
	{
		if (!lastRaisingTime.ContainsKey(transform))
		{
			lastRaisingTime.Add(transform, Time.time);
			Raise(transform, emotionName);
			return;
		}
		float num = lastRaisingTime[transform];
		float time = Time.time;
		if (time - num > interval)
		{
			lastRaisingTime[transform] = time;
			Raise(transform, emotionName);
		}
	}
}
