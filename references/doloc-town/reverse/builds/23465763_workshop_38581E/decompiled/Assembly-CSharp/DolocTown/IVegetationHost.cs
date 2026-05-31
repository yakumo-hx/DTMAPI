using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public interface IVegetationHost : IBaseHost
{
	[JsonProperty]
	VegetationManager DM_vegetation { get; }

	ResourceGenInfoProto VegetationGenInfo { get; }

	void RenderAllVegetation()
	{
		DolocAPI.EntitySystem.SetupAll<VegetationRenderer, Vegetation>(DM_vegetation.AllDatas, RenderVegetation);
	}

	void HideAllVegetation()
	{
		foreach (Vegetation allData in DM_vegetation.AllDatas)
		{
			DolocAPI.EntitySystem.Recycle(allData.Renderer);
		}
	}

	void Extend(Vector2Int offset)
	{
		foreach (Vegetation allData in DM_vegetation.AllDatas)
		{
			allData.ShiftTerrainContent(offset, Vector3.zero);
			allData.Host = this;
			DM_terrain.FillContent(allData);
		}
	}

	void RenderVegetation(VegetationRenderer renderer, Vegetation vegetation)
	{
		vegetation.Renderer = renderer;
		renderer.Vegetation = vegetation;
		renderer.position = new Vector3(vegetation.Position.x, vegetation.Position.y, 0.125f);
		renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		renderer.Sr.sprite = vegetation.CurrentSprite;
		renderer.Collider2d.SetPath(0, DungeonResourceRenderer.shapeBuffer.GetSpriteShape(vegetation.CurrentSprite));
		vegetation.OnRender();
	}

	void GenNewVegetation(bool randomGrowthLevel = false)
	{
		VegetationSpawnEntry vegetationSpawnEntry = CurrentRoom.RoomSpawnInfo.VegetationSpawnEntry;
		if (VegetationGenInfo.shouldGen && !vegetationSpawnEntry.IsEmpty && DM_vegetation.CounterGenerator.Tick())
		{
			VegetationSpawnInfo spawnLut_Ref = vegetationSpawnEntry.SpawnLut_Ref;
			Dictionary<string, int> resourceCount = DM_vegetation.ResourceCount;
			if (spawnLut_Ref.TrySpawnSingle(resourceCount, out var spawnData) && DolocAPI.QueryVegetationProto(spawnData.VegetationId, out var proto))
			{
				CreateVegetationNoRender(proto, randomGrowthLevel);
			}
		}
	}

	void GenerateVegetation(bool randomGrowthLevel)
	{
		VegetationSpawnEntry vegetationSpawnEntry = CurrentRoom.RoomSpawnInfo.VegetationSpawnEntry;
		if (!VegetationGenInfo.shouldGen || vegetationSpawnEntry.IsEmpty)
		{
			return;
		}
		List<(VegetationSpawnData, int)> list = vegetationSpawnEntry.SpawnLut_Ref.SpawnByFloor();
		List<string> list2 = new List<string>();
		foreach (var item3 in list)
		{
			VegetationSpawnData item = item3.Item1;
			int item2 = item3.Item2;
			for (int i = 0; i < item2; i++)
			{
				list2.Add(item.SpawnId);
			}
		}
		list2.Shuffle();
		int num = 0;
		int randomCount = vegetationSpawnEntry.CountRange.RandomCount;
		foreach (string item4 in list2)
		{
			if (DolocAPI.QueryVegetationProto(item4, out var proto) && CreateVegetationNoRender(proto, randomGrowthLevel) != null)
			{
				num++;
				if (num > randomCount)
				{
					break;
				}
			}
		}
		for (int j = num; j < randomCount; j++)
		{
			if (vegetationSpawnEntry.SpawnLut_Ref.TrySpawnSingle(CurrentRoom.DM_dungeonResource.ResourceCountByName, out var spawnData) && DolocAPI.QueryVegetationProto(spawnData.VegetationId, out var proto2))
			{
				CreateVegetationNoRender(proto2, randomGrowthLevel);
			}
		}
	}

	Vegetation CreateVegetationNoRender(VegetationInfo proto, bool randomGrowthLevel)
	{
		IEnumerable<Vector2Int> first = DM_terrain.FilterFreePositions(CurrentRoom.Geometry.groundPositions, TerrainLayerName.Vegetation);
		Vector2Int[] positions = VegetationGenInfo.constraint.GetPositions(proto.Type);
		Vector2Int[] array = first.Intersect(positions).FindFreeBlocks(proto.Width);
		if (array.Length == 0)
		{
			return null;
		}
		Vector2Int anchor = array.Choice();
		Vector3 position = (new Vector2((float)anchor.x + (float)proto.Width * 0.5f, anchor.y) + CurrentRoom.RoomGridPos) * 1.5f;
		Vegetation vegetation = DM_vegetation.CreateVegetation(proto, anchor, position);
		vegetation.Host = this;
		DM_terrain.FillContent(vegetation);
		if (randomGrowthLevel)
		{
			vegetation.RandomInitState();
		}
		return vegetation;
	}

	bool RemoveVegetation(Vegetation vegetation)
	{
		if (vegetation == null)
		{
			return false;
		}
		if (!DM_vegetation.RemoveData(vegetation))
		{
			return false;
		}
		DM_terrain.RemoveContent(vegetation);
		if (vegetation.Renderer != null)
		{
			DolocAPI.EntitySystem.Recycle(vegetation.Renderer);
		}
		return true;
	}

	void ClearAllVegetation()
	{
		foreach (Vegetation allData in DM_vegetation.AllDatas)
		{
			DM_terrain.RemoveContent(allData);
			DolocAPI.EntitySystem.Recycle(allData.Renderer);
		}
		DM_vegetation.Clear();
	}

	void ReGenRoomVegetation(bool isRender)
	{
		ClearAllVegetation();
		GenerateVegetation(randomGrowthLevel: true);
		DM_vegetation.CounterGenerator.Reset();
		if (isRender)
		{
			RenderAllVegetation();
		}
	}

	void __AfterLoadVegetation()
	{
		Vegetation[] array = DM_vegetation.AllDatas.Where((Vegetation x) => !(x?.isValid ?? false)).ToArray();
		foreach (Vegetation vegetation in array)
		{
			RemoveVegetation(vegetation);
		}
		DM_vegetation.RefreshCounterInterval();
		foreach (Vegetation allData in DM_vegetation.AllDatas)
		{
			allData.Host = this;
			DM_terrain.FillContent(allData);
		}
	}

	void AfterNewGame()
	{
		DM_vegetation.RefreshCounterInterval();
		GenerateVegetation(randomGrowthLevel: true);
	}
}
