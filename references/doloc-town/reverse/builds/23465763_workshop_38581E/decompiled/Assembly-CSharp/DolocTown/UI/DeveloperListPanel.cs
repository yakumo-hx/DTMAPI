using System.Linq;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DeveloperListPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	private Text[] playerTestersContents;

	[SerializeField]
	private ScrollRect _scrollRect;

	[SerializeField]
	private CanvasGroup contentCanvasGroup;

	private bool shouldAutoScroll;

	private Sequence sequence;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta
	{
		get
		{
			if (DolocAPI.UserInput.DeviceType != 0)
			{
				return 0.002f;
			}
			return 0.01f;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		SetVisible(value: false);
		InitPlayerTesters();
	}

	private void InitPlayerTesters()
	{
		if (playerTestersContents.IsNullOrEmpty())
		{
			return;
		}
		int num = playerTestersContents.Length;
		StringBuilder[] array = new StringBuilder[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new StringBuilder();
		}
		TextAsset textAsset = Resources.Load<TextAsset>("Other/beta_testers");
		if (textAsset != null && !textAsset.text.IsNullOrEmpty())
		{
			string[] array2 = (from x in textAsset.text.Split('\n')
				where !x.IsNullOrEmpty()
				select x).ToArray();
			for (int j = 0; j < array2.Length; j++)
			{
				string text = array2[j].Trim();
				array[j % num].Append(text + "\n");
			}
		}
		for (int k = 0; k < num; k++)
		{
			playerTestersContents[k].text = array[k].ToString().Trim();
		}
	}

	public void AutoScroll(float deltaTime)
	{
		if (shouldAutoScroll && base.isRender && !base.inAnimation)
		{
			if (_scrollRect.verticalScrollbar.value > 0f)
			{
				_scrollRect.verticalScrollbar.value = Mathf.Clamp01(_scrollRect.verticalScrollbar.value - deltaTime * 0.02f);
				return;
			}
			_scrollRect.verticalScrollbar.value = 0f;
			shouldAutoScroll = false;
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		RebuildLayout();
		shouldAutoScroll = true;
		contentCanvasGroup.alpha = 0f;
		sequence?.Kill();
		DolocAPI.DelayFrame(delegate
		{
			_scrollRect.verticalScrollbar.value = 1f;
		});
		closeButton.gameObject.SetActive(value: false);
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Join(contentCanvasGroup.DOFade(1f, 1.5f));
		closeButton.gameObject.SetActive(value: true);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		shouldAutoScroll = false;
	}

	public void OnMove()
	{
	}
}
