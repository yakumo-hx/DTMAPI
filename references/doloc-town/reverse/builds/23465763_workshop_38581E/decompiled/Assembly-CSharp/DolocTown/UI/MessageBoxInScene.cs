using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MessageBoxInScene : DolocUiRecyclableObject
{
	[SerializeField]
	private Text textInfo;

	[SerializeField]
	private Image imageBackground;

	private float originAlpha;

	private Vector2 positionWS;

	public Action<MessageBoxInScene> Recycle { get; set; }

	public float Alpha
	{
		get
		{
			return textInfo.color.a;
		}
		set
		{
			DolocUtils.setAlpha(imageBackground, value * originAlpha);
			DolocUtils.setAlpha(textInfo, value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		originAlpha = imageBackground.color.a;
	}

	public void ShowAsWaiter(string content, Vector2 posWS, float durShow, float durWait, Ease scaleEase)
	{
		positionWS = posWS;
		textInfo.text = content;
		base.transform.localScale = new Vector3(0f, 1f, 1f);
		Alpha = 0f;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.transform.DOScaleX(1f, durShow).SetEase(scaleEase));
		sequence.Join(DOTween.To(() => Alpha, delegate(float x)
		{
			Alpha = x;
		}, 1f, durShow));
		sequence.OnComplete(delegate
		{
			StartCoroutine(WaitForHide(durWait));
		});
	}

	private IEnumerator WaitForHide(float time)
	{
		yield return new WaitForSeconds(time);
		Recycle?.Invoke(this);
	}

	public Action ShowAsManual(string content, Vector2 posWS, float durShow, Ease scaleEase)
	{
		positionWS = posWS;
		textInfo.text = content;
		base.transform.localScale = new Vector3(0f, 1f, 1f);
		Alpha = 0f;
		Sequence s = DOTween.Sequence();
		s.Join(base.transform.DOScaleX(1f, durShow).SetEase(scaleEase));
		s.Join(DOTween.To(() => Alpha, delegate(float x)
		{
			Alpha = x;
		}, 1f, durShow));
		return delegate
		{
			Recycle?.Invoke(this);
		};
	}

	private void LateUpdate()
	{
		base.position = DolocAPI.WorldToScreen(positionWS);
	}
}
