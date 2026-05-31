using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DolocNavigationButton : DolocIcon
{
	[HideInInspector]
	public int index;

	[HideInInspector]
	public UnityEvent<int> onClick = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onSelect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDeselect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onPointerEnter = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onPointerExit = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int, MoveDirection> onMove = new UnityEvent<int, MoveDirection>();

	private UnityAction<int> onLeftClick;

	private UnityAction<int> onRightClick;

	private UnityAction<int> onAssistLeftClick;

	private UnityAction<int> onAssistRightClick;

	private UnityAction<int> onLeftLongClick;

	private UnityAction<int> onRightLongClick;

	private Func<int, bool> onLeftContinuesClick;

	private Func<int, bool> onRightContinuesClick;

	public UnityAction<int, bool> onHighLighted;

	public UnityAction<int, bool> onGrayed;

	[FormerlySerializedAs("button")]
	public DolocButtonComponent _button;

	protected CanvasGroup buttonCanvasGroup;

	private bool _visible = true;

	private bool _interactable = true;

	private bool _grayed;

	private bool _highLighted;

	private Vector2 backupSize;

	private Vector2 backupPositionLocal;

	private bool alreadyBackup;

	public DolocButtonComponent button
	{
		get
		{
			if (_button == null)
			{
				Init();
			}
			return _button;
		}
	}

	public Selectable.Transition transition
	{
		get
		{
			return button.transition;
		}
		set
		{
			button.transition = value;
		}
	}

	public SpriteState spriteState
	{
		get
		{
			return button.spriteState;
		}
		set
		{
			button.spriteState = value;
		}
	}

	public bool IsSelected => EventSystem.current?.currentSelectedGameObject == button.gameObject;

	public bool visible
	{
		get
		{
			return _visible;
		}
		set
		{
			if (_visible != value)
			{
				buttonCanvasGroup.alpha = (value ? 1 : 0);
				buttonCanvasGroup.interactable = value;
				buttonCanvasGroup.blocksRaycasts = value;
				if (value && grayed)
				{
					OnGrayed(value: true);
				}
				button.interactable = value && _interactable;
				_visible = value;
			}
		}
	}

	public bool interactable
	{
		get
		{
			return button.interactable;
		}
		set
		{
			if (button.interactable != value)
			{
				grayed = !value;
				_interactable = value;
				button.interactable = value;
			}
		}
	}

	public bool blocksRaycasts
	{
		set
		{
			buttonCanvasGroup.blocksRaycasts = value;
		}
	}

	public bool grayed
	{
		get
		{
			return _grayed;
		}
		set
		{
			if (_grayed != value)
			{
				onGrayed?.Invoke(index, value);
				OnGrayed(value);
				_grayed = value;
			}
		}
	}

	public bool highLighted
	{
		get
		{
			return _highLighted;
		}
		set
		{
			if (_highLighted != value)
			{
				onHighLighted?.Invoke(index, value);
				OnHighLighted(value);
				_highLighted = value;
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		buttonCanvasGroup = GetComponent<CanvasGroup>();
		if (_button == null)
		{
			_button = GetComponent<DolocButtonComponent>();
		}
		button.onClick.AddListener(delegate
		{
			onClick?.Invoke(index);
		});
		button.onSelect.AddListener(delegate
		{
			OnSelect();
			onSelect.Invoke(index);
		});
		button.onDeselect.AddListener(delegate
		{
			OnDeselect();
			onDeselect.Invoke(index);
		});
		button.onPointerEnter.AddListener(delegate
		{
			OnPointerEnter();
			onPointerEnter.Invoke(index);
		});
		button.onPointerExit.AddListener(delegate
		{
			OnPointerExit();
			onPointerExit.Invoke(index);
		});
		button.onLeftClick.AddListener(delegate
		{
			onLeftClick?.Invoke(index);
		});
		button.onRightClick.AddListener(delegate
		{
			onRightClick?.Invoke(index);
		});
		button.onAssistLeftClick.AddListener(delegate
		{
			onAssistLeftClick?.Invoke(index);
		});
		button.onAssistRightClick.AddListener(delegate
		{
			onAssistRightClick?.Invoke(index);
		});
		button.onLeftLongClick.AddListener(delegate
		{
			onLeftLongClick?.Invoke(index);
		});
		button.onRightLongClick.AddListener(delegate
		{
			onRightLongClick?.Invoke(index);
		});
		button.onLeftContinuesClick = () => onLeftContinuesClick?.Invoke(index) ?? false;
		button.onRightContinuesClick = () => onRightContinuesClick?.Invoke(index) ?? false;
		button.onMove.AddListener(delegate(MoveDirection dir)
		{
			onMove?.Invoke(index, dir);
		});
	}

	public void FireClick(bool fireSelect = false, bool ignoreActiveState = true)
	{
		if (interactable)
		{
			button.FireClick(fireSelect, ignoreActiveState);
		}
	}

	public void Select()
	{
		if (button != null)
		{
			button.Select();
		}
	}

	protected virtual void OnSelect()
	{
	}

	protected virtual void OnDeselect()
	{
	}

	protected virtual void OnPointerEnter()
	{
	}

	protected virtual void OnPointerExit()
	{
	}

	public void SetClickCallbacks(UnityAction<int> onLeftClick, UnityAction<int> onRightClick = null, UnityAction<int> onAssistLeftClick = null, UnityAction<int> onAssistRighClick = null, UnityAction<int> onLeftLongClick = null, UnityAction<int> onRightLongClick = null, Func<int, bool> onLeftContinuesClick = null, Func<int, bool> onRightContinuesClick = null)
	{
		ClearAllClickCallbacks();
		this.onLeftClick = onLeftClick;
		this.onRightClick = onRightClick;
		this.onAssistLeftClick = onAssistLeftClick;
		onAssistRightClick = onAssistRighClick;
		this.onLeftLongClick = onLeftLongClick;
		this.onRightLongClick = onRightLongClick;
		this.onLeftContinuesClick = onLeftContinuesClick;
		this.onRightContinuesClick = onRightContinuesClick;
	}

	public void ClearAllClickCallbacks()
	{
		onLeftClick = null;
		onRightClick = null;
		onAssistLeftClick = null;
		onAssistRightClick = null;
		onLeftLongClick = null;
		onRightLongClick = null;
		onLeftContinuesClick = null;
		onRightContinuesClick = null;
	}

	protected virtual void OnGrayed(bool value)
	{
		buttonCanvasGroup.DOFade(value ? 0.3f : 1f, 0.1f);
	}

	protected virtual void OnHighLighted(bool value)
	{
	}

	public void BackUpAndNormalizeToCenter()
	{
		if (!alreadyBackup)
		{
			alreadyBackup = true;
			backupSize = base.size;
			backupPositionLocal = base.positionLocal;
			base.positionLocal += base.size / 2f;
			base.size = Vector2.one;
		}
	}

	public void Revert()
	{
		if (alreadyBackup)
		{
			alreadyBackup = false;
			base.size = backupSize;
			base.positionLocal = backupPositionLocal;
		}
	}
}
