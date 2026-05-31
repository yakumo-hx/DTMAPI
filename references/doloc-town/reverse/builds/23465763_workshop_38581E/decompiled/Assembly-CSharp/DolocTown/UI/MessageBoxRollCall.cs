using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MessageBoxRollCall : DolocUiObject
{
	private readonly struct MessageRecord
	{
		public readonly string title;

		public readonly string subTitle;

		public readonly float duration;

		public readonly float waitTime;

		public readonly float fadeTime;

		public MessageRecord(string title, string subTitle, float duration, float waitTime, float fadeTime)
		{
			this.title = title;
			this.subTitle = subTitle;
			this.duration = duration;
			this.waitTime = waitTime;
			this.fadeTime = fadeTime;
		}
	}

	[SerializeField]
	public Image backgroundImg;

	[SerializeField]
	public Image seperatorImg;

	[SerializeField]
	public Text labelText;

	[SerializeField]
	public Text subLabelText;

	private readonly Queue<MessageRecord> queue = new Queue<MessageRecord>();

	private float _alphaValue;

	private Sequence _animation;

	private float alpha
	{
		get
		{
			return _alphaValue;
		}
		set
		{
			_alphaValue = value;
			DolocUtils.setAlpha(backgroundImg, value);
			DolocUtils.setAlpha(seperatorImg, value);
			DolocUtils.setAlpha(labelText, value);
			DolocUtils.setAlpha(subLabelText, value);
		}
	}

	public string title
	{
		get
		{
			return labelText.text;
		}
		set
		{
			labelText.text = value;
		}
	}

	public string subTitle
	{
		get
		{
			return subLabelText.text;
		}
		set
		{
			subLabelText.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		SetVisible(value: false);
	}

	public void Show(string title, string subTitle, float duration = 0.8f, float waitTime = 3f, float fadeTime = 0.3f)
	{
		if (_animation != null)
		{
			if (queue.Count < 5)
			{
				queue.Enqueue(new MessageRecord(title, subTitle, duration, waitTime, fadeTime));
			}
			return;
		}
		this.title = title;
		this.subTitle = subTitle;
		alpha = 0f;
		SetVisible(value: true);
		_animation = DOTween.Sequence();
		_animation.Join(DOTween.To(() => alpha, delegate(float v)
		{
			alpha = v;
		}, 1f, duration));
		float _wait = 0f;
		_animation.Append(DOTween.To(() => _wait, delegate(float v)
		{
			_wait = v;
		}, 1f, waitTime));
		_animation.Append(DOTween.To(() => alpha, delegate(float v)
		{
			alpha = v;
		}, 0f, fadeTime));
		_animation.OnComplete(delegate
		{
			_animation = null;
			if (queue.Count > 0)
			{
				MessageRecord messageRecord = queue.Dequeue();
				Show(messageRecord.title, messageRecord.subTitle, messageRecord.duration, messageRecord.waitTime, messageRecord.fadeTime);
			}
			else
			{
				SetVisible(value: false);
			}
		});
	}
}
