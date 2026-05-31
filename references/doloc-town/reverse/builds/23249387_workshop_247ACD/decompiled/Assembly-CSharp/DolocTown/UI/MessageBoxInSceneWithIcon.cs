using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MessageBoxInSceneWithIcon : DolocUiRecyclableObject
{
	[SerializeField]
	private Image imageBackground;

	[SerializeField]
	private Image iconHolder;

	[SerializeField]
	private Text textHolder;

	private float originAlpha;

	private Vector2 positionWS;

	public Action<MessageBoxInSceneWithIcon> Recycle { get; set; }

	public float Alpha
	{
		get
		{
			return textHolder.color.a;
		}
		set
		{
			DolocUtils.setAlpha(imageBackground, value * originAlpha);
			DolocUtils.setAlpha(textHolder, value);
		}
	}

	public Sprite Icon
	{
		get
		{
			return iconHolder.sprite;
		}
		set
		{
			LayoutElement component = iconHolder.GetComponent<LayoutElement>();
			if (value == null)
			{
				iconHolder.sprite = null;
				component.preferredHeight = 0f;
				component.preferredWidth = 0f;
			}
			else
			{
				iconHolder.sprite = value;
				component.preferredWidth = value.rect.width * 4f;
				component.preferredHeight = value.rect.height * 4f;
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		originAlpha = imageBackground.color.a;
	}

	public override void OnReuse()
	{
		base.OnReuse();
		base.transform.localScale = new Vector3(0f, 1f, 1f);
		Alpha = 0f;
	}

	private IEnumerator WaitForChange(Action callback)
	{
		yield return new WaitForEndOfFrame();
		callback();
	}

	private IEnumerator WaitForHide(float time)
	{
		yield return new WaitForSeconds(time);
		Recycle?.Invoke(this);
	}

	public void ShowAsWaiter(string content, Sprite icon, Vector2 posWS, float durShow, float durWait, Ease scaleEase)
	{
		positionWS = posWS;
		textHolder.text = content;
		Icon = icon;
		IEnumerator routine = WaitForChange(delegate
		{
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
		});
		StartCoroutine(routine);
	}

	public Action ShowAsManual(string content, Sprite icon, Vector2 posWS, float durShow, Ease scaleEase)
	{
		positionWS = posWS;
		textHolder.text = content;
		Icon = icon;
		IEnumerator routine = WaitForChange(delegate
		{
			Sequence s = DOTween.Sequence();
			s.Join(base.transform.DOScaleX(1f, durShow).SetEase(scaleEase));
			s.Join(DOTween.To(() => Alpha, delegate(float x)
			{
				Alpha = x;
			}, 1f, durShow));
		});
		StartCoroutine(routine);
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
