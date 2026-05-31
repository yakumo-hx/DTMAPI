using System;
using System.Collections;
using UnityEngine;

namespace DolocTown;

public class ContinuesGoEffects : DolocRecyclableObject
{
	public Action<ContinuesGoEffects> recycle { get; set; }

	public void Play(Vector2 position, float duration)
	{
		base.transform.position = position;
		StartCoroutine(__Wait(duration));
	}

	private IEnumerator __Wait(float time)
	{
		yield return new WaitForSeconds(time);
		recycle(this);
	}
}
