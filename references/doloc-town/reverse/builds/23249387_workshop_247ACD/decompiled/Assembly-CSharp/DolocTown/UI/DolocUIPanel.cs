using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(CanvasGroup))]
public abstract class DolocUIPanel : DolocUiEntity, IView, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private Sequence sequence;

	[SerializeField]
	protected DolocButtonComponent closeButton;

	[SerializeField]
	public OperationTipInUI operationTip;

	private UnityEvent onCloseButtonClick = new UnityEvent();

	private bool origionBlocksRaycasts;

	[SerializeField]
	private bool _isWidget;

	private DolocUIPanel[] widgets = Array.Empty<DolocUIPanel>();

	protected CanvasGroup panelCanvasGroup;

	private float scrollDistance = 150f;

	private bool allowInteracte = true;

	[FormerlySerializedAs("fullScreen")]
	[SerializeField]
	public bool disableScaleDown;

	[FormerlySerializedAs("useScreenAdaptation")]
	[SerializeField]
	private bool disableScreenAdaption;

	[SerializeField]
	private UIAlignmentType showPositionPivot = UIAlignmentType.Center;

	[SerializeField]
	private UIAlignmentType screenAnchor = UIAlignmentType.Center;

	[SerializeField]
	private Vector2 showPositionOffset = Vector2.zero;

	[SerializeField]
	private UiPanelDisplayAnimType _displayAnimType;

	[SerializeField]
	private float _displayAnimDuration = 0.3f;

	private Vector2 anchoredShowPosition;

	public bool isRender { get; private set; }

	public bool inAnimation { get; private set; }

	public virtual bool redoDisplayAnimation => true;

	public virtual bool activeAllWidgetOnShow => true;

	public virtual UnityEvent OnCloseButtonClick => onCloseButtonClick;

	public bool isWidget => _isWidget;

	protected Vector2 anchoredHidePosition { get; set; }

	protected virtual bool useDefaultHidePosition => true;

	public UiPanelDisplayAnimType displayAnimType
	{
		get
		{
			return _displayAnimType;
		}
		set
		{
			_displayAnimType = value;
		}
	}

	public float displayAnimDuration
	{
		get
		{
			return _displayAnimDuration;
		}
		set
		{
			_displayAnimDuration = value;
		}
	}

	protected Vector2 screenResolution => DolocAPI.screenSize;

	public Vector2 originAnchoredPosition { get; private set; }

	public void SetCloseButtonVisible(bool value)
	{
		if (closeButton != null)
		{
			closeButton.gameObject.SetActive(value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		anchoredShowPosition = (disableScreenAdaption ? base.anchoredPosition : showPositionOffset);
		originAnchoredPosition = anchoredShowPosition;
		if (closeButton != null)
		{
			closeButton.onClick.AddListener(delegate
			{
				onCloseButtonClick.Invoke();
			});
		}
		if (operationTip != null)
		{
			operationTip.Init();
		}
		panelCanvasGroup = GetComponent<CanvasGroup>();
		origionBlocksRaycasts = panelCanvasGroup.blocksRaycasts;
		if (!isWidget)
		{
			SetVisible(value: false);
			widgets = (from x in GetComponentsInChildren<DolocUIPanel>(includeInactive: true)
				where x.isWidget
				select x).ToArray();
			DolocUIPanel[] array = widgets;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Init();
			}
		}
	}

	public void Show(bool useTween = true, Action call = null)
	{
		panelCanvasGroup.interactable = origionBlocksRaycasts && allowInteracte;
		panelCanvasGroup.blocksRaycasts = origionBlocksRaycasts && allowInteracte;
		if (isRender && !redoDisplayAnimation)
		{
			call?.Invoke();
			return;
		}
		isRender = true;
		OnStartShow();
		Display(visible: true, useTween, call).Forget();
		if (activeAllWidgetOnShow)
		{
			DolocUIPanel[] array = widgets;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Show();
			}
		}
	}

	public void Hide(bool useTween = true, Action call = null)
	{
		panelCanvasGroup.interactable = false;
		panelCanvasGroup.blocksRaycasts = false;
		if (!isRender && !redoDisplayAnimation)
		{
			call?.Invoke();
			return;
		}
		isRender = false;
		DolocUIPanel[] array = widgets;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Hide();
		}
		OnStartHide();
		Display(visible: false, useTween, call).Forget();
	}

	protected virtual void OnStartShow()
	{
	}

	protected virtual void OnFinishShow()
	{
	}

	protected virtual void OnStartHide()
	{
	}

	protected virtual void OnFinishHide()
	{
	}

	public void MoveToCenter(CenterType type)
	{
		switch (type)
		{
		case CenterType.Vertical:
			MoveToVCenter();
			break;
		case CenterType.Horizontal:
			MoveToHCenter();
			break;
		case CenterType.Both:
			MoveToHCenter();
			MoveToVCenter();
			break;
		}
	}

	public void SetCanvasGroupAlpha(float alpha)
	{
		panelCanvasGroup.alpha = (isRender ? alpha : 0f);
	}

	private async UniTaskVoid Display(bool visible, bool useTween, Action call)
	{
		if (disableScaleDown)
		{
			RevertScaleDown();
		}
		inAnimation = true;
		anchoredShowPosition = GetAnchoredShowPosition();
		anchoredHidePosition = (useDefaultHidePosition ? GetAnchoredHidePosition(visible, displayAnimType) : GetCustomHidePosition(visible, displayAnimType));
		if (sequence != null)
		{
			sequence.Kill();
		}
		sequence = DOTween.Sequence();
		if (visible)
		{
			panelCanvasGroup.alpha = 0f;
			SetVisible(value: true);
			base.anchoredPosition = anchoredHidePosition;
		}
		float num = (useTween ? displayAnimDuration : 0f);
		switch (displayAnimType)
		{
		case UiPanelDisplayAnimType.None:
			base.anchoredPosition = anchoredShowPosition;
			sequence.Join(panelCanvasGroup.DOFade(visible ? 1 : 0, 0f));
			sequence.AppendInterval(num);
			break;
		case UiPanelDisplayAnimType.FadeInOut:
			base.anchoredPosition = anchoredShowPosition;
			sequence.Join(panelCanvasGroup.DOFade(visible ? 1 : 0, num));
			break;
		case UiPanelDisplayAnimType.FromTop:
		case UiPanelDisplayAnimType.FromBottom:
		case UiPanelDisplayAnimType.FromLeft:
		case UiPanelDisplayAnimType.FromRight:
		case UiPanelDisplayAnimType.ScrollUp:
			sequence.Join(panelCanvasGroup.DOFade(visible ? 1 : 0, num));
			sequence.Join(base.rectTransform.DOAnchorPos(visible ? anchoredShowPosition : anchoredHidePosition, num));
			sequence.SetEase(Ease.OutExpo);
			break;
		case UiPanelDisplayAnimType.SoftPop:
			sequence.Join(panelCanvasGroup.DOFade(visible ? 1 : 0, num).SetEase(Ease.InOutQuad));
			sequence.Join(base.rectTransform.DOAnchorPos(visible ? anchoredShowPosition : anchoredHidePosition, num).SetEase(Ease.OutBack));
			break;
		case UiPanelDisplayAnimType.HorizontalScroll:
			sequence.Join(panelCanvasGroup.DOFade(visible ? 1 : 0, num).SetEase(Ease.InOutQuad));
			sequence.Join(base.rectTransform.DOAnchorPos(visible ? anchoredShowPosition : anchoredHidePosition, num).SetEase(Ease.OutBack));
			break;
		}
		if (Time.timeScale != 0f)
		{
			sequence.timeScale = 1f / Time.timeScale;
		}
		sequence.OnComplete(delegate
		{
			inAnimation = false;
			if (visible)
			{
				OnFinishShow();
			}
			else
			{
				if (!isWidget)
				{
					SetVisible(value: false);
				}
				OnFinishHide();
			}
			sequence = null;
			call?.Invoke();
		});
	}

	private void MoveToHCenter()
	{
		DolocUtils.moveToHCenter(base.rectTransform);
	}

	private void MoveToVCenter()
	{
		DolocUtils.moveToVCenter(base.rectTransform);
	}

	public new void RebuildLayout()
	{
		bool activeSelf = base.gameObject.activeSelf;
		float alpha = panelCanvasGroup.alpha;
		if (!activeSelf)
		{
			SetVisible(value: true);
			panelCanvasGroup.alpha = 0f;
		}
		LayoutGroup[] componentsInChildren = GetComponentsInChildren<LayoutGroup>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(componentsInChildren[i].transform as RectTransform);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.rectTransform);
		SetVisible(activeSelf);
		panelCanvasGroup.alpha = alpha;
	}

	public void EnableInteract(bool value)
	{
		allowInteracte = value;
		if (isRender)
		{
			panelCanvasGroup.interactable = value;
			panelCanvasGroup.blocksRaycasts = value;
		}
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
	}

	private Vector2 GetAnchoredPosition(Vector2 worldPos)
	{
		return (Vector2)base.rectTransform.parent.InverseTransformPoint(worldPos) - base.parentRect.rect.size * base.rectTransform.anchorMin;
	}

	public void SetShowPosition(Vector2 screenPos)
	{
		anchoredShowPosition = GetAnchoredPosition(screenPos);
	}

	public void SetAnchoredShowPosition(float space, UIAlignmentType screenAnchor)
	{
		Vector2 pivotVector = DolocUtils.GetPivotVector(this.screenAnchor);
		Vector2 offset = ((screenAnchor == UIAlignmentType.Center) ? new Vector2(space, space) : new Vector2(Lambda(pivotVector.x) * space, Lambda(pivotVector.y) * space));
		SetAnchoredShowPosition(offset, screenAnchor, screenAnchor);
		static float Lambda(float x)
		{
			return (x <= 0f) ? 1 : ((x >= 1f) ? (-1) : 0);
		}
	}

	public void SetAnchoredShowPosition(Vector2 offset, UIAlignmentType screenAnchor)
	{
		SetAnchoredShowPosition(offset, screenAnchor, screenAnchor);
	}

	public void SetAnchoredShowPosition(Vector2 offset, UIAlignmentType panelPivot, UIAlignmentType screenAnchor)
	{
		anchoredShowPosition = offset;
		showPositionPivot = panelPivot;
		this.screenAnchor = screenAnchor;
		RefreshAnchorAndPivot();
	}

	private void RefreshAnchorAndPivot()
	{
		if (!disableScreenAdaption)
		{
			base.rectTransform.SetAnchorAndPivot(showPositionPivot);
		}
	}

	private Vector2 GetAnchoredShowPosition()
	{
		return anchoredShowPosition;
	}

	public Vector2 GetPositionByAnchor(UIAlignmentType anchorType)
	{
		return (Vector2)base.parentRect.TransformPoint(base.rectTransform.anchoredPosition) + base.parentRect.rect.size * base.rectTransform.anchorMin - base.pivot * base.size + DolocUtils.GetPivotVector(anchorType) * base.size + (anchoredShowPosition - base.anchoredPosition);
	}

	protected Vector2 GetAnchoredHidePosition(bool visible, UiPanelDisplayAnimType type)
	{
		return type switch
		{
			UiPanelDisplayAnimType.FromTop => anchoredShowPosition + new Vector2(0f, base.height), 
			UiPanelDisplayAnimType.FromBottom => anchoredShowPosition + new Vector2(0f, 0f - base.height), 
			UiPanelDisplayAnimType.FromLeft => anchoredShowPosition + new Vector2(0f - base.width, 0f), 
			UiPanelDisplayAnimType.FromRight => anchoredShowPosition + new Vector2(base.width, 0f), 
			UiPanelDisplayAnimType.ScrollUp => anchoredShowPosition + ((!visible) ? 1 : (-1)) * new Vector2(0f, scrollDistance), 
			UiPanelDisplayAnimType.SoftPop => anchoredShowPosition + new Vector2(0f, 0f - base.height), 
			UiPanelDisplayAnimType.HorizontalScroll => anchoredShowPosition + new Vector2(base.width, 0f), 
			_ => anchoredShowPosition, 
		};
	}

	protected virtual Vector2 GetCustomHidePosition(bool visible, UiPanelDisplayAnimType type)
	{
		return base.anchoredPosition;
	}

	public override Vector2 GetAdaptionPosition(Vector2 pos)
	{
		if (disableScaleDown)
		{
			RevertScaleDown();
		}
		return base.GetAdaptionPosition(pos);
	}

	protected void SetLeftAndRightLayout(DolocUIPanel leftPanel, DolocUIPanel rightPanel, int space = 60)
	{
		RebuildLayout();
		leftPanel.RebuildLayout();
		rightPanel.RebuildLayout();
		RebuildLayout();
		float space2 = (DolocAPI.screenSize.x - leftPanel.width - rightPanel.width - (float)space) / 2f;
		leftPanel.displayAnimType = UiPanelDisplayAnimType.FromLeft;
		leftPanel.SetAnchoredShowPosition(space2, UIAlignmentType.LeftMiddle);
		rightPanel.displayAnimType = UiPanelDisplayAnimType.FromRight;
		rightPanel.SetAnchoredShowPosition(space2, UIAlignmentType.RightMiddle);
	}
}
