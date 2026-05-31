using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MessageBoxNode : DolocUiObject
{
	[SerializeField]
	private Image imageIcon;

	[SerializeField]
	private Text textMessage;

	[SerializeField]
	private Ease ease = Ease.OutBack;

	[SerializeField]
	private float showTime = 0.5f;

	[SerializeField]
	private float hideTime = 0.5f;

	[SerializeField]
	private int maxQueueLength = 3;

	private Sequence currentAnim;

	private Queue<MessageBoxBase.MessageRecord> queue = new Queue<MessageBoxBase.MessageRecord>();

	private Vector2 showAnchoredPos;

	private Vector2 hideAnchoredPos => new Vector2((0f - base.width) * 1.5f, showAnchoredPos.y);

	protected override void __Init()
	{
		base.__Init();
		showAnchoredPos = base.anchoredPosition;
		SetVisible(value: false);
	}

	public bool Show(Sprite icon, string message)
	{
		float uiNodeMessageHoldDuration = DolocAPI.GlobalParameter.UiNodeMessageHoldDuration;
		if (currentAnim != null)
		{
			if (queue.Count < maxQueueLength)
			{
				queue.Enqueue(new MessageBoxBase.MessageRecord(message, uiNodeMessageHoldDuration, icon));
				return true;
			}
			return false;
		}
		_Show(icon, message, uiNodeMessageHoldDuration);
		return true;
	}

	private void _Show(Sprite icon, string message, float holdTime)
	{
		SetVisible(value: true);
		imageIcon.sprite = icon;
		textMessage.text = message;
		RebuildLayout();
		base.rectTransform.anchoredPosition = hideAnchoredPos;
		currentAnim = DOTween.Sequence();
		currentAnim.Join(base.rectTransform.DOAnchorPos(showAnchoredPos, showTime).SetEase(ease));
		currentAnim.AppendInterval(holdTime);
		currentAnim.Append(base.rectTransform.DOAnchorPos(hideAnchoredPos, hideTime));
		Sequence sequence = currentAnim;
		sequence.onComplete = (TweenCallback)Delegate.Combine(sequence.onComplete, (TweenCallback)delegate
		{
			currentAnim = null;
			if (queue.Count > 0)
			{
				MessageBoxBase.MessageRecord messageRecord = queue.Dequeue();
				_Show(messageRecord.icon, messageRecord.message, messageRecord.holdTime);
			}
			else
			{
				SetVisible(value: false);
			}
		});
	}

	private void test()
	{
		Init();
		Show(LocSprites.UI_INFOICON_STAR, "测试消息");
	}
}
