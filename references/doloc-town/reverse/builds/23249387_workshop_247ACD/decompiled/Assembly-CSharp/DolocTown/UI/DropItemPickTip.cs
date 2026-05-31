using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DropItemPickTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Transform content;

	[SerializeField]
	private Image imgIcon;

	[SerializeField]
	private Text textCount;

	[SerializeField]
	private float showTime = 0.6f;

	[SerializeField]
	private float hideTime = 0.2f;

	[SerializeField]
	private float holdTime = 4f;

	[SerializeField]
	private float space = 12f;

	[SerializeField]
	private Ease ease = Ease.OutBack;

	private Sequence sequence;

	[HideInInspector]
	public UnityEvent<DropItemPickTip> onComplete = new UnityEvent<DropItemPickTip>();

	public string id;

	private int count;

	private string title;

	private Vector3 hiddenPos;

	public bool idle { get; private set; }

	public string itemName { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		idle = true;
		hiddenPos = content.localPosition + new Vector3(base.width + space, 0f);
		content.localPosition = hiddenPos;
	}

	public void Show(string itemName, Sprite icon, string title, int count = 1)
	{
		this.itemName = itemName;
		this.title = title;
		this.count = count;
		imgIcon.sprite = icon;
		textCount.text = $"{title}+{count}";
		content.localPosition = hiddenPos;
		StartAnimation();
	}

	public void ShowContinues(int pickCount = 1)
	{
		count += pickCount;
		textCount.text = $"{title}+{count}";
		StartAnimation();
	}

	private void StartAnimation()
	{
		idle = false;
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Join(content.DOLocalMove(Vector3.zero, showTime).SetEase(ease));
		sequence.AppendInterval(holdTime);
		sequence.Append(content.DOLocalMove(hiddenPos, hideTime).SetEase(ease));
		sequence.OnComplete(EndAnimation);
	}

	private void EndAnimation()
	{
		idle = true;
		onComplete?.Invoke(this);
	}
}
