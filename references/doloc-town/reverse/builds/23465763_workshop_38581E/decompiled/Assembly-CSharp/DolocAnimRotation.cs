using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DolocAnimRotation : MonoBehaviour
{
	[SerializeField]
	private float time = 1f;

	[SerializeField]
	private Ease ease = Ease.Linear;

	[SerializeField]
	private Vector2 offset = new Vector2(0f, 180f);

	[SerializeField]
	private float scaleTime = 1f;

	[SerializeField]
	private Ease scaleEase = Ease.Linear;

	[SerializeField]
	private float scaleMax = 1.2f;

	[SerializeField]
	private float scaleMin = 0.9f;

	private Tweener anim;

	private Vector3 ScaleMax;

	private Vector3 ScaleMin;

	private bool isMax;

	private void Start()
	{
		ScaleMax = new Vector3(scaleMax, scaleMax, 1f);
		ScaleMin = new Vector3(scaleMin, scaleMin, 1f);
		scale();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			play(offset.x);
		}
		else if (Input.GetKeyDown(KeyCode.G))
		{
			play(offset.y);
		}
	}

	private void scale()
	{
		if (isMax)
		{
			isMax = false;
			base.transform.DOScale(ScaleMin, scaleTime).SetEase(scaleEase).OnComplete(delegate
			{
				scale();
			});
		}
		else
		{
			isMax = true;
			base.transform.DOScale(ScaleMax, scaleTime).SetEase(scaleEase).OnComplete(delegate
			{
				scale();
			});
		}
	}

	private IEnumerator coroutineScale()
	{
		yield return new WaitForSeconds(scaleTime);
		scale();
	}

	private void play(float _offset)
	{
		if (anim != null)
		{
			anim.Kill();
		}
		base.transform.rotation = Quaternion.Euler(0f, 0f, offset.y - _offset);
		anim = base.transform.DORotate(new Vector3(0f, 0f, _offset), time).SetEase(ease).OnComplete(delegate
		{
			anim = null;
		});
	}
}
