using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Room;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CityMapPanel : DolocUIPanel
{
	private static readonly int VisibleMask = Shader.PropertyToID("_VisibleMask");

	private static readonly int FogMask = Shader.PropertyToID("_FogMask");

	private static readonly int HiddenMask = Shader.PropertyToID("_HiddenMask");

	private static readonly int Disable = Shader.PropertyToID("_Disable");

	[SerializeField]
	private string mapId;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private float distanceWeight = 1f;

	[SerializeField]
	private DolocNavigationButton agentPointer;

	[SerializeField]
	private DolocNavigationButton motorPointer;

	[SerializeField]
	private MapArea farmArea;

	[SerializeField]
	private bool useAreaHoverBox = true;

	[SerializeField]
	private bool useBigMissionIcon;

	private Dictionary<string, MapArea> areas = new Dictionary<string, MapArea>();

	private Dictionary<string, MapArea> roomAreaCache = new Dictionary<string, MapArea>();

	private HashSet<string> roomsWithMission = new HashSet<string>();

	[SerializeField]
	private RectTransform tipAnchor;

	[SerializeField]
	private Image cursor;

	[SerializeField]
	private CanvasGroup mapCanvasGroup;

	[SerializeField]
	private Image mapImage;

	[SerializeField]
	private Transform tipRoot;

	[SerializeField]
	private Transform indicatorRoot;

	[SerializeField]
	private Transform textRoot;

	[SerializeField]
	private GameObject bgMask;

	[SerializeField]
	private RectTransform customCursorArea;

	private ObjectPool<DolocNavigationButton> tipPool;

	private Dictionary<string, List<DolocNavigationButton>> missionTipCache = new Dictionary<string, List<DolocNavigationButton>>();

	private MissionTip missionTip;

	private MapTypeInfo mapTypeProto;

	private NashObjectPool<SingleImage> indicatorPool;

	private Vector2 originAchoredPosition;

	private string firstSelectMission;

	private DolocNavigationButton firstSelectMissionTip;

	private Action<Vector2> PointerPositionSetter;

	private bool hasInitCursorPosition;

	[SerializeField]
	private ScrollRect scrollRect;

	public string MapId => mapId;

	private MapManager mapManager => DolocAPI.archiveHandle.farmData.mapManager;

	public GameObject latestHoveredObject { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		originAchoredPosition = container.gameObject.GetComponent<RectTransform>().anchoredPosition;
		indicatorPool = new NashObjectPool<SingleImage>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_PFB_IMAGE), indicatorRoot);
		indicatorPool.OnCreate = delegate(SingleImage x)
		{
			x.SetSprite(DolocAPI.GetAsset<Sprite>("ui_miniicon_default"));
		};
		tipPool = new ObjectPool<DolocNavigationButton>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MAP_MISSION_TIP), tipRoot);
		ObjectPool<DolocNavigationButton> objectPool = tipPool;
		objectPool.OnRecycle = (Action<DolocNavigationButton>)Delegate.Combine(objectPool.OnRecycle, (Action<DolocNavigationButton>)delegate(DolocNavigationButton tip)
		{
			tip.iconMaterial = LocMaterials.UI_MAT_DEFAULT;
		});
		MapArea[] componentsInChildren = container.GetComponentsInChildren<MapArea>(includeInactive: true);
		foreach (MapArea mapArea in componentsInChildren)
		{
			if (mapArea.proto == null)
			{
				Debug.LogError("区域对象没有找到对应的配置！<" + mapArea.name + ">");
			}
			else
			{
				areas[mapArea.AreaId] = mapArea;
			}
		}
		InitAreas();
		mapCanvasGroup.alpha = 1f;
		SetBackgroundMaskActive(value: false);
		mapTypeProto = DolocConfig.Tables.TbMapType.GetOrDefault(mapId);
		agentPointer.Init();
		agentPointer.onPointerEnter.AddListener(delegate
		{
			latestHoveredObject = agentPointer.gameObject;
			AdjustCursorPosition(agentPointer.position);
			agentPointer.HoverText(new TextGroup("", base.staticTexts.UiTipCurrentPosition, DolocAPI.archiveHandle.currentSceneTitle));
		});
		agentPointer.onPointerExit.AddListener(delegate
		{
			if (latestHoveredObject == agentPointer.gameObject)
			{
				latestHoveredObject = null;
			}
			agentPointer.HideHoverBox();
		});
		agentPointer.iconSprite = mapTypeProto.PointerIcon.Asset;
		agentPointer.rectTransform.sizeDelta = agentPointer.iconSprite.rect.size * 4f;
		motorPointer.Init();
		motorPointer.onPointerEnter.AddListener(delegate
		{
			latestHoveredObject = motorPointer.gameObject;
			AdjustCursorPosition(motorPointer.position);
			motorPointer.HoverText(new TextGroup("", base.staticTexts.UiTipCurrentMotorPosition, DolocAPI.archiveHandle.farmData.agentData.motorData.CurrentRoom.SceneConfig.Title));
		});
		motorPointer.onPointerExit.AddListener(delegate
		{
			if (latestHoveredObject == motorPointer.gameObject)
			{
				latestHoveredObject = null;
			}
			motorPointer.HideHoverBox();
		});
		cursor.sprite = mapTypeProto.CursorIcon.Asset;
		cursor.rectTransform.sizeDelta = cursor.sprite.rect.size * 4f;
		if (mapTypeProto.UseMask && mapImage != null)
		{
			if (mapManager.GetMask(mapId, out var visibleRoomMask, out var fogMask, out var hiddenAreaMask))
			{
				mapImage.material.SetFloat(Disable, 0f);
				mapImage.material.SetTexture(VisibleMask, visibleRoomMask);
				mapImage.material.SetTexture(FogMask, fogMask);
				mapImage.material.SetTexture(HiddenMask, hiddenAreaMask);
			}
			else
			{
				Debug.LogError("地图<" + mapId + ">获取遮罩失败！");
			}
		}
	}

	private void InitAreas()
	{
		foreach (MapArea value in areas.Values)
		{
			value.Init();
			MapAreaType areaType = value.AreaType;
			if (areaType == MapAreaType.Room || areaType == MapAreaType.Fixpoint)
			{
				if (roomAreaCache.ContainsKey(value.proto.RoomName))
				{
					Debug.LogError("MapPanel: 房间区域<" + value.proto.RoomName + ">重复添加！go<" + value.gameObject.name + ">");
				}
				roomAreaCache[value.proto.RoomName] = value;
			}
		}
	}

	public void SetOffset(Vector2 offset)
	{
		container.gameObject.GetComponent<RectTransform>().anchoredPosition = originAchoredPosition + offset;
		mapCanvasGroup.gameObject.GetComponent<RectTransform>().anchoredPosition = originAchoredPosition + offset;
	}

	public void GetFocused()
	{
	}

	public void Register()
	{
		RegisterAgentPosition();
		RegisterMotorPosition();
		RegisterMissionTips();
		RegisterLabelAreas();
		RefreshHiddenAreas();
		RefreshAreaTextLabel();
		RefreshConditionVisible();
	}

	public void UnRegister()
	{
		roomsWithMission.Clear();
		foreach (MapArea value in areas.Values)
		{
			if (!(value.mapPoint == null))
			{
				value.mapPoint.onPointerEnter.RemoveAllListeners();
				value.mapPoint.onPointerEnter.RemoveAllListeners();
			}
		}
		ClearMissionTips();
	}

	private void RegisterAgentPosition()
	{
		if (GetMapPosByWorldPosition(DolocAPI.CurrentRoom, DolocAPI.agent.PositionCenter, out var mapPosition))
		{
			agentPointer.position = mapPosition;
			if (firstSelectMission.IsNullOrEmpty())
			{
				hasInitCursorPosition = true;
			}
			agentPointer.gameObject.SetActive(value: true);
			agentPointer.iconMaterial = LocMaterials.UI_MAT_FLOAT_ARROW_VERTICAL;
		}
		else
		{
			agentPointer.gameObject.SetActive(value: false);
			agentPointer.position = Vector2.zero;
		}
	}

	private void RegisterMotorPosition()
	{
		motorPointer.gameObject.SetActive(value: false);
		MotorDataManager motorData = DolocAPI.archiveHandle.farmData.agentData.motorData;
		if (!DolocAPI.IsAgentRiding && motorData.isUnlocked && GetMapPosByWorldPosition(motorData.CurrentRoom, motorData.position, out var mapPosition))
		{
			motorPointer.position = mapPosition;
			motorPointer.gameObject.SetActive(value: true);
		}
	}

	private void RegisterLabelAreas()
	{
		foreach (MapArea area in areas.Values)
		{
			if (area.mapPoint == null || area.AreaType != MapAreaType.Label || area.proto == null || area.proto.CoveredRooms.Contains(DolocAPI.CurrentRoom.RoomId) || roomsWithMission.Intersect(area.proto.CoveredRooms).Any())
			{
				area.SetVisible(value: false);
				continue;
			}
			if (!useAreaHoverBox)
			{
				break;
			}
			area.mapPoint.onPointerEnter.AddListener(delegate
			{
				latestHoveredObject = area.mapPoint.gameObject;
				tipAnchor.position = area.topCenterPos;
				AdjustCursorPosition(area.centerPos);
				tipAnchor.HoverTextSmall(area.Label);
			});
			area.mapPoint.onPointerExit.AddListener(delegate
			{
				if (latestHoveredObject == area.mapPoint.gameObject)
				{
					latestHoveredObject = null;
				}
				tipAnchor.HideHoverBox();
			});
		}
	}

	private void RegisterMissionTips()
	{
		missionTipCache.Clear();
		tipPool.RecycleAll();
		IMission[] missions = DolocAPI.archiveHandle.GetMissions();
		if (missions.IsNullOrEmpty())
		{
			return;
		}
		MapTipData[] array = (from mission in missions
			select new MapTipData(mission) into data
			where data.notEmpty
			select data).ToArray();
		if (array.IsNullOrEmpty())
		{
			return;
		}
		int count = array.Sum((MapTipData data) => data.mapTipCount);
		tipPool.CheckCount(count);
		int num = 0;
		MapTipData[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			MapTipData mapTipData = array2[i];
			for (int j = 0; j < mapTipData.mapTipCount; j++)
			{
				DolocNavigationButton tip = tipPool[num++];
				MapMissionTipType mapMissionTipType = mapTipData.tipTypes[j];
				Room room;
				bool flag = DolocAPI.QueryRoom(mapTipData.roomIds[j], out room);
				Vector2 mapPosition = Vector2.zero;
				if (flag)
				{
					switch (mapMissionTipType)
					{
					case MapMissionTipType.None:
						flag = false;
						break;
					case MapMissionTipType.MarkPoint:
					case MapMissionTipType.NPC:
						flag &= GetMapPosByWorldPosition(room, mapTipData.worldPositions[j], out mapPosition);
						break;
					case MapMissionTipType.Room:
						flag &= GetMapPosByRoomDefault(room, out mapPosition);
						break;
					default:
						throw new ArgumentOutOfRangeException();
					}
				}
				if (!flag)
				{
					tip.gameObject.SetActive(value: false);
					continue;
				}
				missionTipCache.TryAdd(mapTipData.missionId, new List<DolocNavigationButton>());
				missionTipCache[mapTipData.missionId].Add(tip);
				tip.position = mapPosition;
				roomsWithMission.Add(mapTipData.roomIds[j]);
				TextGroup textGroup = new TextGroup(mapTipData.missionTitle, mapTipData.positionInfos[j], mapTipData.missionTip);
				tip.onPointerEnter.AddListener(delegate
				{
					latestHoveredObject = tip.gameObject;
					AdjustCursorPosition(tip.position);
					tip.HoverText(textGroup);
				});
				tip.onPointerExit.AddListener(delegate
				{
					if (latestHoveredObject == tip.gameObject)
					{
						latestHoveredObject = null;
					}
					tip.HideHoverBox();
				});
				SetSprite(tip.iconImg, useBigMissionIcon ? mapTipData.missionTypeBigIcons[j] : mapTipData.missionTypeIcons[j], autoSize: true);
			}
		}
		SelectMission(firstSelectMission);
	}

	private void RefreshHiddenAreas()
	{
		indicatorPool.RecycleAll();
		MapAreaInfo[] indicatedHiddenAreas = mapManager.GetIndicatedHiddenAreas(mapId);
		foreach (MapAreaInfo mapAreaInfo in indicatedHiddenAreas)
		{
			if (areas.TryGetValue(mapAreaInfo.AreaId, out var value))
			{
				indicatorPool.Next.position = value.centerPos;
			}
		}
	}

	private void RefreshAreaTextLabel()
	{
		if (!(textRoot == null))
		{
			MapText[] componentsInChildren = textRoot.GetComponentsInChildren<MapText>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].RefreshText(out var _);
			}
		}
	}

	private void RefreshConditionVisible()
	{
		ConditionalVisible[] componentsInChildren = mapImage.gameObject.GetComponentsInChildren<ConditionalVisible>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ValidateCondition();
		}
	}

	private void ClearMissionTips()
	{
		foreach (DolocNavigationButton item in tipPool)
		{
			item.onPointerEnter.RemoveAllListeners();
			item.onPointerExit.RemoveAllListeners();
		}
		missionTipCache.Clear();
		tipPool.RecycleAll();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		foreach (DolocNavigationButton item in tipPool)
		{
			item.iconMaterial = LocMaterials.UI_MAT_DEFAULT;
		}
		hasInitCursorPosition = false;
		PointerPositionSetter = null;
		firstSelectMissionTip = null;
	}

	protected override void OnFinishHide()
	{
		base.OnFinishHide();
		firstSelectMission = string.Empty;
		SetBackgroundMaskActive(value: false);
		DolocAPI.HideHoverBox();
	}

	private bool GetMapPosByRoomDefault(Room room, out Vector2 mapPosition)
	{
		mapPosition = Vector2.zero;
		if (room == null)
		{
			return false;
		}
		MapArea value = null;
		if (room.Type == RoomType.Farm && DolocAPI.archiveHandle.GetMaxLevelFarmProto(out var _))
		{
			value = farmArea;
		}
		else if (!roomAreaCache.TryGetValue(room.SceneRawName, out value) && !roomAreaCache.TryGetValue(room.RoomId, out value))
		{
			return false;
		}
		mapPosition = value.centerPos;
		return true;
	}

	public bool GetMapPosByWorldPosition(string roomId, Vector2 worldPosition, out Vector2 mapPosition)
	{
		DolocAPI.QueryRoom(roomId, out var room);
		return GetMapPosByWorldPosition(room, worldPosition, out mapPosition);
	}

	public bool GetMapPosByWorldPosition(Room room, Vector2 worldPosition, out Vector2 mapPosition)
	{
		mapPosition = Vector2.zero;
		if (room == null)
		{
			return false;
		}
		Vector2 vector;
		if (room.Type == RoomType.Farm && DolocAPI.archiveHandle.GetMaxLevelFarmProto(out var roomProto))
		{
			if (farmArea == null)
			{
				return false;
			}
			if (room.IsInHouse && ((IBuildingHost)DolocAPI.archiveHandle.MainFarm).DM_building.QueryBuildingByRoomTitle(room.Title, out var building))
			{
				worldPosition = building.Position;
			}
			vector = roomProto.geometry.CalPosPercent(worldPosition);
			mapPosition = farmArea.GetPosByPercent(vector.x, vector.y);
			return true;
		}
		if (!roomAreaCache.TryGetValue(room.SceneRawName, out var value) && !roomAreaCache.TryGetValue(room.RoomId, out value))
		{
			return false;
		}
		vector = room.Geometry.CalPosPercent(worldPosition);
		mapPosition = value.GetPosByPercent(vector.x, vector.y);
		return true;
	}

	public void SetFocusedMission(string missionId)
	{
		firstSelectMission = missionId;
	}

	public void SetBackgroundMaskActive(bool value)
	{
		bgMask.gameObject.SetActive(value);
	}

	private bool SelectMission(string missionId)
	{
		missionTipCache.TryGetValue(missionId ?? string.Empty, out var value);
		DolocNavigationButton dolocNavigationButton = value?.First();
		if (dolocNavigationButton != null)
		{
			dolocNavigationButton.iconMaterial = LocMaterials.UI_MAT_FLOAT_ARROW_VERTICAL;
			agentPointer.iconMaterial = LocMaterials.UI_MAT_DEFAULT;
			hasInitCursorPosition = true;
			firstSelectMissionTip = dolocNavigationButton;
			return true;
		}
		firstSelectMission = missionId;
		return false;
	}

	public bool ContainsMission(string missionId)
	{
		IMission[] missions = DolocAPI.archiveHandle.GetMissions();
		if (missions.IsNullOrEmpty())
		{
			return false;
		}
		MapTipData[] array = (from mission in missions
			select new MapTipData(mission) into data
			where data.notEmpty
			select data).ToArray();
		if (array.IsNullOrEmpty())
		{
			return false;
		}
		MapTipData[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			MapTipData mapTipData = array2[i];
			if (mapTipData.missionId != missionId)
			{
				continue;
			}
			int num = 0;
			if (num >= mapTipData.mapTipCount)
			{
				continue;
			}
			Room room;
			bool flag = DolocAPI.QueryRoom(mapTipData.roomIds[num], out room);
			if (flag)
			{
				Vector2 mapPosition;
				switch (mapTipData.tipTypes[num])
				{
				case MapMissionTipType.None:
					flag = false;
					break;
				case MapMissionTipType.MarkPoint:
				case MapMissionTipType.NPC:
					flag &= GetMapPosByWorldPosition(room, mapTipData.worldPositions[num], out mapPosition);
					break;
				case MapMissionTipType.Room:
					flag &= GetMapPosByRoomDefault(room, out mapPosition);
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
			return flag;
		}
		return false;
	}

	public void UpdateCursorPosition(Vector2 screenPosition)
	{
		SetCursorPosition(screenPosition);
	}

	private void SetCursorPosition(Vector2 screenPosition)
	{
		bool active = RectTransformUtility.RectangleContainsScreenPoint(customCursorArea, screenPosition, null);
		cursor.transform.position = screenPosition;
		cursor.gameObject.SetActive(active);
	}

	public void SetPointerPositionSetter(Action<Vector2> setter)
	{
		PointerPositionSetter = setter;
	}

	private void AdjustCursorPosition(Vector2 screenPosition)
	{
	}

	public bool GetInitCursorPosition(out Vector2 pos)
	{
		pos = Vector2.zero;
		if (!hasInitCursorPosition)
		{
			return false;
		}
		if (firstSelectMissionTip != null)
		{
			pos = firstSelectMissionTip.position;
			return true;
		}
		if (agentPointer.gameObject.activeSelf)
		{
			pos = agentPointer.position;
			return true;
		}
		return false;
	}

	public void MoveToAgentPos()
	{
		SetMapContentPos(new Vector2(0f - agentPointer.anchoredPositionX, scrollRect.content.rect.height / 2f - agentPointer.anchoredPositionY));
	}

	public void MoveToMissionPos(string missionId)
	{
		missionTipCache.TryGetValue(missionId ?? string.Empty, out var value);
		DolocNavigationButton dolocNavigationButton = value?.First();
		if (!(dolocNavigationButton == null))
		{
			SetMapContentPos(new Vector2(0f - dolocNavigationButton.anchoredPositionX, scrollRect.content.rect.height / 2f - dolocNavigationButton.anchoredPositionY));
		}
	}

	public void MapScrolling(Vector2 dir)
	{
		if (!(scrollRect == null))
		{
			Vector2 vector = scrollRect.content.anchoredPosition;
			vector.x -= dir.x;
			vector.y -= dir.y;
			scrollRect.content.anchoredPosition = vector;
		}
	}

	private void SetMapContentPos(Vector2 pos)
	{
		if (!(scrollRect == null))
		{
			scrollRect.horizontalNormalizedPosition = 0.5f;
			scrollRect.verticalNormalizedPosition = 0.5f;
			scrollRect.content.anchoredPosition = pos;
		}
	}
}
