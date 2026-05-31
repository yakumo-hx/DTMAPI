using System;
using RedSaw;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocButtonComponent : Button, ISelectable, IClickable
{
	private RectTransform _rectTransform;

	private RSTimer leftLongClickTimer;

	private RSTimer rightLongClickTimer;

	private Timer leftContinuesClickTimer;

	private Timer rightContinuesClickTimer;

	private bool shouldWaitForLeftLongClick;

	private bool shouldWaitForLeftContinuesClick;

	private bool hasLeftClickBeenInvoked;

	private bool shouldWaitForRightLongClick;

	private bool shouldWaitForRightContinuesClick;

	private bool hasRightClickBeenInvoked;

	[HideInInspector]
	public UnityEvent onSelect { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onDeselect { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onPointerEnter { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onPointerExit { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onPointerDown { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onPointerUp { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onLeftClick { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onLeftLongClick { get; } = new UnityEvent();


	[HideInInspector]
	public Func<bool> onLeftContinuesClick { get; set; }

	[HideInInspector]
	public UnityEvent onRightClick { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onRightLongClick { get; } = new UnityEvent();


	[HideInInspector]
	public Func<bool> onRightContinuesClick { get; set; }

	[HideInInspector]
	public UnityEvent onAssistLeftClick { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onAssistRightClick { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent<MoveDirection> onMove { get; } = new UnityEvent<MoveDirection>();


	public RectTransform rectTransform
	{
		get
		{
			if (_rectTransform == null && (object)_rectTransform == null)
			{
				_rectTransform = GetComponent<RectTransform>();
			}
			return _rectTransform;
		}
	}

	[HideInInspector]
	public static ClickType latestClickType { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		leftLongClickTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);
		rightLongClickTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);
		leftContinuesClickTimer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);
		rightContinuesClickTimer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		onSelect.Invoke();
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		onDeselect.Invoke();
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		onPointerEnter.Invoke();
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		onPointerExit.Invoke();
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		if (base.interactable)
		{
			latestClickType = ClickType.Mouse;
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				base.onClick?.Invoke();
			}
		}
	}

	public void FireClick(bool fireSelect = true, bool ignoreActiveState = true)
	{
		if ((ignoreActiveState || IsActive()) && IsInteractable())
		{
			if (fireSelect)
			{
				onSelect.Invoke();
			}
			base.onClick?.Invoke();
			onLeftClick.Invoke();
			DoStateTransition(SelectionState.Pressed, instant: true);
			DoStateTransition(base.currentSelectionState, instant: false);
		}
	}

	public override void OnMove(AxisEventData eventData)
	{
		base.OnMove(eventData);
		onMove.Invoke(eventData.moveDir);
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		latestClickType = ClickType.Key;
		onLeftClick.Invoke();
		base.OnSubmit(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		onPointerDown.Invoke();
		latestClickType = ClickType.Mouse;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			EventSystem.current?.SetSelectedGameObject(base.gameObject);
		}
		if (DolocAPI.UserInput.BaseAssistSplitInProgress)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				onAssistLeftClick.Invoke();
			}
			else if (eventData.button == PointerEventData.InputButton.Right)
			{
				onAssistRightClick.Invoke();
			}
			return;
		}
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			shouldWaitForLeftLongClick = false;
			shouldWaitForLeftContinuesClick = false;
			hasLeftClickBeenInvoked = false;
			if (onLeftLongClick != null && leftLongClickTimer != null)
			{
				shouldWaitForLeftLongClick = true;
				leftLongClickTimer.Reset();
			}
			else
			{
				onLeftClick.Invoke();
				hasLeftClickBeenInvoked = true;
			}
			if (onLeftContinuesClick != null && leftContinuesClickTimer != null)
			{
				shouldWaitForLeftContinuesClick = true;
				leftContinuesClickTimer.ReStart();
			}
		}
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			shouldWaitForRightLongClick = false;
			shouldWaitForRightContinuesClick = false;
			hasRightClickBeenInvoked = false;
			if (onRightLongClick != null && rightLongClickTimer != null)
			{
				shouldWaitForRightLongClick = true;
				rightLongClickTimer.Reset();
			}
			else
			{
				onRightClick.Invoke();
				hasRightClickBeenInvoked = true;
			}
			if (onRightContinuesClick != null && rightContinuesClickTimer != null)
			{
				shouldWaitForRightContinuesClick = true;
				rightContinuesClickTimer.ReStart();
			}
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (shouldWaitForLeftLongClick && !hasLeftClickBeenInvoked)
			{
				onLeftClick?.Invoke();
				hasLeftClickBeenInvoked = true;
			}
			shouldWaitForLeftLongClick = false;
			shouldWaitForLeftContinuesClick = false;
		}
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (shouldWaitForRightLongClick && !hasRightClickBeenInvoked)
			{
				onRightClick?.Invoke();
				hasRightClickBeenInvoked = true;
			}
			shouldWaitForRightLongClick = false;
			shouldWaitForRightContinuesClick = false;
		}
		onPointerUp.Invoke();
	}

	private void Update()
	{
		if (shouldWaitForLeftLongClick && onLeftLongClick != null && leftLongClickTimer != null && leftLongClickTimer.Tick(Time.deltaTime))
		{
			shouldWaitForLeftLongClick = false;
			onLeftLongClick.Invoke();
			hasLeftClickBeenInvoked = true;
		}
		if (shouldWaitForLeftContinuesClick && onLeftContinuesClick != null && leftContinuesClickTimer != null && leftContinuesClickTimer.Update(Time.deltaTime))
		{
			shouldWaitForLeftContinuesClick = onLeftContinuesClick();
			hasLeftClickBeenInvoked = true;
		}
		if (shouldWaitForRightLongClick && onRightLongClick != null && rightLongClickTimer != null && rightLongClickTimer.Tick(Time.deltaTime))
		{
			shouldWaitForRightLongClick = false;
			onRightLongClick.Invoke();
			hasRightClickBeenInvoked = true;
		}
		if (shouldWaitForRightContinuesClick && onRightContinuesClick != null && rightContinuesClickTimer != null && rightContinuesClickTimer.Update(Time.deltaTime))
		{
			shouldWaitForRightContinuesClick = onRightContinuesClick();
			hasRightClickBeenInvoked = true;
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		shouldWaitForLeftLongClick = false;
		shouldWaitForLeftContinuesClick = false;
		shouldWaitForRightLongClick = false;
		shouldWaitForRightContinuesClick = false;
	}
}
