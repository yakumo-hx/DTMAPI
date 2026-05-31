using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SettingPanel : DolocUIPanel, INavPanel, IScrollContentRect
{
	[SerializeField]
	private ScrollRect _scrollRect;

	[SerializeField]
	public TitleMenu titleMenu;

	[SerializeField]
	public DolocButtonComponent saveBtn;

	[SerializeField]
	public DolocButtonComponent revertBtn;

	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private RectTransform content;

	[SerializeField]
	private RectTransform cursorLine;

	[SerializeField]
	private float moveDuration = 0.15f;

	[SerializeField]
	private float scrollOffset = 12f;

	[SerializeField]
	public RebindActionMask listeningKeyMask;

	[SerializeField]
	private GameObject raycastMask;

	[SerializeField]
	public ItemBorder arrow;

	private Dictionary<string, List<RebindActionUI>> rebindActionUIByGroup = new Dictionary<string, List<RebindActionUI>>();

	private List<RebindActionUI> allRebindActionUIs = new List<RebindActionUI>();

	private SettingUIItemManager uiItemMgr;

	private Selectable[] contentSelectables;

	private Selectable[] barSelectables;

	private Selectable latestContentSelectable;

	private bool isFirstRender = true;

	private UserSettingGroupInfo currentGroupInfo;

	private TbUserSettingGroup groupTable => DolocConfig.Tables.TbUserSettingGroup;

	private UserSettingGroupInfo firstGroupInfo => groupTable.DataList.FirstOrDefault();

	public bool conflictNow { get; private set; }

	public bool isRebindPending => allRebindActionUIs.Any((RebindActionUI x) => x.rebindSlots.Any((RebindActionSlot y) => y.isRebindPending));

	public bool isRebindWaitingConfirm => listeningKeyMask.gameObject.activeSelf;

	public bool useRaycastMask
	{
		set
		{
			if (raycastMask != null)
			{
				raycastMask.SetActive(value);
			}
		}
	}

	public Selectable[] allSelectablesArray { get; private set; }

	public int allSelectableCount => allSelectablesArray.Length;

	public bool isContentSelect { get; private set; }

	public bool isSaveButtonSelect { get; private set; }

	public string currentGroupId => currentGroupInfo?.Id ?? string.Empty;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		listeningKeyMask.Init();
		listeningKeyMask.Hide();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
		uiItemMgr = new SettingUIItemManager(content);
		operationTip.SetVisible(value: false);
		barSelectables = new Selectable[2] { saveBtn, revertBtn };
		Selectable[] array = barSelectables;
		foreach (Selectable selectable in array)
		{
			ISelectable btn = selectable as ISelectable;
			btn?.onSelect.AddListener(delegate
			{
				isSaveButtonSelect = selectable == saveBtn;
				isContentSelect = false;
				SetCursorLineActive(value: false);
				btn.rectTransform.GetItemBorder(BorderType.Arrow);
			});
			btn?.onDeselect.AddListener(delegate
			{
				isSaveButtonSelect = false;
				DolocAPI.HideItemBorder();
			});
			btn?.onMove.AddListener(delegate(MoveDirection dir)
			{
				if (dir == MoveDirection.Up || dir == MoveDirection.Down)
				{
					latestContentSelectable.Select();
				}
			});
		}
	}

	public void Render(UserSettings userSettings, bool force)
	{
		uiItemMgr.Render(userSettings);
		if (isFirstRender)
		{
			InitSettingItems();
		}
		isFirstRender = false;
		titleMenu.Render(groupTable.DataList.Select((UserSettingGroupInfo x) => x.Title).ToArray());
		SetSettingGroup(currentGroupInfo, force);
	}

	private void InitSettingItems()
	{
		RefreshSelectables();
		foreach (ISettingUiItem uiItem in uiItemMgr.selectableCmps)
		{
			Selectable selectable = uiItem.selectable;
			ISelectable obj = selectable as ISelectable;
			obj?.onSelect.AddListener(delegate
			{
				isContentSelect = true;
				latestContentSelectable = selectable;
				SetRect(selectable);
				DolocAPI.UIRaiseRoll();
				HoverCursorLine(uiItem);
				cursorLine.transform.DOLocalMoveY(((DolocUiObject)uiItem).transform.localPosition.y, 0f);
			});
			obj?.onDeselect.AddListener(DolocAPI.HideItemBorder);
			if (!(uiItem is RebindActionUI rebindActionUI))
			{
				continue;
			}
			rebindActionUI.mask = listeningKeyMask;
			rebindActionUI.arrow = arrow;
			rebindActionUI.operationTip = operationTip;
			string[] labels = rebindActionUI.rebindActionData.InputAction_Ref.Labels;
			foreach (string key in labels)
			{
				rebindActionUIByGroup.TryAdd(key, new List<RebindActionUI>());
				rebindActionUIByGroup[key].Add(rebindActionUI);
			}
			RebindActionSlot[] rebindSlots = rebindActionUI.rebindSlots;
			for (int i = 0; i < rebindSlots.Length; i++)
			{
				rebindSlots[i].updateBindingUIEvent.AddListener(delegate
				{
					CheckBindKeyNotRepeat();
					foreach (ISettingUiItem allCmp in uiItemMgr.allCmps)
					{
						if (allCmp is ToggleItemUI { isVisible: not false } toggleItemUI)
						{
							toggleItemUI.RefreshText();
						}
					}
				});
			}
		}
		CheckBindKeyNotRepeat();
	}

	private void RefreshSelectables()
	{
		contentSelectables = (from uiItem in uiItemMgr.selectableCmps
			where uiItem.activeSelf
			select uiItem.selectable).ToArray();
		allSelectablesArray = contentSelectables.Concat(barSelectables).ToArray();
	}

	private void SetRect(Selectable selectable)
	{
		if (selectable == contentSelectables.First())
		{
			content.DOAnchorPosY(0f, moveDuration);
			return;
		}
		float num = viewport.rect.height;
		Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selectable.transform);
		float num2 = 0f;
		int num3 = 12;
		if (bounds.max.y + (float)num3 + num > num)
		{
			num2 = -1f * (bounds.max.y + (float)num3 + scrollOffset);
		}
		if (bounds.min.y - (float)num3 + num < 0f)
		{
			num2 = -1f * (bounds.min.y - (float)num3 + num - scrollOffset);
		}
		float endValue = content.anchoredPosition.y + num2;
		content.DOAnchorPosY(endValue, moveDuration);
	}

	public bool GetItemCmp<T>(UserSettingType id, out T item) where T : class, ISettingUiItem
	{
		return uiItemMgr.GetItemCmp<T>(id, out item);
	}

	private void SetCursorLineActive(bool value)
	{
		cursorLine.gameObject.SetActive(value);
		if (!value)
		{
			arrow.Hide();
		}
	}

	private void HoverCursorLine(ISettingUiItem uiItem)
	{
		SetCursorLineActive(value: true);
		Vector2 sizeDelta = cursorLine.sizeDelta;
		cursorLine.sizeDelta = new Vector2(sizeDelta.x, uiItem.height);
		if (uiItem.shouldShowArrow)
		{
			arrow.HoverTo(cursorLine, useAnimation: false, BorderType.Arrow);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetCursorLineActive(value: false);
		titleMenu.Render(groupTable.DataList.Select((UserSettingGroupInfo x) => x.Title).ToArray());
		titleMenu.FireClick(0, fireSelect: true);
		DolocAPI.DelayFrame(delegate
		{
			titleMenu.Select(0);
		});
		SetSettingGroup(0, force: true);
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildNavigation();
		contentSelectables.First().Select();
	}

	public void RebuildNavigation()
	{
		RefreshSelectables();
		contentSelectables.RebuildNavigationVerticalByOrder(wrapAround: false);
		barSelectables.RebuildNavigationHorizontal(barSelectables);
		contentSelectables.First().SetNavigationOnUp(saveBtn);
		contentSelectables.Last().SetNavigationOnDown(saveBtn);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		currentGroupInfo = null;
	}

	protected override void OnFinishHide()
	{
		content.DOAnchorPosY(0f, 0f);
		useRaycastMask = false;
		base.OnFinishHide();
	}

	public void SetSettingGroup(int index, bool force)
	{
		List<UserSettingGroupInfo> dataList = groupTable.DataList;
		if (index >= 0 && index < dataList.Count)
		{
			UserSettingGroupInfo groupInfo = dataList[index];
			SetSettingGroup(groupInfo, force);
		}
	}

	private void SetSettingGroup(UserSettingGroupInfo groupInfo, bool force)
	{
		InputSchemaType inputSchemaType = DolocAPI.UserInput.InputSchemaType;
		DolocInputDeviceType deviceType = DolocAPI.UserInput.DeviceType;
		if (groupInfo == null)
		{
			groupInfo = firstGroupInfo;
		}
		if (!uiItemMgr.uiCmpsByGroup.TryGetValue(groupInfo.Id, out var value))
		{
			return;
		}
		if (!force && currentGroupInfo?.Id == groupInfo.Id)
		{
			DolocAPI.DelayFrame(delegate
			{
				latestContentSelectable?.Select();
			});
			return;
		}
		currentGroupInfo = groupInfo;
		foreach (ISettingUiItem allCmp in uiItemMgr.allCmps)
		{
			bool flag = value.Contains(allCmp);
			((DolocUiObject)allCmp).SetVisible(flag);
			if (flag)
			{
				if (allCmp is RebindActionUI rebindActionUI)
				{
					rebindActionUI.RefreshKeyInfo(inputSchemaType, deviceType);
				}
				if (allCmp is ToggleItemUI toggleItemUI)
				{
					toggleItemUI.RefreshText();
				}
			}
		}
		scrollRect.verticalScrollbar.value = 1f;
		RebuildNavigation();
		DolocAPI.DelayFrame(delegate
		{
			contentSelectables.First()?.Select();
		});
	}

	private bool CheckBindKeyNotRepeat()
	{
		bool flag = true;
		HashSet<RebindActionSlot> hashSet = new HashSet<RebindActionSlot>();
		List<RebindActionSlot> list = new List<RebindActionSlot>();
		foreach (List<RebindActionUI> value2 in rebindActionUIByGroup.Values)
		{
			Dictionary<string, List<RebindActionSlot>> dictionary = new Dictionary<string, List<RebindActionSlot>>();
			foreach (RebindActionUI item in value2)
			{
				RebindActionSlot[] rebindSlots = item.rebindSlots;
				foreach (RebindActionSlot rebindActionSlot in rebindSlots)
				{
					dictionary.TryAdd(rebindActionSlot.EffectivePath, new List<RebindActionSlot>());
					dictionary[rebindActionSlot.EffectivePath].Add(rebindActionSlot);
					list.Add(rebindActionSlot);
				}
			}
			foreach (KeyValuePair<string, List<RebindActionSlot>> item2 in dictionary)
			{
				item2.Deconstruct(out var key, out var value);
				string str = key;
				List<RebindActionSlot> list2 = value;
				bool flag2 = !str.IsNullOrEmpty() && list2.Count > 1;
				for (int j = 0; j < list2.Count; j++)
				{
					RebindActionSlot rebindActionSlot2 = list2[j];
					if (flag2)
					{
						hashSet.Add(rebindActionSlot2);
					}
					flag = flag && !flag2;
					if (rebindActionSlot2.isRebindPending)
					{
						listeningKeyMask.keyIcon.IsConflict = flag2;
						if (flag2)
						{
							RebindActionSlot rebindActionSlot3 = ((j == 0) ? list2[1] : list2[0]);
							listeningKeyMask.SetConflictHint(DolocUtils.Format(base.staticTexts.SettingPanelConflictHint, rebindActionSlot3.boxTitle));
						}
					}
				}
			}
		}
		foreach (RebindActionSlot item3 in list)
		{
			item3.IsConflict = hashSet.Contains(item3);
		}
		conflictNow = flag;
		return flag;
	}

	public void OnMove()
	{
	}
}
