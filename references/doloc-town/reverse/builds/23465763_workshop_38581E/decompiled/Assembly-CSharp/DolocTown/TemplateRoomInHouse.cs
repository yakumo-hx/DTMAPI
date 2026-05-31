using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class TemplateRoomInHouse : TemplateRoom
{
	[JsonProperty]
	private bool isBroken;

	public override bool AffectedByMalignantWeather => isBroken;

	public Building Building { get; set; }

	public bool IsUniqueBuilding => Building.IsUnique;

	public override RoomEffectInfo RoomEffectInfo => Building.currentLevelData.RoomEffect_Ref;

	public IEnumerable<Building> Buildings
	{
		get
		{
			if (!Building.IsUnique)
			{
				yield return Building;
				yield break;
			}
			foreach (Building building in RootRoom.DM_building.Buildings)
			{
				if (building.room == this)
				{
					yield return building;
				}
			}
		}
	}

	public float Health => Building.Health;

	public float MaxHealth => Building.proto.Health;

	public override bool ShouldShowBackground => (Building?.wallpaperData?.ShowWindow).GetValueOrDefault();

	public override bool ShouldMaskBackground
	{
		get
		{
			if (base.baseProto.isInHouse)
			{
				return ShouldShowBackground;
			}
			return false;
		}
	}

	public override WeatherInfo CurrentWeatherInfo
	{
		get
		{
			if (!isBroken)
			{
				return DolocConfig.Tables.TbWeather.DefaultWeatherInfo;
			}
			return RootRoom.CurrentWeatherInfo;
		}
	}

	public TemplateRoomInHouse(string guid, Room parent, RoomProto proto)
		: base(guid, proto)
	{
		isBroken = false;
		SetParent(parent);
	}

	[JsonConstructor]
	public TemplateRoomInHouse(string guid, string ProtoName, PlatformManager DM_platform, EquipmentManager DM_equipment, bool isBroken, DropItemManager DM_dropitem, AnimalManager DM_animal = null)
		: base(guid, ProtoName, DM_platform, DM_equipment, DM_dropitem, DM_animal)
	{
		this.isBroken = isBroken;
	}

	public override void Render(bool includeMonster = true)
	{
		base.Render(includeMonster);
		RenderAllLinkGates();
		Building.RefreshWallpaper();
	}

	public override void ClearRender()
	{
		base.ClearRender();
		Building.ClearWindow();
	}

	private void RenderAllLinkGates()
	{
		HideAllLinkGatesInScene();
		if (DolocAPI.gameManager.gameInitConfig.ignoreBuildingLinkGateUnlock || DolocAPI.archiveHandle.IsBuildingLinkGateUnlocked())
		{
			RenderAllLinkGatesNormal();
			if (Building.IsCellar())
			{
				this.RenderAllLinkGatesForCellar();
			}
			else
			{
				this.RenderAllLinkGatesToCellar();
			}
		}
	}

	private void RenderAllLinkGatesNormal()
	{
		Dictionary<BuildingLinkGateId, BuildingLinkGate> dictionary = LoadLinkGatesInScene();
		foreach (BuildingLinkInfo allLinkInfo in Building.GenLinkMap().AllLinkInfos)
		{
			if (allLinkInfo.IsValid)
			{
				BuildingLinkGateId key = new BuildingLinkGateId(allLinkInfo.linkType, allLinkInfo.index);
				if (dictionary.TryGetValue(key, out var value))
				{
					value.Setup(allLinkInfo);
				}
			}
		}
	}

	private void HideAllLinkGatesInScene()
	{
		BuildingLinkGate[] componentsInScene = base.SceneHandle.GetComponentsInScene<BuildingLinkGate>();
		for (int i = 0; i < componentsInScene.Length; i++)
		{
			componentsInScene[i].SetVisible(value: false);
		}
	}

	public Dictionary<BuildingLinkGateId, BuildingLinkGate> LoadLinkGatesInScene()
	{
		if (Building.IsUnique)
		{
			return LoadLinkGatesInSceneUnique();
		}
		return LoadLinkGatesInSceneNormal();
	}

	private Dictionary<BuildingLinkGateId, BuildingLinkGate> LoadLinkGatesInSceneUnique()
	{
		Dictionary<BuildingLinkGateId, BuildingLinkGate> dictionary = new Dictionary<BuildingLinkGateId, BuildingLinkGate>();
		RoomHandle[] componentsInScene = base.SceneHandle.GetComponentsInScene<RoomHandle>();
		if (componentsInScene.IsNullOrEmpty())
		{
			return dictionary;
		}
		string handleName = Building.room.baseProto.name;
		RoomHandle roomHandle = componentsInScene.FirstOrDefault((RoomHandle rh) => rh.gameObject.name == handleName);
		if (roomHandle == null)
		{
			Debug.LogError("没有找到目标房间ID\"" + Building.templateRoomName + "\"");
			return dictionary;
		}
		BuildingLinkGate[] componentsInChildren = roomHandle.gameObject.GetComponentsInChildren<BuildingLinkGate>(includeInactive: true);
		foreach (BuildingLinkGate buildingLinkGate in componentsInChildren)
		{
			dictionary.TryAdd(buildingLinkGate.GateId, buildingLinkGate);
		}
		return dictionary;
	}

	private Dictionary<BuildingLinkGateId, BuildingLinkGate> LoadLinkGatesInSceneNormal()
	{
		BuildingLinkGate[] componentsInScene = base.SceneHandle.GetComponentsInScene<BuildingLinkGate>(includeInactive: true);
		Dictionary<BuildingLinkGateId, BuildingLinkGate> dictionary = new Dictionary<BuildingLinkGateId, BuildingLinkGate>();
		BuildingLinkGate[] array = componentsInScene;
		foreach (BuildingLinkGate buildingLinkGate in array)
		{
			dictionary.TryAdd(buildingLinkGate.GateId, buildingLinkGate);
		}
		return dictionary;
	}

	public void SetBroken(bool value)
	{
		if (isBroken == value)
		{
			return;
		}
		isBroken = value;
		foreach (Equipment allEquipment in base.DM_equipment.AllEquipments)
		{
			allEquipment.decorator.SetWeatherType(CurrentWeatherInfo.Id);
		}
	}
}
