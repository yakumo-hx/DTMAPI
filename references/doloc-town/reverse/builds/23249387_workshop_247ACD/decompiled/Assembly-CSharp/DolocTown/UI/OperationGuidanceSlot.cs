using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DolocTown.UI;

public class OperationGuidanceSlot : DolocUiRecyclableObject, IInputDeviceDetect, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private UILocalization l10nText;

	[SerializeField]
	private float showTime = 0.3f;

	[SerializeField]
	private DolocButtonComponent closeBtn;

	private Dictionary<int, OperationGuidanceIconGroup> iconGroups = new Dictionary<int, OperationGuidanceIconGroup>();

	private DolocTweenLocalMove localMove;

	private string actionName;

	private OperationEventType eventType;

	public Vector2 SizeDelta => GetComponent<RectTransform>().sizeDelta;

	protected override void __Init()
	{
		base.__Init();
		localMove = new DolocTweenLocalMove(base.transform, Ease.OutExpo, showTime);
		closeBtn.onClick.AddListener(delegate
		{
			DolocAPI.Broadcast(eventType);
		});
		OperationGuidanceIconGroup[] componentsInChildren = GetComponentsInChildren<OperationGuidanceIconGroup>(includeInactive: true);
		foreach (OperationGuidanceIconGroup operationGuidanceIconGroup in componentsInChildren)
		{
			operationGuidanceIconGroup.Init();
			iconGroups.TryAdd(operationGuidanceIconGroup.IconCount, operationGuidanceIconGroup);
		}
		HideAllIcons();
	}

	public void Render(OperationEventType eventType, string l10nKey, string actionName)
	{
		l10nText.SetL10nKey(l10nKey);
		this.eventType = eventType;
		this.actionName = actionName;
		OnRefresh(DolocAPI.UserInput.DeviceType);
		closeBtn.gameObject.SetActive(value: false);
	}

	private void HideAllIcons()
	{
		foreach (OperationGuidanceIconGroup value in iconGroups.Values)
		{
			value.SetVisible(value: false);
		}
	}

	public void OnRefresh(DolocInputDeviceType deviceType)
	{
		HideAllIcons();
		if (!DolocAPI.GetAllActionKeyIconGroup(deviceType, actionName, out var list))
		{
			return;
		}
		string[] urls = (from x in list
			select x.largeIconUrl into x
			where !x.IsNullOrEmpty()
			select x).ToArray();
		if (DolocConfig.Tables.TbGameCombinedKeyIcon.TryMatch(urls, out var asset) && iconGroups.ContainsKey(1))
		{
			iconGroups[1].SetIcons(new Sprite[1] { asset.Asset });
			iconGroups[1].SetVisible(value: true);
			return;
		}
		if (!iconGroups.TryGetValue(list.Count, out var value))
		{
			value = iconGroups.First().Value;
		}
		value.SetIcons(list.Select((ActionIconGroup x) => x.largeIcon).ToArray());
		value.SetVisible(value: true);
	}

	public void ShowAtCurrent()
	{
		Vector2 vector = base.positionLocal;
		base.positionLocal = new Vector2(vector.x - SizeDelta.x, vector.y);
		localMove.forcePlay(vector);
	}

	public void Hide(Action callback)
	{
		Vector2 vector = new Vector2(base.positionLocal.x - SizeDelta.x, base.positionLocal.y);
		localMove.forcePlay(vector);
		DOVirtual.DelayedCall(showTime, delegate
		{
			SetVisible(value: false);
			callback?.Invoke();
		});
	}

	public void MoveToTargetPos(Vector2 target)
	{
		localMove.forcePlay(target);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		closeBtn.gameObject.SetActive(value: true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		closeBtn.gameObject.SetActive(value: false);
	}
}
