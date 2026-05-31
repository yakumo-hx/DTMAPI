using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public interface IDungeonResourceHost : IBaseHost
{
	[JsonProperty]
	DungeonResourceManager DM_dungeonResource { get; }

	ResourceGenInfoProto ResourceGenInfo { get; }

	void GenNewDungeonResource()
	{
		ResourceSpawnEntry resourceSpawnEntry = CurrentRoom.RoomSpawnInfo.ResourceSpawnEntry;
		if (ResourceGenInfo.shouldGen && !resourceSpawnEntry.IsEmpty && DM_dungeonResource.CounterGenerator.Tick())
		{
			ResourceSpawnInfo spawnLut_Ref = resourceSpawnEntry.SpawnLut_Ref;
			Dictionary<string, int> resourceCountByName = ((IDungeonResourceHost)CurrentRoom).DM_dungeonResource.ResourceCountByName;
			if (spawnLut_Ref.TrySpawnSingle(resourceCountByName, out var spawnData, DolocAPI.archiveHandle.DateNow) && DolocAPI.QueryResourceProto(spawnData.ResourceId, out var proto))
			{
				CreateDungeonResourceNoRender(proto);
			}
		}
	}

	void RenderAllResources()
	{
		RenderResource(DM_dungeonResource.AllDungeonResources);
	}

	void HideAllDungeonResources()
	{
		foreach (DungeonResource allDungeonResource in DM_dungeonResource.AllDungeonResources)
		{
			DolocAPI.EntitySystem.Recycle(allDungeonResource.Renderer);
		}
	}

	bool GenerateDungeonResource_DungeonMode(bool refreshPresets)
	{
		if (refreshPresets)
		{
			RoomPresetObjectProto[] presetObjects = CurrentRoom.baseProto.presetObjects;
			foreach (RoomPresetObjectProto roomPresetObjectProto in presetObjects)
			{
				if (roomPresetObjectProto.presetType == RoomPresetObjectType.Resource && !roomPresetObjectProto.disableRefresh)
				{
					CreateResourceFromPreset(roomPresetObjectProto);
				}
			}
		}
		ResourceSpawnEntry resourceSpawnEntry = CurrentRoom.RoomSpawnInfo.ResourceSpawnEntry;
		if (!ResourceGenInfo.shouldGen || resourceSpawnEntry.IsEmpty)
		{
			return true;
		}
		_GenDungeonResource(resourceSpawnEntry.SpawnLut_Ref, resourceSpawnEntry.CountRange.RandomCount);
		return true;
	}

	void _GenDungeonResource(ResourceSpawnInfo lut, int totalCount)
	{
		List<(ResourceSpawnData, int)> list = lut.SpawnByFloor(DolocAPI.archiveHandle.DateNow);
		List<string> list2 = new List<string>();
		foreach (var item3 in list)
		{
			ResourceSpawnData item = item3.Item1;
			int item2 = item3.Item2;
			for (int i = 0; i < item2; i++)
			{
				list2.Add(item.SpawnId);
			}
		}
		list2.Shuffle();
		int num = 0;
		foreach (string item4 in list2)
		{
			if (!DolocAPI.QueryResourceProto(item4, out var proto))
			{
				continue;
			}
			DungeonResource dungeonResource = CreateDungeonResourceNoRender(proto);
			if (dungeonResource != null)
			{
				num++;
				if (num > totalCount)
				{
					break;
				}
				dungeonResource.InitRandomGrowth(CurrentRoom.Type == RoomType.Dungeon);
			}
		}
		for (int j = num; j < totalCount; j++)
		{
			if (lut.TrySpawnSingle(CurrentRoom.DM_dungeonResource.ResourceCountByName, out var spawnData, DolocAPI.archiveHandle.DateNow) && DolocAPI.QueryResourceProto(spawnData.ResourceId, out var proto2))
			{
				CreateDungeonResourceNoRender(proto2)?.InitRandomGrowth(CurrentRoom.Type == RoomType.Dungeon);
			}
		}
	}

	void SetAllResourceToMaxLevel(bool useTween)
	{
		foreach (DungeonResource allDungeonResource in DM_dungeonResource.AllDungeonResources)
		{
			allDungeonResource.SetMaxGrowthLevel(useTween);
		}
	}

	void RenderResource(IEnumerable<DungeonResource> resources)
	{
		if (resources != null)
		{
			DolocAPI.EntitySystem.SetupAll<DungeonResourceRenderer, DungeonResource>(resources, Render);
		}
	}

	void Render(DungeonResourceRenderer renderer, DungeonResource resource)
	{
		renderer.DungeonResource = resource;
		resource.Renderer = renderer;
		renderer.position = resource.Position;
		Sprite currentSprite = resource.CurrentSprite;
		renderer.Sr.sprite = currentSprite;
		renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		renderer.PolygonCollider.SetPath(0, DungeonResourceRenderer.shapeBuffer.GetSpriteShape(currentSprite));
		resource.Render();
	}

	DungeonResource CreateDungeonResource(ResourceInfo proto)
	{
		if (proto == null)
		{
			return null;
		}
		DungeonResource dungeonResource = CreateDungeonResourceNoRender(proto);
		if (dungeonResource == null)
		{
			return null;
		}
		Render(DolocAPI.EntitySystem.Next<DungeonResourceRenderer>(), dungeonResource);
		return dungeonResource;
	}

	DungeonResource CreateDungeonResourceNoRender(ResourceInfo proto)
	{
		IEnumerable<Vector2Int> positions = CurrentRoom.Geometry.groundPositions.Intersect(ResourceGenInfo.constraint.GetPositions(proto.ResourceType));
		TerrainLayerName layerMask = TerrainLayerName.Building | TerrainLayerName.Equipment | proto.TerrainLayer;
		Vector2Int[] array = DM_terrain.FilterFreePositions(positions, layerMask).FindFreeBlocks(proto.Width);
		if (array.Length == 0)
		{
			return null;
		}
		return CreateDungeonResourceNoRender(array.Choice(), proto);
	}

	DungeonResource CreateDungeonResourceNoRender(Vector2Int anchor, ResourceInfo proto)
	{
		DungeonResource dungeonResource = DM_dungeonResource.CreateResource(this, proto, anchor);
		DM_terrain.FillContent(dungeonResource);
		return dungeonResource;
	}

	bool RemoveDungeonResourceNoRender(DungeonResource resource)
	{
		if (DM_dungeonResource.RemoveResource(resource))
		{
			resource.OnRemove();
			DM_terrain.RemoveContent(resource);
			return true;
		}
		return false;
	}

	bool RemoveDungeonResource(DungeonResource resource)
	{
		if (RemoveDungeonResourceNoRender(resource))
		{
			DolocAPI.EntitySystem.Recycle(resource.Renderer);
			return true;
		}
		return false;
	}

	bool _PlantTree(ResourceInfo proto, Vector2Int point)
	{
		DungeonResource dungeonResource = DM_dungeonResource.CreateResource(this, proto, point);
		if (dungeonResource != null)
		{
			DM_terrain.FillContent(dungeonResource);
			Render(DolocAPI.EntitySystem.Next<DungeonResourceRenderer>(), dungeonResource);
			return true;
		}
		return false;
	}

	bool CanPlantTree(Vector2Int point, int width)
	{
		if (CanPlantTreeConstraintCheck(point, width) && CanPlantTreeSpaceCheck(point, width))
		{
			return !IsPlantObstacle(point, width);
		}
		return false;
	}

	bool CanPlantTreeConstraintCheck(Vector2Int point, int width)
	{
		if (CurrentRoom.RoomConstructInfo == null)
		{
			return true;
		}
		if (!CurrentRoom.RoomConstructInfo.AllowBuildResource)
		{
			return false;
		}
		if (!CurrentRoom.RoomConstructInfo.ConstrainResource)
		{
			return true;
		}
		return Grid2D.CoverHArray(point, width).All(((IEnumerable<Vector2Int>)CurrentRoom.baseProto.resourceInfo.constraint[DungeonResourceConstraintType.Tree]).Contains<Vector2Int>);
	}

	bool CanPlantTreeSpaceCheck(Vector2Int point, int width)
	{
		Vector2Int[] array = Grid2D.CoverHArray(point, width);
		if (!array.All(CurrentRoom.Geometry.IsOnGround))
		{
			return false;
		}
		TerrainLayerName layerMask = TerrainLayerName.Building | TerrainLayerName.Equipment | TerrainLayerName.ResourceTree;
		return DM_terrain.AllEmpty(array, layerMask);
	}

	bool IsPlantObstacle(Vector2Int point, int width)
	{
		Vector2Int[] source = Grid2D.CoverHArray(point, width);
		int index = EnvObstacleType.PlantObstacle.GetHashCode();
		return source.Any((Vector2Int p) => CurrentRoom.baseProto.envObstacles.Contains(p, index));
	}

	bool CanPlaceResource(Vector2Int pos, ResourceInfo proto)
	{
		Vector2Int[] array = Grid2D.CoverHArray(pos, proto.Width);
		if (!array.All(CurrentRoom.Geometry.IsOnGround))
		{
			return false;
		}
		TerrainLayerName layerMask = TerrainLayerName.Building | TerrainLayerName.Equipment | proto.TerrainLayer;
		return DM_terrain.AllEmpty(array, layerMask);
	}

	void ClearResourceInPositions(Vector2Int[] positions)
	{
		foreach (DungeonResource contentsFromPosition in DM_terrain.GetContentsFromPositions<DungeonResource>(positions))
		{
			if (contentsFromPosition is IDecalHost decalHost)
			{
				decalHost.TakeOffAllDecals();
			}
			RemoveDungeonResource(contentsFromPosition);
		}
	}

	DungeonResource[] GetResourcesAt(Vector2Int anchor, int width)
	{
		HashSet<DungeonResource> hashSet = new HashSet<DungeonResource>();
		for (int i = 0; i < Mathf.Max(1, width); i++)
		{
			DungeonResource content = DM_terrain.GetContent<DungeonResource>(new Vector2Int(anchor.x + i, anchor.y));
			if (content != null)
			{
				hashSet.Add(content);
			}
		}
		return hashSet.ToArray();
	}

	DungeonResource TryGetRandomResource(DungeonResourceClass classType)
	{
		return DM_dungeonResource.TryGetRandomResource(classType);
	}

	DungeonResource TryGetRandomResource(string id)
	{
		return DM_dungeonResource.TryGetRandomResource(id);
	}

	DungeonResource TryGetRandomResource(IEnumerable<string> ids)
	{
		return DM_dungeonResource.TryGetRandomResource(ids);
	}

	DungeonResource TryGetRandomResourceFromLut(string lutName)
	{
		ResourceSpawnInfo orDefault = DolocConfig.Tables.TbResourceSpawn.GetOrDefault(lutName);
		if (orDefault == null)
		{
			return null;
		}
		IEnumerable<string> source = orDefault.SpawnDatas.Select((ResourceSpawnData x) => x.ResourceId);
		float[] weights = orDefault.SpawnDatas.Select((ResourceSpawnData x) => x.SpawnWeight).ToArray();
		return DM_dungeonResource.TryGetRandomResource(source.ToList(), weights);
	}

	void ClearAllResources(bool useEffect)
	{
		foreach (DungeonResource item in DM_dungeonResource.AllDungeonResources.ToList())
		{
			item.RemoveResource(useEffect);
		}
	}

	DungeonResource CreateResourceFromPreset(RoomPresetObjectProto preset)
	{
		if (!DolocAPI.QueryResourceProto(preset.name, out var proto))
		{
			return null;
		}
		DungeonResource dungeonResource = CreateDungeonResourceNoRender(preset.pos, proto);
		if (dungeonResource != null)
		{
			dungeonResource.SetGrowthLevel(preset.initGrowthLevel, useTween: false);
			return dungeonResource;
		}
		return dungeonResource;
	}

	void RemoveResourceOnMonthChange(bool isRender)
	{
		DungeonResource[] array = DM_dungeonResource.AllDungeonResources.Where((DungeonResource x) => !x.Proto.CheckMatchMonth(DolocAPI.archiveHandle.DateNow.Month)).ToArray();
		foreach (DungeonResource resource in array)
		{
			if (isRender)
			{
				RemoveDungeonResource(resource);
			}
			else
			{
				RemoveDungeonResourceNoRender(resource);
			}
		}
	}

	void AfterNewGame()
	{
		DM_dungeonResource.RefreshCounterInterval();
		GenerateDungeonResource_DungeonMode(refreshPresets: false);
	}

	void __AfterLoadDungeonResources()
	{
		DM_dungeonResource.RefreshCounterInterval();
		DolocInitAssert.IsTrue(DM_dungeonResource != null);
		int num = DM_dungeonResource._ClearInvalidResource();
		if (num > 0)
		{
			Debug.LogError($"房间\"{CurrentRoom.RoomId}\"移除{num}个无效的资源");
		}
		foreach (DungeonResource allDungeonResource in DM_dungeonResource.AllDungeonResources)
		{
			allDungeonResource.Host = this;
			DM_terrain.FillContent(allDungeonResource);
		}
		Queue<DungeonResource> queue = new Queue<DungeonResource>();
		foreach (DungeonResource allDungeonResource2 in DM_dungeonResource.AllDungeonResources)
		{
			if (!DM_terrain.IsOnGround(allDungeonResource2.CoveredPositions))
			{
				queue.Enqueue(allDungeonResource2);
			}
		}
		while (queue.Count > 0)
		{
			DungeonResource resource = queue.Dequeue();
			RemoveDungeonResourceNoRender(resource);
		}
	}
}
