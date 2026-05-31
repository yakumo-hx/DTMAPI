using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class MessageBoxBase : DolocUiObject
{
	public readonly struct MessageRecord
	{
		public readonly Sprite icon;

		public readonly string message;

		public readonly float holdTime;

		public MessageRecord(string message, float holdTime, Sprite icon)
		{
			this.message = message;
			this.holdTime = holdTime;
			this.icon = icon;
		}

		public MessageRecord(string message, float holdTime)
		{
			this.message = message;
			this.holdTime = holdTime;
			icon = null;
		}
	}

	[SerializeField]
	protected Image imagePanel;

	[SerializeField]
	protected Image imageIcon;

	[SerializeField]
	protected TextMeshProUGUI textMessage;

	[Tooltip("最多可以缓存的消息数量")]
	[SerializeField]
	protected int maxQueueLength = 3;

	[Tooltip("消息框显示的时间")]
	[SerializeField]
	protected float showTime = 1f;

	[Tooltip("消息框消失的时间")]
	[SerializeField]
	protected float hideTime = 0.5f;

	[Tooltip("消息框弹出Ease")]
	[SerializeField]
	protected Ease ease = Ease.OutExpo;

	protected float originalAlpha;

	protected Tween tweenShow;

	protected Tween tweenHide;

	protected Queue<MessageRecord> queue;

	private float _alpha;

	protected float alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			_alpha = value;
			DolocUtils.setAlpha(imagePanel, value * originalAlpha);
			DolocUtils.setAlpha(imageIcon, value);
			DolocUtils.setAlpha(textMessage, value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		queue = new Queue<MessageRecord>(maxQueueLength);
		originalAlpha = imagePanel.color.a;
	}

	public bool Show(Sprite icon, string message, float holdTime = 2f)
	{
		if (base.gameObject.activeSelf)
		{
			if (queue.All((MessageRecord x) => x.message != message) && queue.Count < maxQueueLength)
			{
				queue.Enqueue(new MessageRecord(message, holdTime, icon));
			}
			return false;
		}
		SetVisible(value: true);
		_Show(icon, message, holdTime);
		return true;
	}

	protected void _Show(Sprite icon, string message, float holdTime)
	{
		_reset();
		imageIcon.sprite = icon;
		textMessage.text = DolocAPI.GetParsedKeystrokeText(DolocAPI.UserInput.DeviceType, message);
		tweenShow = createShowTween();
		tweenShow.OnComplete(delegate
		{
			StartCoroutine(_Wait(holdTime));
		});
	}

	protected IEnumerator _Wait(float time)
	{
		yield return new WaitForSeconds(time);
		if (queue.Count > 0)
		{
			MessageRecord messageRecord = queue.Dequeue();
			_Show(messageRecord.icon, messageRecord.message, messageRecord.holdTime);
		}
		else
		{
			_hide();
		}
	}

	protected void _hide()
	{
		tweenHide?.Kill();
		tweenHide = createHideTween();
		tweenHide.OnComplete(delegate
		{
			tweenShow?.Kill();
			tweenShow = null;
			SetVisible(value: false);
		});
	}

	protected abstract Tween createHideTween();

	protected abstract Tween createShowTween();

	protected abstract void _reset();
}
