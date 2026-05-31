using System;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class HomePage : DolocUIPanel
{
	[SerializeField]
	private Image background;

	[SerializeField]
	public HomePageTextMenu textMenu;

	[SerializeField]
	public Image backgroundMask;

	[SerializeField]
	public CanvasGroup foregroundCanvasGroup;

	[SerializeField]
	public Text versionText;

	[SerializeField]
	private Color[] maskColors;

	private Color DefaultMaskColor;

	[SerializeField]
	private OuterLinkButtonGroup linkButtons;

	[SerializeField]
	private LanguageButtonGroup languageButtons;

	private Sequence showSequence;

	private Sequence fadeSequence;

	public bool isFocused
	{
		get
		{
			if (!textMenu.isFocused && !linkButtons.isFocused)
			{
				return languageButtons.isFocused;
			}
			return true;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		textMenu.Init();
		base.displayAnimType = UiPanelDisplayAnimType.FadeInOut;
		textMenu.displayAnimType = UiPanelDisplayAnimType.SoftPop;
		textMenu.displayAnimDuration = 1.2f;
		foreach (LinkButton slot in linkButtons.slots)
		{
			slot.button.onMove.AddListener(OnFunctionButtonMove);
		}
		foreach (LanguageButton slot2 in languageButtons.slots)
		{
			slot2.button.onMove.AddListener(OnFunctionButtonMove);
		}
		DefaultMaskColor = backgroundMask.color;
		foregroundCanvasGroup.gameObject.SetActive(value: false);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DolocAPI.HideItemBorder();
	}

	private void ChangeTest(int idx)
	{
		ChangeColorOfMask(idx);
		ChangeColorOfHomeLight(idx);
	}

	private void ChangeColorOfMask(int index)
	{
		if (maskColors.IsNullOrEmpty() || index < 0 || index >= maskColors.Length)
		{
			backgroundMask.color = DefaultMaskColor;
		}
		else
		{
			backgroundMask.color = maskColors[index];
		}
	}

	private void ChangeColorOfHomeLight(int index)
	{
		HomeLight homeLight = UnityEngine.Object.FindObjectOfType<HomeLight>();
		if (homeLight != null)
		{
			homeLight.ChangeColor(index);
		}
	}

	private void ChangeMaskColorAccordingToTime()
	{
		if (maskColors.IsNullOrEmpty())
		{
			backgroundMask.color = DefaultMaskColor;
			return;
		}
		if (maskColors.Length == 1)
		{
			backgroundMask.color = maskColors[0];
			return;
		}
		int hour = DateTime.Now.Hour;
		int num = maskColors.Length;
		int num2 = (int)((float)hour / 24f * (float)num);
		backgroundMask.color = maskColors[num2];
	}

	private void ChangeHomeLightColorAccordingToTime()
	{
		HomeLight homeLight = UnityEngine.Object.FindObjectOfType<HomeLight>();
		if (homeLight != null)
		{
			homeLight.ChangeColorAccordingToTime();
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		RefreshText();
		ChangeMaskColorAccordingToTime();
		ChangeHomeLightColorAccordingToTime();
		ShowBackground();
		foregroundCanvasGroup.alpha = 1f;
		foregroundCanvasGroup.gameObject.SetActive(value: true);
		fadeSequence?.Kill();
		fadeSequence = DOTween.Sequence();
		fadeSequence.Append(foregroundCanvasGroup.DOFade(0f, 0.5f).OnComplete(delegate
		{
			foregroundCanvasGroup.gameObject.SetActive(value: false);
		}));
	}

	private void ShowBackground(TweenCallback callback = null)
	{
		showSequence?.Kill();
		showSequence = DOTween.Sequence();
		MaterialSetter01 materialSetter = new MaterialSetter01(background.material, "_Progress")
		{
			value = 0f
		};
		showSequence.Join(materialSetter.PlayWait(0.5f, 1.2f, Ease.OutExpo));
		showSequence.OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public void BuildNavigation()
	{
		foreach (TextButton slot in textMenu.slots)
		{
			slot.button.onMove.RemoveAllListeners();
			slot.button.onMove.AddListener(OnTextButtonMove);
		}
	}

	private void OnTextButtonMove(MoveDirection direction)
	{
		switch (direction)
		{
		case MoveDirection.Left:
			linkButtons.SelectFirst();
			break;
		case MoveDirection.Right:
			languageButtons.SelectFirst();
			break;
		}
	}

	private void OnFunctionButtonMove(MoveDirection direction)
	{
		if (direction == MoveDirection.Left || direction == MoveDirection.Right)
		{
			textMenu.GetFocus();
		}
	}

	public void RefreshText()
	{
		versionText.text = DolocUtils.Format(DolocConfig.StaticTexts.UiTextDemoStatement, Application.version);
	}
}
