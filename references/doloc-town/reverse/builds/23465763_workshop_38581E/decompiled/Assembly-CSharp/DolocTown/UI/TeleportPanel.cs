using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TeleportPanel : DolocUIPanel, INavPanel
{
	[SerializeField]
	private Transform container;

	[SerializeField]
	private Transform lockedContainer;

	[SerializeField]
	private Image mapRender;

	[SerializeField]
	private Vector2 offset;

	[SerializeField]
	private float distanceWeight = 10f;

	public TicketTip ticketTip;

	public ObjectPool<TeleportPoint> slots;

	public ObjectPool<TeleportPoint> lockedSlots;

	private Dictionary<string, int> indexDict = new Dictionary<string, int>();

	public Selectable[] allSelectablesArray => ((IEnumerable<TeleportPoint>)slots).Select((Func<TeleportPoint, Selectable>)((TeleportPoint x) => x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TELEPORT_POINT);
		slots = new ObjectPool<TeleportPoint>(asset, container);
		lockedSlots = new ObjectPool<TeleportPoint>(asset, lockedContainer);
		base.displayAnimType = UiPanelDisplayAnimType.FromTop;
		ticketTip = GetComponentInChildren<TicketTip>(includeInactive: true);
		ticketTip.Init();
	}

	public void InitTeleportPoints(TeleportPointGroupData data)
	{
		indexDict.Clear();
		if (!data.notEmpty)
		{
			slots.CheckCount(0);
			lockedSlots.CheckCount(0);
			return;
		}
		Sprite mapSprite = mapRender.sprite;
		int count = data.unlockedPoints.Count;
		slots.CheckCount(count);
		for (int i = 0; i < count; i++)
		{
			TeleportPointData info2 = data.unlockedPoints[i];
			indexDict.Add(info2.id, i);
			TeleportPoint teleportPoint = slots[i];
			teleportPoint.index = i;
			SetPointPos(teleportPoint, info2);
			teleportPoint.onPointerEnter.RemoveListener(Select);
			teleportPoint.onPointerEnter.AddListener(Select);
		}
		RebuildNavigation();
		count = data.lockedPoints.Count;
		lockedSlots.CheckCount(count);
		for (int j = 0; j < count; j++)
		{
			TeleportPointData info3 = data.lockedPoints[j];
			TeleportPoint teleportPoint2 = lockedSlots[j];
			SetPointPos(teleportPoint2, info3);
			teleportPoint2.interactable = false;
		}
		void SetPointPos(TeleportPoint slot, TeleportPointData info)
		{
			Vector2Int mapPosition = info.mapPosition;
			Rect rect = mapRender.rectTransform.rect;
			float x = ((float)mapPosition.x / mapSprite.rect.size.x - 0.5f) * rect.width;
			float y = ((float)mapPosition.y / mapSprite.rect.size.y - 0.5f) * rect.height;
			slot.positionLocal = new Vector2(x, y);
		}
	}

	public void RebuildNavigation()
	{
		this.RebuildNavigation(allSelectablesArray, distanceWeight);
	}

	public void SetTicketCount(int count)
	{
		ticketTip.SetTicketCount(count);
	}

	public void Select(int index)
	{
		slots[Mathf.Clamp(index, 0, slots.ActiveCount - 1)].Select();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DolocAPI.HideHoverBox();
	}

	public TeleportPoint GetSlot(int index)
	{
		return slots[Mathf.Clamp(index, 0, slots.ActiveCount - 1)];
	}
}
