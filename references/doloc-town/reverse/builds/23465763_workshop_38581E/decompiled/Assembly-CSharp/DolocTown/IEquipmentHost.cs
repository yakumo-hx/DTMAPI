using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public interface IEquipmentHost : IBaseHost
{
	Room RootRoom { get; }

	WeatherInfo CurrentWeatherInfo { get; }

	[JsonProperty]
	EquipmentManager DM_equipment { get; }

	ElectricSystem DM_electric { get; }

	AutomateSystem DM_automate { get; }

	IEnumerable<Equipment> AllEquipments => DM_equipment.AllEquipments;

	IEnumerable<Equipment> AllEquipmentsIncludeSubrooms
	{
		get
		{
			foreach (Equipment allEquipment in DM_equipment.AllEquipments)
			{
				yield return allEquipment;
			}
			if (CurrentRoom.IsInHouse && CurrentRoom.Type == RoomType.Farm)
			{
				yield break;
			}
			foreach (Building building in CurrentRoom.DM_building.Buildings)
			{
				foreach (Equipment allEquipment2 in building.room.DM_equipment.AllEquipments)
				{
					yield return allEquipment2;
				}
			}
		}
	}

	bool AdjustLightIntensityFlag { get; set; }

	void RenderAllEquipments()
	{
		CurrentRoom.SetNonEnvLight(DolocAPI.archiveHandle.ShouldLightUp);
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			RenderEquipment(allEquipment);
		}
	}

	void HideAllEquipments()
	{
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			DolocAPI.EntitySystem.Recycle(allEquipment.Renderer);
		}
	}

	bool IsEquipmentBuildable(IEnumerable<Vector2Int> cvPositions, IEnumerable<Vector2Int> gdPositions, bool forceOnGround = false)
	{
		return DM_terrain.IsEquipmentConstructable(cvPositions, gdPositions, forceOnGround);
	}

	bool IsDecalLayerEmpty(IEnumerable<Vector2Int> cvPositions)
	{
		return DM_terrain.AllEmpty(cvPositions, TerrainLayerName.Decal);
	}

	Equipment CreateEquipmentFromDirty(Vector3 wp, Vector2Int anchor, Equipment dirty, bool turn, IDecalHost decalHost = null, int decalSlotIndex = -1)
	{
		if (dirty.proto.isDecal && (decalHost == null || decalSlotIndex < 0))
		{
			Debug.LogError("建造贴纸设备需要传入decalHost和decalSlot参数");
			return null;
		}
		Equipment equipment = DM_equipment.CreateEquipmentFromDirty(this, wp, anchor, dirty, turn);
		if (equipment == null)
		{
			return null;
		}
		if (dirty.proto.isDecal)
		{
			IDecal decal = equipment;
			if (!decal.HostFilter(decalHost))
			{
				DM_equipment.RemoveEquipment(equipment);
				return null;
			}
			decal.AttachToDecalHost(decalHost, decalSlotIndex);
		}
		DM_terrain.FillContent(equipment);
		DM_electric.TryAddElectronicComponent(equipment);
		DM_automate.TryAddAutomateBotManager(equipment);
		equipment.decorator.SetWeatherType(CurrentWeatherInfo.Id);
		RenderEquipment(equipment);
		equipment.OnCreated();
		CheckLightChanged(equipment);
		return equipment;
	}

	Equipment CreateEquipment(Vector3 wp, Vector2Int anchor, EquipmentInfo proto, bool turn, IDecalHost decalHost = null, int decalSlotIndex = -1)
	{
		if (proto == null)
		{
			return null;
		}
		if (proto.isDecal && (decalHost == null || decalSlotIndex < 0))
		{
			Debug.LogError("建造贴纸设备需要传入decalHost和decalSlot参数");
			return null;
		}
		Equipment equipment = DM_equipment.CreateEquipment(this, wp, anchor, proto, turn);
		if (equipment == null)
		{
			return null;
		}
		if (proto.isDecal)
		{
			IDecal decal = equipment;
			if (!decal.HostFilter(decalHost))
			{
				DM_equipment.RemoveEquipment(equipment);
				return null;
			}
			decal.AttachToDecalHost(decalHost, decalSlotIndex);
		}
		DM_terrain.FillContent(equipment);
		DM_electric.TryAddElectronicComponent(equipment);
		DM_automate.TryAddAutomateBotManager(equipment);
		equipment.decorator.SetWeatherType(CurrentWeatherInfo.Id);
		RenderEquipment(equipment);
		equipment.OnCreated();
		CheckLightChanged(equipment);
		return equipment;
	}

	Equipment CreateEquipmentFromPreset(RoomPresetObjectProto preset, bool turn)
	{
		if (!DolocAPI.QueryEquipment(preset.name, out var proto))
		{
			return null;
		}
		Vector2 vector = (preset.pos + new Vector2((float)proto.CoverSize.x * 0.5f, 0f)) * 1.5f + CurrentRoom.RoomPosition;
		Equipment equipment = CreateEquipmentNoRender(vector, preset.pos, proto, turn);
		if (equipment is IContainer container)
		{
			container.OverwriteInventory(preset.inventory);
		}
		return equipment;
	}

	Equipment CreateEquipmentNoRender(Vector3 wp, Vector2Int anchor, EquipmentInfo proto, bool turn)
	{
		if (proto == null)
		{
			return null;
		}
		Equipment equipment = DM_equipment.CreateEquipment(this, wp, anchor, proto, turn);
		if (equipment != null)
		{
			DM_electric.TryAddElectronicComponent(equipment);
			DM_automate.TryAddAutomateBotManager(equipment);
			DM_terrain.FillContent(equipment);
			equipment.OnCreated();
			return equipment;
		}
		return null;
	}

	Equipment GetEquipment(int index)
	{
		if (index < 0)
		{
			return null;
		}
		return DM_equipment.AllEquipments.FirstOrDefault((Equipment equipment) => equipment.index == index);
	}

	Equipment GetEquipment(Vector2Int cellpos)
	{
		return DM_terrain.GetContent<Equipment>(cellpos);
	}

	T GetEquipment<T>(Vector2Int cellPos) where T : Equipment
	{
		if (DM_terrain.GetContent<Equipment>(cellPos) is T result)
		{
			return result;
		}
		return null;
	}

	Equipment GetDecalEquipment(Vector2Int cellpos)
	{
		return DM_terrain.GetContent<Equipment>(cellpos, TerrainLayerName.Decal);
	}

	T[] GetEquipments<T>() where T : Equipment
	{
		return DM_equipment.GetEquipments<T>();
	}

	T[] GetEquipmentsOfAnyType<T>(IEnumerable<Vector2Int> positions = null)
	{
		if (positions == null)
		{
			HashSet<T> hashSet = new HashSet<T>();
			foreach (Equipment allEquipment in DM_equipment.AllEquipments)
			{
				if (allEquipment is T item)
				{
					hashSet.Add(item);
				}
			}
			return hashSet.ToArray();
		}
		HashSet<T> hashSet2 = new HashSet<T>();
		foreach (Vector2Int position in positions)
		{
			if (DM_terrain.QueryContent<Equipment>(position, out var cnt) && cnt is T item2)
			{
				hashSet2.Add(item2);
			}
		}
		return hashSet2.ToArray();
	}

	int CountEquipment<T>() where T : Equipment
	{
		return DM_equipment.CountEquipment<T>();
	}

	int CountEquipment(string name)
	{
		return DM_equipment.CountEquipment(name);
	}

	T GetEquipment<T>() where T : Equipment
	{
		return DM_equipment.GetEquipment<T>();
	}

	T GetEquipment<T>(Func<T, bool> condition) where T : Equipment
	{
		return DM_equipment.GetEquipment(condition);
	}

	bool QueryEquipment(Vector2Int cellPosition, out Equipment equipment)
	{
		return DM_terrain.QueryContent<Equipment>(cellPosition, out equipment);
	}

	void RemoveEquipment(Equipment equipment, bool putInBackpack = false, bool retrieveItem = true, bool includeTerrain = true)
	{
		if (equipment == null || equipment.index < 0)
		{
			return;
		}
		if (DM_equipment.IsUpdating)
		{
			DM_equipment.AddRemoveBuffer(equipment);
			return;
		}
		DM_equipment.RemoveEquipment(equipment);
		DM_electric.TryRemoveElectronicComponent(equipment);
		DM_automate.TryRemoveAutomateBotManager(equipment);
		if (includeTerrain)
		{
			DM_terrain.RemoveContent(equipment);
		}
		if (retrieveItem)
		{
			equipment.RetrieveItemOnRemoval(putInBackpack);
		}
		equipment.DecoratedRemove();
		equipment.RemoveFromDecalHost();
		CheckLightChanged(equipment);
		DolocAPI.EntitySystem.Recycle(equipment.Renderer);
	}

	void RemoveAllEquipment()
	{
		if (DM_equipment.IsUpdating)
		{
			foreach (Equipment allEquipment in DM_equipment.AllEquipments)
			{
				if (!allEquipment.proto.isDecal)
				{
					DM_equipment.AddRemoveBuffer(allEquipment);
				}
			}
			return;
		}
		for (int num = DM_equipment.AllEquipments.Count() - 1; num >= 0; num--)
		{
			Equipment equipment = DM_equipment.AllEquipments.ElementAt(num);
			if (equipment != null && !equipment.proto.isDecal)
			{
				DM_electric.TryRemoveElectronicComponent(equipment);
				DM_automate.TryRemoveAutomateBotManager(equipment);
				DM_terrain.RemoveContent(equipment);
				equipment.RetrieveItemOnRemoval(putInBackpack: false);
				equipment.DecoratedRemove();
				DolocAPI.EntitySystem.Recycle(equipment.Renderer);
			}
		}
		DM_equipment.Clear();
	}

	void RenderEquipment(Equipment equipment)
	{
		EquipmentRenderer equipmentRenderer2 = (equipment.Renderer = DolocAPI.EntitySystem.Next<EquipmentRenderer>());
		equipmentRenderer2.Equipment = equipment;
		equipmentRenderer2.position = equipment.Position;
		equipmentRenderer2.Sprite = equipment.EquipmentSprite;
		equipmentRenderer2.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		equipmentRenderer2.ShowOutline = false;
		equipmentRenderer2.Alpha = 1f;
		equipment.DecoratedRender();
	}

	bool MoveEquipment(Equipment equipment, Vector2Int anchor, Vector3 wp)
	{
		if (equipment == null)
		{
			return false;
		}
		DM_terrain.RemoveContent(equipment);
		equipment.MoveTerrainContent(anchor, wp);
		DM_terrain.FillContent(equipment);
		if (equipment.IsRender && equipment.Renderer != null)
		{
			equipment.Renderer.SetVisible(value: true);
			equipment.Renderer.position = equipment.Position;
			equipment.DecoratedMove();
			return true;
		}
		equipment.OnMove();
		return true;
	}

	void DEBUG_ManualInvokeAffectors<T>() where T : Equipment
	{
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			if (allEquipment is T && allEquipment is IAffector affector)
			{
				affector.InvokeAffect();
			}
		}
	}

	void RenderOccupied(GridRenderer gridRenderer, Equipment equipment)
	{
		equipment.OccupiedGridRenderer = gridRenderer;
		gridRenderer.SetGridRendererState(equipment.GetGlobalAnchor(), equipment.CoveredSize, DolocAPI.eftConfig.terrainInvalidColor);
		AffectorArea equipmentAffectorArea = BuilderUtils.GetEquipmentAffectorArea(equipment.proto);
		if (equipmentAffectorArea != null)
		{
			GridAreaBackground gridAreaBackground = DolocAPI.EntitySystem.Next<GridAreaBackground>();
			gridAreaBackground.GridSize = gridRenderer.GridSize + new Vector2Int(2 * equipmentAffectorArea.horizontalRange, equipmentAffectorArea.verticalRangeTop + equipmentAffectorArea.verticalRangeBottom);
			gridAreaBackground.GridPosition = gridRenderer.GridPosition - new Vector2Int(equipmentAffectorArea.horizontalRange, equipmentAffectorArea.verticalRangeBottom);
		}
	}

	void RenderAllEquipmentsOccupied()
	{
		DolocAPI.EntitySystem.SetupAll<GridRenderer, Equipment>(DM_equipment.AllEquipments.Where((Equipment x) => !x.proto.isDecal), RenderOccupied);
	}

	void UpdateAffectedEquipment(Vector2Int[] affectedPositions)
	{
		HashSet<Equipment> hashSet = new HashSet<Equipment>();
		foreach (Vector2Int cellpos in affectedPositions)
		{
			Equipment equipment = GetEquipment(cellpos);
			if (!hashSet.Contains(equipment) && equipment is IAffectorReceiver)
			{
				hashSet.Add(equipment);
			}
		}
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			if (!allEquipment.proto.isDecal)
			{
				Color color = (hashSet.Contains(allEquipment) ? DolocAPI.eftConfig.terrainValidColor : DolocAPI.eftConfig.terrainInvalidColor);
				allEquipment.OccupiedGridRenderer.SetGridRendererColor(color);
			}
		}
	}

	void AddOccupiedGrid(Equipment equipment)
	{
		if (equipment != null)
		{
			GridRenderer gridRenderer = DolocAPI.EntitySystem.Next<GridRenderer>();
			if (!(gridRenderer == null))
			{
				RenderOccupied(gridRenderer, equipment);
			}
		}
	}

	void RecycleOccupiedGridRenderer(Equipment equipment)
	{
		if (equipment != null)
		{
			DolocAPI.EntitySystem.Recycle(equipment.OccupiedGridRenderer);
			equipment.OccupiedGridRenderer = null;
		}
	}

	void RecycleAllGridRenderer()
	{
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			allEquipment.OccupiedGridRenderer = null;
		}
		DolocAPI.EntitySystem.Clear<GridAreaBackground>();
		DolocAPI.EntitySystem.Clear<GridRenderer>();
	}

	void CheckLightChanged(Equipment equipment)
	{
		if (equipment is ILamp)
		{
			AdjustLightIntensityFlag = true;
			if (CurrentRoom.isRenderNow && CurrentRoom.Type == RoomType.Farm && CurrentRoom.IsInHouse)
			{
				DolocAPI.RefreshSunLight(0f);
			}
		}
	}

	float GetLightIntensityFactor(Equipment equipment)
	{
		if (!(equipment is ILamp))
		{
			return 1f;
		}
		Vector2Int lightIntensityDecayLampArea = DolocAPI.GlobalParameter.LightIntensityDecayLampArea;
		int value = DM_terrain.GetContentsFromArea<Equipment>(equipment.Anchor - lightIntensityDecayLampArea, equipment.CoveredSize + lightIntensityDecayLampArea * 2).Count((Equipment x) => x is ILamp lamp && lamp.ShouldLight) - 1;
		float[] array = (CurrentRoom.IsInHouse ? DolocAPI.GlobalParameter.LightIntensityDecayFactorPerLampInside : DolocAPI.GlobalParameter.LightIntensityDecayFactorPerLampOutside);
		return array[Mathf.Clamp(value, 0, array.Length - 1)];
	}

	bool HasActiveLamp()
	{
		if (CurrentRoom.Type != RoomType.Farm || !CurrentRoom.IsInHouse)
		{
			return false;
		}
		return DM_equipment.AllEquipments.Count((Equipment x) => x is ILamp lamp && lamp.ShouldLight) > 0;
	}

	float GetLightIntensityAdder()
	{
		if (CurrentRoom.Type != RoomType.Farm || !CurrentRoom.IsInHouse)
		{
			return 0f;
		}
		int value = DM_equipment.AllEquipments.Count((Equipment x) => x is ILamp lamp && lamp.ShouldLight);
		float[] envLightIntensityAdderPerLamp = DolocAPI.GlobalParameter.EnvLightIntensityAdderPerLamp;
		return envLightIntensityAdderPerLamp[Mathf.Clamp(value, 0, envLightIntensityAdderPerLamp.Length - 1)];
	}

	void AfterNewGame()
	{
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			allEquipment.AfterNewGame();
		}
	}

	bool GetRandomEmptyBuildPosition(Vector2Int coverSize, out Vector2Int position)
	{
		position = default(Vector2Int);
		Vector2Int[] array = DM_terrain.FilterFreePositions(CurrentRoom.GroundPositions, TerrainLayerName.Equipment).FindFreeBlocks(coverSize.x);
		if (array.Length == 0)
		{
			return false;
		}
		Vector2Int[] array2 = array.Shuffle();
		foreach (Vector2Int vector2Int in array2)
		{
			if (DM_terrain.IsEquipmentConstructable(coverSize.IterateGrid(vector2Int)))
			{
				position = vector2Int;
				return true;
			}
		}
		return false;
	}

	void __AfterLoadEquipments()
	{
		__ClearInvalidEquipments();
		__HandleCrowedOutEquipments();
		__RebuildElectricAndAutomate();
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			allEquipment.AfterLoadEquipment();
		}
	}

	void __ClearInvalidEquipments()
	{
		Queue<Equipment> queue = new Queue<Equipment>();
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			if (!allEquipment.IsValid)
			{
				queue.Enqueue(allEquipment);
			}
		}
		if (queue.Count > 0)
		{
			Debug.LogWarning($"房间\"{CurrentRoom.Title}\"有{queue.Count}个设备因无效而被移除");
			while (queue.Count > 0)
			{
				DM_equipment.RemoveEquipment(queue.Dequeue());
			}
		}
	}

	void __HandleCrowedOutEquipments()
	{
		Queue<Equipment> queue = new Queue<Equipment>();
		HashSet<Equipment> hashSet = new HashSet<Equipment>();
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			if (!DM_equipment.ValidateEquipmentType(allEquipment))
			{
				queue.Enqueue(allEquipment);
				hashSet.Add(allEquipment);
				continue;
			}
			Vector2Int[] coveredPositions = allEquipment.CoveredPositions;
			if (allEquipment.proto.isDecal)
			{
				allEquipment.SetHost(this);
				continue;
			}
			IEnumerable<Vector2Int> gdPositions = allEquipment.proto.GroundPositions(allEquipment.Anchor);
			if (!DM_terrain.IsEquipmentConstructable(coveredPositions, gdPositions) && !allEquipment.IsOccupy)
			{
				queue.Enqueue(allEquipment);
				continue;
			}
			allEquipment.SetHost(this);
			DM_terrain.FillContent(allEquipment);
		}
		while (queue.Count > 0)
		{
			Equipment equipment = queue.Dequeue();
			equipment.SetHost(CurrentRoom);
			if (hashSet.Contains(equipment))
			{
				equipment.OnFunctionChange();
			}
			RemoveEquipment(equipment, putInBackpack: false, retrieveItem: true, includeTerrain: false);
		}
	}

	void __TEMP_Handle(Equipment equipment)
	{
		if (equipment is ChickenNest chickenNest)
		{
			Debug.Log($"<color=red>检测到有效的鸡窝设备,位于{chickenNest.Anchor}</color>");
			if (DM_terrain.GetContent<ChickenNest>(chickenNest.Anchor) == null)
			{
				Debug.LogError("<color=red>鸡窝设备" + chickenNest.Name + "的内容为空,请检查设备是否被正确加载</color>");
			}
			else
			{
				Debug.Log("<color=green>鸡窝设备" + chickenNest.Name + "的内容不为空</color>");
			}
		}
	}

	void __RebuildElectricAndAutomate()
	{
		if (RootRoom == CurrentRoom)
		{
			IEnumerable<Equipment> allEquipmentsIncludeSubrooms = AllEquipmentsIncludeSubrooms;
			IEnumerable<Equipment> equipments = (allEquipmentsIncludeSubrooms as Equipment[]) ?? allEquipmentsIncludeSubrooms.ToArray();
			DM_electric.Rebuild(equipments);
			DM_automate.Rebuild(equipments);
		}
	}
}
