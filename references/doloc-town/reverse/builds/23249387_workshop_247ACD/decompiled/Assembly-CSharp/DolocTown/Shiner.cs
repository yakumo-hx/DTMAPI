using System.Collections;
using UnityEngine;

namespace DolocTown;

public class Shiner
{
	private readonly SpriteRenderer sr;

	private Coroutine coroutine;

	private Material originalMat;

	private Color originColor;

	private bool IsShine => coroutine != null;

	public Shiner(SpriteRenderer sr)
	{
		this.sr = sr;
	}

	public void Raise(Material shineMat, float time)
	{
		if (!(sr == null))
		{
			if (IsShine)
			{
				DolocAPI.StopCoroutine(coroutine);
				sr.sharedMaterial = originalMat;
				sr.color = originColor;
				coroutine = null;
			}
			originColor = sr.color;
			originalMat = sr.sharedMaterial;
			sr.sharedMaterial = shineMat;
			sr.color = Color.white;
			coroutine = DolocAPI.StartCoroutine(Wait(time));
		}
	}

	private IEnumerator Wait(float time)
	{
		yield return new WaitForSeconds(time);
		if (sr != null)
		{
			sr.sharedMaterial = originalMat;
			sr.color = originColor;
		}
		coroutine = null;
	}
}
