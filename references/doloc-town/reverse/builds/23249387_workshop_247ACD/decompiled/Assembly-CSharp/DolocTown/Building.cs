using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public class Building : TerrainContent
{
	public static Building LastEnterBuilding;

	[JsonProperty]
	private float health;

	public BuildingSupport buildingSupport;

	protected bool isRender;

	private BuildingLinkGateMap _linkGateMap;

	protected bool isTouching;

	protected bool isDoorTouching;

	public BuildingInfo proto { get; private set; }

	[JsonProperty]
	public TemplateRoomInHouse room { get; private set; }

	[JsonProperty]
	public string uuid { get; private set; }

	[JsonProperty]
	public string BuildingName => proto.Id;

	[JsonProperty]
	public bool isClosed { get; private set; }

	[JsonProperty]
	public string customName { get; private set; }

	[JsonProperty]
	public string wallpaperId { get; private set; }

	[JsonProperty]
	public string exteriorId { get; private set; }

	[JsonProperty]
	public int level { get; private set; }

	public BuildingLevelData currentLevelData => proto.GetLevelData(level);

	public string templateRoomName => currentLevelData.TemplateRoomName;

	public Sprite SceneSprite
	{
		get
		{
			if (exteriorData == null)
			{
				return proto.DefaultSceneSprite;
			}
			if (!isClosed)
			{
				Sprite asset = exteriorData.OpenedSpriteGroup.SceneSprite.Asset;
				if (asset != null)
				{
					return asset;
				}
			}
			return exteriorData.ClosedSpriteGroup.SceneSprite.Asset;
		}
	}

	public Sprite DoorSprite
	{
		get
		{
			if (exteriorData == null)
			{
				return null;
			}
			if (!isClosed && exteriorData.CanOpenDoor)
			{
				Sprite asset = exteriorData.OpenedSpriteGroup.DoorSprite.Asset;
				if (asset != null)
				{
					return asset;
				}
			}
			return exteriorData.ClosedSpriteGroup.DoorSprite.Asset;
		}
	}

	public Sprite UiSprite
	{
		get
		{
			Sprite sprite = exteriorData?.OverrideItemIcon.Asset;
			if (!(sprite != null))
			{
				return proto.UiSpriteAsset.Asset;
			}
			return sprite;
		}
	}

	public BuildingRenderer Renderer { get; set; }

	public string Title
	{
		get
		{
			if (!customName.IsNullOrEmpty())
			{
				return customName;
			}
			return proto.Title;
		}
		set
		{
			customName = ((value == proto.Title) ? "" : value);
		}
	}

	public string TargetRoomGuid
	{
		get
		{
			if (proto.IsUnique)
			{
				return uuid;
			}
			return room.Title;
		}
	}

	private Vector2 WorldSize
	{
		get
		{
			if (SceneSprite == null)
			{
				return Vector2.zero;
			}
			return SceneSprite.rect.size * 0.125f;
		}
	}

	public Vector2Int InnerSize
	{
		get
		{
			DolocAPI.QueryTemplateRoom(templateRoomName, out var roomProto);
			return roomProto.geometry.gridSize;
		}
	}

	public override bool IsRemoved => !room.RootRoom.DM_building.Buildings.Contains(this);

	public Vector3 PositionBottom => new Vector3(base.Position.x, base.Position.y, 0f);

	public Vector3 PositionTop => new Vector3(base.Position.x, base.Position.y + WorldSize.y, 0f);

	public Vector3 PositionCenter => new Vector3(base.Position.x, base.Position.y + WorldSize.y * 0.5f, 0f);

	public Vector3 PositionLB => (Vector2)base.Position - SceneSprite.pivot * 0.125f;

	public Vector2Int GlobalAnchor => BuilderUtils.GetRealGridPos(base.Anchor, room.RootRoom.RoomPosition);

	public Vector2 EntryPosition => (Vector2)PositionLB + proto.EntryPosition - new Vector2(0f, 0.01f);

	[DebugInfo("地窖边界检测")]
	public bool IsOverCellarBorder
	{
		get
		{
			if (!DolocAPI.IsGameInitialized)
			{
				return false;
			}
			if (DolocAPI.archiveHandle?.MainFarm == null)
			{
				return false;
			}
			float num = PositionLB.x + (float)proto.CoverSize.x * 1.5f;
			Vector2 roomPosition = DolocAPI.archiveHandle.MainFarm.RoomPosition;
			Vector2 roomSize = DolocAPI.archiveHandle.MainFarm.RoomSize;
			float num2 = roomPosition.x + roomSize.x - DolocAPI.GlobalParameter.CellarDistanceToFarmRight;
			return num <= num2;
		}
	}

	public Vector3 ArrowTipPosition => (Vector2)PositionLB + (Vector2)proto.ArrowTipPixelOffset * 0.125f;

	public Vector3 HealthTipPosition => (Vector2)PositionLB + (Vector2)proto.HealthTipPixelOffset * 0.125f;

	public Vector3 PositionTip => ArrowTipPosition;

	public Vector3 PositionAssistTip => ArrowTipPosition + new Vector3(0f, 1.5f);

	public float HealthProcess => health * proto.HealthReciprocal;

	public float Health => health;

	public bool IsBroken => health <= 0f;

	public bool IsIntact => health >= proto.Health;

	public bool CanPatch => proto.HasHealthInfo;

	public bool IsUnique => proto.IsUnique;

	[DebugInfo("联通门映射数据")]
	public BuildingLinkGateMap LinkGateMap
	{
		get
		{
			if (_linkGateMap == null)
			{
				_linkGateMap = GenLinkMap();
			}
			return _linkGateMap;
		}
	}

	public bool canUpgrade => level < maxLevel;

	public int maxLevel => proto.MaxLevel;

	public BuildingWallpaperData wallpaperData => GetWallpaperData(wallpaperId);

	public BuildingExteriorData exteriorData => GetExteriorData(exteriorId);

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]>
		{
			{
				TerrainLayerName.BuildingFront,
				proto.GetPositionsOfType(anchor, BuildingTileType.Front)
			},
			{
				TerrainLayerName.BuildingSide,
				proto.GetPositionsOfType(anchor, BuildingTileType.Side)
			},
			{
				TerrainLayerName.CeilingFront,
				proto.GetPositionsOfType(anchor, BuildingTileType.Ceiling, BuildingTileType.Side)
			},
			{
				TerrainLayerName.CeilingSide,
				proto.GetPositionsOfType(anchor, BuildingTileType.Ceiling, BuildingTileType.Front)
			},
			{
				TerrainLayerName.Door,
				proto.GetPositionsOfType(anchor, BuildingTileType.Door)
			},
			{
				TerrainLayerName.BuildingNoOverlay,
				proto.GetPositionsOfType(anchor, BuildingTileType.NoOverlay)
			}
		};
	}

	public Building(Room parent, BuildingInfo proto, RoomProto protoInHouse, Vector2Int anchor, Vector2 positionWS)
		: base(-1, anchor, positionWS)
	{
		this.proto = proto;
		uuid = Guid.NewGuid().ToString();
		health = proto.Health;
		customName = null;
		if (proto.IsUnique)
		{
			room = ((IBuildingHost)parent.CurrentRoom).FindFirstBuildingRoomById(proto.Id);
			if (room != null)
			{
				this.proto = room.Building.proto;
			}
		}
		if (room == null)
		{
			TemplateRoomInHouse templateRoomInHouse2 = (room = new TemplateRoomInHouse(Guid.NewGuid().ToString(), parent, protoInHouse)
			{
				Building = this
			});
		}
		exteriorId = proto.DefaultExterior;
		wallpaperId = proto.DefaultWallpaper;
		isClosed = !proto.IsAnimalBuilding;
	}

	public void InvokeChangeNameInputBox(Action onConfirm = null)
	{
		DolocAPI.EnterUI((RenamingUiState state) => state.HandleStartUpArgs(DolocConfig.StaticTexts.BuildingPanelInputBuildingName, Title, proto.Title, delegate(string text)
		{
			Title = text;
			onConfirm?.Invoke();
		}, DolocAPI.GlobalParameter.InputPlayerNameMaxLength, delegate
		{
			string prompt = string.Format(DolocConfig.StaticTexts.UiOperationEnterFormat, Title);
			this.ChangeSceneOperationTipPrompt(prompt);
		}));
	}

	public void SetRoom(TemplateRoomInHouse newRoom)
	{
		room = newRoom;
	}

	public void SetClosed(bool value)
	{
		isClosed = value;
		RefreshRender();
	}

	public void RefreshRender()
	{
		if (isRender && !(Renderer == null))
		{
			Renderer.spriteRenderer.sprite = SceneSprite;
			Renderer.ShowOutline = false;
			Renderer.ShowOutline = isDoorTouching;
			Renderer.InitMaterialInfos();
		}
	}

	public void Damage(float value)
	{
		value = Mathf.Max(0f, value);
		ComposeHealth(0f - value);
		if (isRender && isTouching)
		{
			Renderer.OnDamage();
		}
	}

	public void Repair(float value)
	{
		value = Mathf.Max(0f, value);
		ComposeHealth(value);
		if (isRender && isTouching)
		{
			Renderer.OnRepair();
		}
	}

	private void ComposeHealth(float value)
	{
		health += value;
		if (health <= 0f)
		{
			room.SetBroken(value: true);
			health = 0f;
		}
		else
		{
			room.SetBroken(value: false);
			health = Mathf.Min(health, proto.Health);
		}
	}

	public void Dismantle(Action afterDismantle = null)
	{
		Room rootRoom = room.RootRoom;
		IBuildingHost host = rootRoom;
		if (host == null)
		{
			return;
		}
		if (room.DM_animal.AllAnimals.Any())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrBuildingOccupiedAnimal);
			return;
		}
		if (room.DM_equipment.AllEquipments.Any((Equipment equipment) => equipment.IsOccupy))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrBuildingOccupied);
			return;
		}
		if (!host.CanRemoveBuilding(this))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrBuildingInvalidOtherNoSupport);
			return;
		}
		Equipment[] equipments = GetBuildingCeilingEquipments().ToArray();
		if (equipments.Any((Equipment equipment) => equipment.IsOccupy))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrBuildingOccupiedCeiling);
			return;
		}
		DolocAPI.ShowQuestionBox(DolocConfig.StaticTexts.FarmbuilderQuestionRemove, delegate
		{
			Equipment[] array = equipments;
			foreach (Equipment equipment2 in array)
			{
				((IEquipmentHost)host).RemoveEquipment(equipment2);
			}
			host.RemoveBuilding(this);
			DolocAPI.RefreshScanner();
			afterDismantle?.Invoke();
		}, null, firstSelectConfirm: false);
	}

	[JsonConstructor]
	protected Building(int id, string uuid, Vector2Int anchor, Vector3 position, string buildingName, TemplateRoomInHouse room, float health, bool isClosed, int level, string wallpaperId, string exteriorId, string customName = null, Wallpaper wallpaper = null)
		: base(id, anchor, position)
	{
		ValidateData(buildingName, level, out buildingName, out level);
		if (wallpaperId.IsNullOrEmpty() && wallpaper != null && !wallpaper.wallpaperName.IsNullOrEmpty())
		{
			wallpaperId = wallpaper.wallpaperName;
		}
		proto = DolocConfig.Tables.TbBuilding.GetOrDefault(buildingName);
		this.room = room;
		this.health = health;
		this.room.Building = this;
		this.isClosed = isClosed;
		this.customName = customName;
		this.uuid = uuid ?? Guid.NewGuid().ToString();
		this.wallpaperId = wallpaperId;
		if (wallpaperData == null)
		{
			this.wallpaperId = proto.DefaultWallpaper;
		}
		this.exteriorId = exteriorId;
		if (exteriorData == null)
		{
			this.exteriorId = proto.DefaultExterior;
		}
	}

	private void ValidateData(string id, int level, out string validId, out int validlevel)
	{
		validId = id;
		validlevel = level;
		switch (id)
		{
		case "cellar_2":
			validId = "cellar";
			validlevel = 1;
			break;
		case "cellar_3":
			validId = "cellar";
			validlevel = 2;
			break;
		case "cellar_4":
			validId = "cellar";
			validlevel = 3;
			break;
		}
	}

	protected override bool ValidateDeserialization()
	{
		return proto != null;
	}

	public void AfterNewGame()
	{
		room.__AfterNewGame();
	}

	public void AfterLoadData()
	{
		room.__AfterLoadData();
	}

	public bool TryGetLinkGatePosition(BuildingLinkType type, int index, out Vector2 position)
	{
		return proto.TryGetLinkGatePosition(level, type, index, out position);
	}

	private Vector2 GetEntryPosition(Room room)
	{
		if (room == null)
		{
			return default(Vector2);
		}
		Vector2 defaultEntryPosition = room.Geometry.DefaultEntryPosition;
		if (!(room is TemplateRoomInHouse templateRoomInHouse) || !templateRoomInHouse.Building.IsUnique)
		{
			return defaultEntryPosition;
		}
		defaultEntryPosition.x = this.GetCellarGatePosition();
		return defaultEntryPosition;
	}

	public Vector2 BuildingToWorld(Vector2 position)
	{
		Vector2 entryPosition = GetEntryPosition(room);
		float num = position.x - entryPosition.x;
		Vector2 entryPosition2 = EntryPosition;
		return new Vector2(entryPosition2.x + num, entryPosition2.y);
	}

	public virtual void OnEnter()
	{
		if (DolocAPI.agent.IsCurrentStateSupportTeleport)
		{
			_linkGateMap = null;
			this.PushSceneOperationTipToHide();
			Vector2 entryPosition = GetEntryPosition(room);
			DolocAPI.EnterFarm(TargetRoomGuid, entryPosition);
			DolocAPI.BroadcastString(GameEventType.ARRIVE_ROOM_BUILDING, BuildingName);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_BUILDING);
			DolocAPI.Broadcast(OperationEventType.ENTER_BUILDING);
			if (IsUnique && this.IsOverlappedCellarBuilding())
			{
				LastEnterBuilding = this;
			}
		}
	}

	public virtual void OnRender()
	{
		isRender = true;
		Renderer.OnRender();
	}

	public virtual void OnUnRender()
	{
		isRender = false;
		Renderer.OnUnRender();
	}

	public virtual void OnMove()
	{
		OnUnRender();
		OnRender();
		buildingSupport = null;
	}

	public virtual void OnRemove()
	{
	}

	public void RetrieveItemOnRemoval(bool putInBackpack)
	{
		CountItem[] itemCost = proto.ItemCost;
		for (int i = 0; i < itemCost.Length; i++)
		{
			CountItem countItem = itemCost[i];
			PlaceItemInBagOrCreateDropItem(DolocAPI.GenerateItem(countItem.itemName, countItem.itemCount), putInBackpack, sendMessage: true);
		}
		AddMoneyOrCreateDropItem((int)((float)proto.MoneyCost * DolocAPI.GlobalParameter.DemolitionRecycleFactor), putInBackpack, sendMessage: false);
		if (proto.IsUnique)
		{
			return;
		}
		((IEquipmentHost)room).RemoveAllEquipment();
		foreach (DropItemBase allData in room.DM_dropitem.AllDatas)
		{
			if (!(allData is DropItem dropItem))
			{
				if (!(allData is SpecialDropItem specialDropItem))
				{
					if (allData is DropItemMoney dropItemMoney)
					{
						AddMoneyOrCreateDropItem(dropItemMoney.Count, putInBackpack, allData.ShouldSendMsg);
					}
				}
				else
				{
					PlaceItemInBagOrCreateDropItem(specialDropItem.DropItem, putInBackpack, allData.ShouldSendMsg);
				}
			}
			else
			{
				PlaceItemInBagOrCreateDropItem(DolocAPI.GenerateItem(dropItem.ItemName), putInBackpack, allData.ShouldSendMsg);
			}
		}
		room.DM_dropitem.Clear();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Platform totalPlatform in room.DM_platform.totalPlatforms)
		{
			dictionary.TryAdd(totalPlatform.proto.Id, 0);
			dictionary[totalPlatform.proto.Id] += totalPlatform.geometry.TileCount;
		}
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			int num = item.Value;
			DolocAPI.QueryItemProto(item.Key, out var itemInfo);
			int b = ((num % 10 == 0) ? (num / 10) : (num / 10 + 1));
			while (num > 0)
			{
				int num2 = Mathf.Min(num, b);
				PlaceItemInBagOrCreateDropItem(DolocAPI.GenerateItem(itemInfo, num2), putInBackpack, sendMessage: false);
				num -= num2;
			}
		}
	}

	public IEnumerable<Equipment> GetBuildingCeilingEquipments()
	{
		List<Equipment> list = new List<Equipment>();
		Vector2Int[] array = proto.GetPositionsOfType(base.Anchor, BuildingTileType.Ceiling, BuildingTileType.Side, Vector2Int.up).ToArray();
		foreach (Equipment contentsFromPosition in room.RootRoom.DM_terrain.GetContentsFromPositions<Equipment>(array))
		{
			if (!contentsFromPosition.proto.isDecal && (array.Contains(contentsFromPosition.Anchor) || array.Contains(contentsFromPosition.RightBottom)) && !room.RootRoom.DM_terrain.AllFilled(contentsFromPosition.proto.GroundPositions(contentsFromPosition.Anchor), TerrainLayerName.Structure))
			{
				list.Add(contentsFromPosition);
			}
		}
		return list;
	}

	private void PlaceItemInBagOrCreateDropItem(Item item, bool putInBackpack, bool sendMessage)
	{
		if (item != null)
		{
			if (putInBackpack)
			{
				DolocAPI.RaiseItemObtainTip(item.name, item.uiSprite, item.title, item.count);
				item = DolocAPI.PlaceItem(item, DolocAPI.userSettings.autoUseBox, useFade: true);
			}
			if (item != null)
			{
				((IDropItemHost)room.RootRoom).CreateDropItem(item, (Vector2)base.Position, sendMessage, 0f);
			}
		}
	}

	private void AddMoneyOrCreateDropItem(int count, bool putInBackpack, bool sendMessage)
	{
		if (putInBackpack)
		{
			DolocAPI.RaiseItemObtainTip("money", LocSprites.UI_ICON_GOLD28X, DolocConfig.StaticTexts.ItemMoneyTitle, count);
			if (sendMessage)
			{
				DolocAPI.archiveHandle.CurrentMoney += count;
			}
			else
			{
				DolocAPI.archiveHandle.SetCurrentMoney(DolocAPI.archiveHandle.CurrentMoney + count, out var _);
			}
		}
		else
		{
			((IDropItemHost)room.RootRoom).CreateDropItemMoney(count, (Vector2)base.Position, sendMessage);
		}
	}

	public BuildingLinkGateMap GenLinkMap(bool shouldCalculateRemote = true)
	{
		IBuildingHost rootRoom = room.RootRoom;
		return BuildingLinkGateMap.GenLinkGateMap(this, rootRoom.DM_building.Buildings, shouldCalculateRemote);
	}

	public bool Upgrade()
	{
		if (!canUpgrade)
		{
			return false;
		}
		BuildingLevelData levelData = proto.GetLevelData(level + 1);
		if (!DolocAPI.assets.rooms.QueryData(levelData.TemplateRoomName, out var data))
		{
			Debug.LogWarning("未找到名为 \"" + levelData.TemplateRoomName + "\" 的房间原型，无法扩展农场");
			return false;
		}
		level = Mathf.Clamp(level + 1, 0, maxLevel);
		return room.TerrainExtend(data);
	}

	public virtual void OnTouch()
	{
		isTouching = true;
		if (proto.HasHealthInfo)
		{
			Renderer.ShowHealthTip();
		}
	}

	public virtual void OnDisTouch()
	{
		isTouching = false;
		isDoorTouching = false;
		if (proto.HasHealthInfo && Renderer != null)
		{
			Renderer.HideHealthTip();
		}
	}

	public void OnInteract()
	{
		if (exteriorData.CanOpenDoor)
		{
			SetClosed(!isClosed);
			if (isRender)
			{
				DolocAPI.Sound.PostSoundEvent(isClosed ? SoundEvents.PLAY_ANIMAL_CLOSE_DOOR : SoundEvents.PLAY_ANIMAL_OPEN_DOOR);
				room.ChangeSceneOperationTipPrompt(GetInteractText());
				RefreshRender();
			}
		}
	}

	public void SetNumberTipVisibleIfTouching(bool value)
	{
		if (isRender && isTouching)
		{
			if (value)
			{
				Renderer.ShowHealthTip();
			}
			else
			{
				Renderer.HideHealthTip();
			}
		}
	}

	public void SetTouchDoorState(bool value)
	{
		isDoorTouching = value;
		if (value)
		{
			string prompt = string.Format(DolocConfig.StaticTexts.UiOperationEnterFormat, Title);
			this.ShowSceneOperationTip(PositionTip, prompt, DolocAPI.UserInput.NormalRoomInteractActionName);
			if (exteriorData.CanOpenDoor)
			{
				room.ShowSceneOperationTip(PositionAssistTip, GetInteractText(), DolocAPI.UserInput.GlobalInteractActionName);
			}
		}
		else
		{
			this.HideSceneOperationTip();
			room.HideSceneOperationTip();
		}
		if (isRender && Renderer != null)
		{
			Renderer.ShowOutline = value;
		}
	}

	private string GetInteractText()
	{
		if (!isClosed)
		{
			return DolocConfig.StaticTexts.UiOperationCloseDoor;
		}
		return DolocConfig.StaticTexts.UiOperationOpenDoor;
	}

	private BuildingWallpaperData GetWallpaperData(string id)
	{
		Dictionary<string, BuildingWallpaperData> dictionary = DolocConfig.Tables.TbBuildingWallpaper.GetOrDefault(id ?? "")?.WallpaperDatas_Index;
		if (dictionary != null && dictionary.TryGetValue(BuildingName, out var value))
		{
			return value;
		}
		return null;
	}

	private BuildingExteriorData GetExteriorData(string id)
	{
		Dictionary<string, BuildingExteriorData> dictionary = DolocConfig.Tables.TbBuildingExterior.GetOrDefault(id ?? "")?.ExteriorDatas_Index;
		if (dictionary != null && dictionary.TryGetValue(BuildingName, out var value))
		{
			return value;
		}
		return null;
	}

	public bool SetWallpaper(BuildingWallpaperInfo wallpaperInfo, bool shouldRender = false)
	{
		if (wallpaperInfo == null)
		{
			return false;
		}
		if (GetWallpaperData(wallpaperInfo.Id) == null)
		{
			return false;
		}
		wallpaperId = wallpaperInfo.Id;
		RefreshWallpaper();
		return true;
	}

	public bool SetExterior(BuildingExteriorInfo exteriorInfo, bool shouldRender = false)
	{
		if (exteriorInfo == null)
		{
			return false;
		}
		if (GetExteriorData(exteriorInfo.Id) == null)
		{
			return false;
		}
		exteriorId = exteriorInfo.Id;
		RefreshRender();
		return true;
	}

	private bool TryGetRoomHandle(out RoomHandle handle)
	{
		handle = null;
		ISceneHandle sceneHandle = room?.SceneHandle;
		if (sceneHandle == null)
		{
			Debug.LogWarning("建筑\"" + BuildingName + "\"无法渲染墙纸，场景句柄为空");
			return false;
		}
		RoomHandle[] componentsInScene = sceneHandle.GetComponentsInScene<RoomHandle>();
		if (componentsInScene.IsNullOrEmpty())
		{
			Debug.LogWarning("建筑\"" + BuildingName + "\"无法渲染墙纸，场景中没有找到房间句柄");
			return false;
		}
		handle = componentsInScene.First();
		return true;
	}

	public void RefreshWallpaper()
	{
		if (!room.isRenderNow || wallpaperData == null || !TryGetRoomHandle(out var handle))
		{
			return;
		}
		SpriteRenderer component = handle.GetComponent<SpriteRenderer>();
		if (!(component == null))
		{
			component.sprite = wallpaperData.InternalSprite.Asset;
			DolocAPI.EntitySystem.Clear<BuildingWindow>();
			if (wallpaperData.ShowWindow)
			{
				DolocAPI.EntitySystem.Next<BuildingWindow>().Render(wallpaperData.WindowMask.Asset, handle.transform.position);
			}
			DolocAPI.envBackgroundEx.SetVisible(room.ShouldShowBackground);
			DolocAPI.envBackgroundEx.SetBackgroundMask(room.ShouldMaskBackground);
		}
	}

	public void ClearWindow()
	{
		DolocAPI.EntitySystem.Clear<BuildingWindow>();
	}
}
