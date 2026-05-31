using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MapPanel : DolocUIPanel
{
	private class MapCache
	{
		public int index;

		public CityMapPanel panel;

		public bool isFullMap;

		public string mapId => panel.MapId;

		public string title => DolocConfig.Tables.TbMapType.GetOrDefault(mapId)?.Title ?? mapId;

		public MapCache(int index, CityMapPanel panel)
		{
			isFullMap = true;
			this.index = index;
			this.panel = panel;
		}
	}

	[SerializeField]
	private TitleMenu titleMenu;

	[SerializeField]
	public CityMapPanel cityMap;

	private List<MapCache> mapCaches = new List<MapCache>();

	private MapCache[] activeMaps;

	private CityMapPanel currentMap;

	private int selectedIndex;

	private string firstSelectedMapId;

	private string focusedMissionId;

	private bool detectCursor;

	private Vector2 currentCursorPosition;

	private bool IsFullMap => activeMaps[selectedIndex].isFullMap;

	protected override void __Init()
	{
		base.__Init();
		CityMapPanel[] componentsInChildren = GetComponentsInChildren<CityMapPanel>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			mapCaches.Add(new MapCache(i, componentsInChildren[i]));
		}
		titleMenu.Init();
		titleMenu.onAnyClicked.AddListener(GetMapFocused);
		Hide();
	}

	private void RefreshActiveMaps()
	{
		MapManager mapMgr = DolocAPI.archiveHandle.farmData.mapManager;
		activeMaps = mapCaches.Where((MapCache x) => mapMgr.IsMapActive(x.mapId)).ToArray();
		for (int i = 0; i < activeMaps.Length; i++)
		{
			activeMaps[i].index = i;
		}
	}

	public void Register()
	{
		titleMenu.SetSelectCallbacks(OnMapSelect);
		RefreshActiveMaps();
		titleMenu.Render(activeMaps.Select((MapCache x) => x.title).ToArray());
		int num = 0;
		MapCache[] array = activeMaps;
		foreach (MapCache mapCache in array)
		{
			if (!firstSelectedMapId.IsNullOrEmpty())
			{
				if (mapCache.mapId == firstSelectedMapId)
				{
					num = mapCache.index;
				}
			}
			else if (!focusedMissionId.IsNullOrEmpty() && mapCache.panel.ContainsMission(focusedMissionId))
			{
				mapCache.panel.SetFocusedMission(focusedMissionId);
				num = mapCache.index;
			}
		}
		selectedIndex = num;
	}

	public void Unregister()
	{
		titleMenu.RemoveCallbacks();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		foreach (MapCache mapCache in mapCaches)
		{
			mapCache.panel.SetCloseButtonVisible(value: false);
			mapCache.panel.Hide();
			mapCache.panel.SetOffset(DolocAPI.archiveHandle.farmData.mapManager.GetMapOffset(mapCache.mapId));
		}
		titleMenu.FireClick(selectedIndex, fireSelect: true);
		FollowPointerPosition().Forget();
	}

	private async UniTaskVoid FollowPointerPosition()
	{
		while (base.inAnimation)
		{
			if (currentMap != null && currentMap.GetInitCursorPosition(out var pos) && DolocAPI.UserInput.DeviceType != 0)
			{
				currentCursorPosition = pos;
			}
			await UniTask.DelayFrame(1);
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.rectTransform.parent.transform as RectTransform);
		}
	}

	protected override void OnFinishHide()
	{
		base.OnFinishHide();
		foreach (MapCache mapCache in mapCaches)
		{
			mapCache.panel.SetCloseButtonVisible(value: true);
			mapCache.panel.Hide();
		}
	}

	private void OnMapSelect(int index)
	{
		CityMapPanel cityMapPanel = currentMap;
		if (cityMapPanel != null)
		{
			cityMapPanel.UnRegister();
			cityMapPanel.Hide();
		}
		selectedIndex = index;
		currentMap = GetAndInitMap(index);
		if (cityMapPanel != currentMap)
		{
			DolocAPI.HideHoverBox();
		}
		RefreshOperationTip();
		if (IsFullMap)
		{
			ZoomInMap();
		}
	}

	private CityMapPanel GetAndInitMap(int index)
	{
		MapCache obj = activeMaps[Mathf.Clamp(index, 0, activeMaps.Length - 1)];
		obj.panel.SetFocusedMission(focusedMissionId);
		obj.panel.Register();
		obj.panel.Show();
		return obj.panel;
	}

	private void GetMapFocused()
	{
		if (currentMap != null)
		{
			currentMap.GetFocused();
		}
	}

	public void SetFirstSelectedMap(string mapId)
	{
		firstSelectedMapId = mapId;
		focusedMissionId = null;
	}

	public void SetFocusedMission(string missionId)
	{
		firstSelectedMapId = null;
		focusedMissionId = missionId;
	}

	public void ClearFocusedData()
	{
		firstSelectedMapId = null;
		focusedMissionId = null;
	}

	public void SelectPrevMap()
	{
		titleMenu.FireClickLeft();
	}

	public void SelectNextMap()
	{
		titleMenu.FireClickRight();
	}

	public void EnableDetectCursor(bool value)
	{
		detectCursor = value;
	}

	public void UpdateCursorPositionByDelta(Vector2 delta)
	{
		if (detectCursor)
		{
			Vector2 screenPosition = currentCursorPosition + delta;
			UpdateCursorPosition(screenPosition);
			DolocAPI.cursorManager.SimulatePointerHover(screenPosition, delta, currentMap.latestHoveredObject);
		}
	}

	public void UpdateCursorPosition(Vector2 screenPosition)
	{
		if (detectCursor)
		{
			currentCursorPosition = screenPosition;
			currentMap.UpdateCursorPosition(screenPosition);
		}
	}

	public void SwitchMapSize()
	{
		if (IsFullMap)
		{
			ZoomOutMap();
		}
		else
		{
			ZoomInMap();
		}
	}

	public void MapScrolling(Vector2 dir)
	{
		currentMap.MapScrolling(dir);
	}

	public void MoveToAgentPosition()
	{
		currentMap.MoveToAgentPos();
	}

	private void ZoomOutMap()
	{
		MapTypeInfo mapProto = DolocConfig.Tables.TbMapType.Get(currentMap.MapId);
		CityMapPanel cityMapPanel = mapCaches.FirstOrDefault((MapCache map) => map.mapId == mapProto.MiniMap)?.panel;
		if (!(cityMapPanel == null))
		{
			activeMaps[selectedIndex].isFullMap = false;
			CityMapPanel cityMapPanel2 = currentMap;
			cityMapPanel2.UnRegister();
			cityMapPanel2.Hide();
			currentMap = cityMapPanel;
			if (cityMapPanel2 != currentMap)
			{
				DolocAPI.HideHoverBox();
			}
			currentMap.SetFocusedMission(focusedMissionId);
			currentMap.Register();
			currentMap.Show();
			GetMapFocused();
			RefreshOperationTip();
		}
	}

	private void ZoomInMap()
	{
		MapTypeInfo mapProto = DolocConfig.Tables.TbMapType.Get(currentMap.MapId);
		CityMapPanel cityMapPanel = mapCaches.FirstOrDefault((MapCache map) => map.mapId == mapProto.MainMap)?.panel;
		if (!(cityMapPanel == null))
		{
			activeMaps[selectedIndex].isFullMap = true;
			CityMapPanel cityMapPanel2 = currentMap;
			cityMapPanel2.UnRegister();
			cityMapPanel2.Hide();
			currentMap = cityMapPanel;
			if (cityMapPanel2 != currentMap)
			{
				DolocAPI.HideHoverBox();
			}
			currentMap.SetFocusedMission(focusedMissionId);
			currentMap.Register();
			currentMap.Show();
			GetMapFocused();
			RefreshOperationTip();
			if (!focusedMissionId.IsNullOrEmpty())
			{
				currentMap.MoveToMissionPos(focusedMissionId);
			}
			else
			{
				currentMap.MoveToAgentPos();
			}
		}
	}

	private void RefreshOperationTip()
	{
		MapTypeInfo mapTypeInfo = DolocConfig.Tables.TbMapType.Get(currentMap.MapId);
		if (mapTypeInfo.MiniMap.IsNullOrEmpty() && mapTypeInfo.MainMap.IsNullOrEmpty())
		{
			operationTip.SetTextKey(base.staticTexts.UiTipSwitchClassifying);
			return;
		}
		operationTip.SetTextKey(new string[3]
		{
			base.staticTexts.UiTipSwitchClassifying,
			base.staticTexts.UiTipSwitchMapSize,
			base.staticTexts.UiTipMapCentered
		});
	}
}
