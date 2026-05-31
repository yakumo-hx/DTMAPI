using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MessageBoxLittle : DolocUiRecyclableObject
{
	[SerializeField]
	private float popDistance = 100f;

	[SerializeField]
	private float showTime = 0.7f;

	[SerializeField]
	private float hideTime = 0.3f;

	[SerializeField]
	private float holdTime = 1.4f;

	[SerializeField]
	private Ease ease = Ease.OutQuart;

	[SerializeField]
	private Image image;

	[SerializeField]
	private Text text;

	private Vector2 showPos;

	private Sequence sequenceShow;

	private Sequence sequenceHide;

	private bool isShowing;

	private readonly Queue<MessageBoxBase.MessageRecord> queue = new Queue<MessageBoxBase.MessageRecord>();

	private Vector2 hidePosTop => showPos + new Vector2(0f, popDistance * base.screenScaleY);

	private Vector2 hidePosBottom => showPos + new Vector2(0f, (0f - popDistance) * base.screenScaleY);

	private float alpha
	{
		get
		{
			return image.color.a;
		}
		set
		{
			DolocUtils.setAlpha(image, value);
			DolocUtils.setAlpha(text, value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		showPos = base.rectTransform.anchoredPosition;
	}

	public bool ShowMessage(string message, float holdTime = 2f)
	{
		if (isShowing)
		{
			if (queue.All((MessageBoxBase.MessageRecord x) => x.message != message))
			{
				queue.Enqueue(new MessageBoxBase.MessageRecord(message, holdTime));
			}
			return false;
		}
		isShowing = true;
		SetVisible(value: true);
		Reset();
		base.transform.SetAsLastSibling();
		text.text = message;
		sequenceShow.Kill();
		sequenceShow = DOTween.Sequence();
		sequenceShow.Join(base.rectTransform.DOAnchorPos(showPos, showTime).SetEase(ease));
		sequenceShow.Join(DOTween.To(() => alpha, delegate(float x)
		{
			alpha = x;
		}, 1f, showTime));
		sequenceShow.OnComplete(delegate
		{
			StartCoroutine(Wait(holdTime));
		});
		return true;
	}

	private IEnumerator Wait(float time)
	{
		yield return new WaitForSeconds(time);
		sequenceHide?.Kill();
		sequenceHide = DOTween.Sequence();
		sequenceHide.Join(base.rectTransform.DOAnchorPos(hidePosTop, showTime).SetEase(ease));
		sequenceHide.Join(DOTween.To(() => alpha, delegate(float x)
		{
			alpha = x;
		}, 0f, hideTime));
		sequenceHide.OnComplete(delegate
		{
			isShowing = false;
			if (queue.Count > 0)
			{
				MessageBoxBase.MessageRecord messageRecord = queue.Dequeue();
				ShowMessage(messageRecord.message, messageRecord.holdTime);
			}
			else
			{
				SetVisible(value: false);
			}
		});
	}

	protected void Reset()
	{
		base.rectTransform.anchoredPosition = hidePosBottom;
		alpha = 0f;
	}
}
