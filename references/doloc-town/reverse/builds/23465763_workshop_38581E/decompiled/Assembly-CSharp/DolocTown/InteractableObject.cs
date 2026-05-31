using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class InteractableObject : DolocObject, ILockable, IInteractable, IFellable, IAttackable
{
	public enum ToggleType
	{
		None,
		Single,
		Switch
	}

	[SerializeField]
	protected TextConfig textTitle;

	[SerializeField]
	private bool useCustomEvent;

	[SerializeField]
	private SendMessageTiming sendMessageTiming;

	[SerializeField]
	private string _customEventId;

	[SerializeField]
	private bool _useGuid;

	[SerializeField]
	private string _guid;

	[SerializeField]
	private string _roomId;

	[SerializeField]
	private bool hasLockState;

	[SerializeField]
	private string _lockObjectId;

	[SerializeField]
	private bool updateTipWhenLock;

	[SerializeField]
	private TextConfig tipWhenLock;

	[SerializeField]
	private TextTipConfig tipOnInteractWhenLock;

	[SerializeField]
	private bool toggleVisibleOnLock;

	[SerializeField]
	protected Transform[] objectsShowOnLock;

	[SerializeField]
	protected Transform[] objectsHideOnLock;

	[SerializeField]
	private bool useFixedTip;

	[SerializeField]
	protected TextConfig textTip;

	[SerializeField]
	protected TipPositionType tipPositionType;

	[SerializeField]
	protected Vector2 tipOffset;

	[SerializeField]
	private bool useOutlineOnTouch;

	[SerializeField]
	private ObjectOutlineType outlineType;

	[SerializeField]
	protected SpriteRenderer[] renderersWithOutline;

	[SerializeField]
	private bool toggleVisibleOnTouch;

	[SerializeField]
	protected Transform[] objectsToggleVisibleOnTouch;

	[SerializeField]
	private ToggleType triggerType;

	[SerializeField]
	private bool toggleVisibleOnInteract;

	[SerializeField]
	protected Transform[] objectsToggleVisibleOnInteract;

	[SerializeField]
	private bool updateTipWhenToggleOn;

	[SerializeField]
	private TextConfig tipWhenToggleOn;

	[SerializeField]
	private bool useConditionChecker;

	[SerializeField]
	protected ConditionGroupChecker conditionChecker;

	[SerializeField]
	private bool useInteractableLayer;

	[SerializeField]
	public int Priority;

	private Dictionary<SpriteRenderer, EntityOutlineRenderer> outlineRenderers = new Dictionary<SpriteRenderer, EntityOutlineRenderer>();

	protected Collider2D _collider;

	protected Renderer _renderer;

	protected virtual bool notSupportCustomEvent => true;

	public string CustomEventId => _customEventId;

	public string guid => _guid;

	public bool useGuid => _useGuid;

	public string roomId => _roomId;

	protected bool isRender { get; private set; }

	protected RoomInteractableObjectManager archiveData { get; private set; }

	public virtual AttackableType attackableType => AttackableType.None;

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public string lockObjectId => _lockObjectId;

	private bool showTipTextConfig => useFixedTip;

	private bool hasToggleState => triggerType != ToggleType.None;

	public int InteractableLayer
	{
		get
		{
			if (!useInteractableLayer)
			{
				return 0;
			}
			return Priority;
		}
	}

	public virtual bool OnlyTouch => false;

	public virtual bool CanInteractContinues => false;

	public virtual string InteractableTitle => textTitle.Text;

	public bool isTouched { get; protected set; }

	private bool shouldSetOutline
	{
		get
		{
			if (useOutlineOnTouch && renderersWithOutline != null)
			{
				return renderersWithOutline.Length != 0;
			}
			return false;
		}
	}

	private bool shouldSetTip
	{
		get
		{
			if (useFixedTip && !textTip.isEmpty)
			{
				if (DolocAPI.IsAgentRiding)
				{
					return shouldShowTipInRiding;
				}
				return true;
			}
			return false;
		}
	}

	protected virtual bool shouldShowTipInRiding => false;

	protected virtual bool showTip => true;

	protected virtual object tipCaller => this;

	protected virtual string InteractKeyName => DolocAPI.UserInput.GlobalInteractActionName;

	protected override void __Init()
	{
		base.__Init();
		if (!renderersWithOutline.IsNullOrEmpty())
		{
			renderersWithOutline = renderersWithOutline.Where((SpriteRenderer x) => x != null).ToArray();
		}
	}

	protected void TrySendGameEvent(SendMessageTiming timing = SendMessageTiming.Custom)
	{
		if (useCustomEvent && (timing == SendMessageTiming.Custom || timing == sendMessageTiming))
		{
			if (_customEventId.IsNullOrEmpty() || DolocConfig.Tables.TbCustomEvent.GetOrDefault(_customEventId) == null)
			{
				Debug.LogError("交互物<" + base.gameObject.name + ">自定义事件id不合法：<" + _customEventId + ">");
			}
			else
			{
				DolocAPI.BroadcastString(GameEventType.CUSTOM, _customEventId);
			}
		}
	}

	public void Render(Room room)
	{
		isRender = true;
		if (archiveData == null)
		{
			RoomInteractableObjectManager roomInteractableObjectManager = (archiveData = room.DM_interactableObject);
		}
		if (archiveData == null)
		{
			OnRender(room);
			return;
		}
		LoadData(room);
		OnRender(room);
	}

	public void UnRender()
	{
		isRender = false;
		OnUnRender();
		DisableOutline();
	}

	protected virtual void OnRender(Room room)
	{
	}

	protected virtual void OnUnRender()
	{
	}

	private void LoadData(Room room)
	{
		if (archiveData != null)
		{
			LoadLockState();
			if (useGuid)
			{
				LoadObjectVisibleState();
				LoadToggleState();
				OnLoadData(room);
			}
		}
	}

	private void LoadObjectVisibleState()
	{
		if (archiveData != null)
		{
			archiveData.LoadObjectVisibleState(guid, out var visible);
			SetVisible(visible);
		}
	}

	protected virtual void OnLoadData(Room room)
	{
	}

	public virtual bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		return false;
	}

	public bool OnSwordAttack(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		return OnAttacked(attack, criticalRate, pos, out isDead);
	}

	public virtual bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return false;
	}

	public void SetLockState(bool value)
	{
		if (!hasLockState)
		{
			return;
		}
		if (toggleVisibleOnLock)
		{
			Transform[] array = objectsShowOnLock;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value);
			}
			array = objectsHideOnLock;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(!value);
			}
		}
		OnLockStateChange(value);
	}

	private void LoadLockState()
	{
		if (hasLockState)
		{
			SetLockState(archiveData?.LoadLockState(lockObjectId) ?? false);
		}
	}

	protected virtual void OnLockStateChange(bool value)
	{
	}

	void IInteractable.OnTouch()
	{
		if (!isTouched)
		{
			ReverseVisibleOnTouch();
		}
		isTouched = true;
		DolocAPI.CurrentInteractableObject = this;
		if (shouldSetOutline)
		{
			EnableOutline();
		}
		if (shouldSetTip && showTip)
		{
			ShowDefaultTip();
		}
		TrySendGameEvent(SendMessageTiming.Touch);
		OnTouch();
	}

	void IInteractable.OnDisTouch()
	{
		if (isTouched)
		{
			ReverseVisibleOnTouch();
		}
		isTouched = false;
		if (DolocAPI.CurrentInteractableObject == this)
		{
			DolocAPI.CurrentInteractableObject = null;
		}
		if (shouldSetOutline)
		{
			DisableOutline();
		}
		if (shouldSetTip)
		{
			HideTip();
		}
		TrySendGameEvent(SendMessageTiming.DisTouch);
		OnDisTouch();
	}

	private void ReverseVisibleOnTouch()
	{
		if (toggleVisibleOnTouch)
		{
			Transform[] array = objectsToggleVisibleOnTouch;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.ToggleActive();
			}
		}
	}

	void IInteractable.OnInteract()
	{
		if (hasLockState && !DolocAPI.gameManager.gameInitConfig.skipInteractionConditionCheck)
		{
			RoomInteractableObjectManager roomInteractableObjectManager = archiveData;
			if (roomInteractableObjectManager != null && roomInteractableObjectManager.LoadLockState(lockObjectId))
			{
				PushTip();
				tipOnInteractWhenLock.ShowText(delegate(string text)
				{
					DolocAPI.ShowMessageBoxSmall(text);
				}, () => GetTipPosition());
				return;
			}
		}
		if (useConditionChecker && !conditionChecker.IsConditionMet(out var conditionFailedText))
		{
			PushTip();
			conditionFailedText.ShowText(delegate(string text)
			{
				DolocAPI.ShowMessageBoxSmall(text);
			}, () => GetTipPosition());
			return;
		}
		if (useGuid)
		{
			bool flag = archiveData?.LoadToggleState(guid) ?? false;
			switch (triggerType)
			{
			case ToggleType.Single:
				if (!flag)
				{
					Toggle(value: true, visible: true);
				}
				break;
			case ToggleType.Switch:
				Toggle(!flag, visible: true);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case ToggleType.None:
				break;
			}
		}
		TrySendGameEvent(SendMessageTiming.Interact);
		if (shouldSetOutline)
		{
			EnableOutline();
		}
		OnInteract();
	}

	private void Toggle(bool value, bool visible)
	{
		archiveData?.SaveToggleState(guid, value);
		if (toggleVisibleOnInteract)
		{
			Transform[] array = objectsToggleVisibleOnInteract;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.ToggleActive();
			}
		}
		if (isTouched && updateTipWhenToggleOn && !tipWhenToggleOn.isEmpty)
		{
			PushTipToHide();
			ShowDefaultTip();
		}
		OnToggle(value, visible);
	}

	private void LoadToggleState()
	{
		if (hasToggleState)
		{
			RoomInteractableObjectManager roomInteractableObjectManager = archiveData;
			if (roomInteractableObjectManager != null && roomInteractableObjectManager.LoadToggleState(guid))
			{
				Toggle(value: true, visible: false);
			}
		}
	}

	protected Vector2 GetTipPosition(float rate = 1.1f)
	{
		Vector2 zero;
		switch (tipPositionType)
		{
		case TipPositionType.FromTop:
			zero = DolocAPI.CalcPopPosition(base.transform, rate) + tipOffset;
			break;
		case TipPositionType.FromBottom:
			zero = (Vector2)base.transform.position + new Vector2(0f, 5.5f) + tipOffset;
			break;
		default:
			zero = Vector2.zero;
			throw new ArgumentOutOfRangeException();
		}
		if (DolocAPI.IsAgentRiding && zero.y - position.y < 6f)
		{
			zero.y += 3f;
		}
		return zero;
	}

	protected void ShowTip(string prompt, float rate = 1.1f)
	{
		if (!prompt.IsNullOrEmpty())
		{
			Vector2 tipPosition = GetTipPosition(rate);
			tipCaller.ShowSceneOperationTip(tipPosition, prompt, InteractKeyName);
		}
	}

	protected void ShowTip(Vector2 pos, string prompt)
	{
		if (!prompt.IsNullOrEmpty())
		{
			tipCaller.ShowSceneOperationTip(pos, prompt, InteractKeyName);
		}
	}

	protected void DisableOutline()
	{
		foreach (EntityOutlineRenderer value in outlineRenderers.Values)
		{
			DolocAPI.EntitySystem.Recycle(value);
		}
		outlineRenderers.Clear();
	}

	private void EnableOutline()
	{
		DisableOutline();
		if (renderersWithOutline == null)
		{
			return;
		}
		SpriteRenderer[] array = renderersWithOutline;
		foreach (SpriteRenderer spriteRenderer in array)
		{
			if (!(spriteRenderer == null) && spriteRenderer.gameObject.activeSelf && spriteRenderer.enabled)
			{
				outlineRenderers.TryAdd(spriteRenderer, DolocAPI.EntitySystem.Next<EntityOutlineRenderer>());
				outlineRenderers[spriteRenderer].ShowOutline(spriteRenderer, outlineType);
			}
		}
	}

	protected void ShowDefaultTip()
	{
		ShowTip(GetDefaultTipText());
	}

	protected void HideTip()
	{
		tipCaller.HideSceneOperationTip();
	}

	protected void PushTip()
	{
		tipCaller.PushSceneOperationTip();
	}

	protected void PushTipToHide()
	{
		tipCaller.PushSceneOperationTipToHide();
	}

	public void HideTouchEffect()
	{
		HideTip();
		DisableOutline();
	}

	protected bool CheckTipShouldOverwrite()
	{
		if (hasLockState && updateTipWhenLock)
		{
			RoomInteractableObjectManager roomInteractableObjectManager = archiveData;
			if (roomInteractableObjectManager != null && roomInteractableObjectManager.LoadLockState(lockObjectId))
			{
				return true;
			}
		}
		if (updateTipWhenToggleOn && archiveData != null)
		{
			RoomInteractableObjectManager roomInteractableObjectManager2 = archiveData;
			if (roomInteractableObjectManager2 != null && roomInteractableObjectManager2.LoadToggleState(guid))
			{
				return !tipWhenToggleOn.isEmpty;
			}
		}
		return false;
	}

	protected virtual string GetDefaultTipText()
	{
		string text = textTip.Text;
		if (hasLockState && updateTipWhenLock)
		{
			RoomInteractableObjectManager roomInteractableObjectManager = archiveData;
			if (roomInteractableObjectManager != null && roomInteractableObjectManager.LoadLockState(lockObjectId))
			{
				text = tipWhenLock.Text;
				goto IL_0087;
			}
		}
		if (updateTipWhenToggleOn && archiveData != null)
		{
			RoomInteractableObjectManager roomInteractableObjectManager2 = archiveData;
			if (roomInteractableObjectManager2 != null && roomInteractableObjectManager2.LoadToggleState(guid) && !tipWhenToggleOn.isEmpty)
			{
				text = tipWhenToggleOn.Text;
			}
		}
		goto IL_0087;
		IL_0087:
		return text;
	}

	protected virtual void OnTouch()
	{
	}

	protected virtual void OnToggle(bool value, bool visible)
	{
	}

	protected virtual void OnInteract()
	{
		PushTip();
	}

	protected virtual void OnDisTouch()
	{
	}

	private void Start()
	{
		if (!isInitialized)
		{
			_renderer = GetComponent<Renderer>();
			_collider = GetComponent<Collider2D>();
			Init();
		}
	}

	protected virtual void OnDestroy()
	{
		DisableOutline();
	}

	protected void CreateDropItem(string itemName)
	{
		DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, itemName, position2d);
	}

	protected void CreateDropItems(CountItem countItem)
	{
		DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, countItem.itemName, position2d, countItem.itemCount);
	}

	public void PostSoundEvent(string eventName)
	{
		DolocAPI.Sound.PostSoundEvent(eventName);
	}
}
