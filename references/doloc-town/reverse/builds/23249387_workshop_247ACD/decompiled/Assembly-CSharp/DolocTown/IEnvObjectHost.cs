using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public interface IEnvObjectHost : IBaseHost
{
	EnvObjectManager DM_envObject { get; }

	ResourceGenInfoProto EnvObjectGenInfo { get; }

	bool blockCreate { get; set; }

	void RenderAllEnvObject()
	{
		RefreshEnvObjects();
		DolocAPI.EntitySystem.SetupAll<EnvObjectRenderer, EnvObject>(DM_envObject.AllDatas, RenderEnvObject);
	}

	void HideAllEnvObject()
	{
		foreach (EnvObject allData in DM_envObject.AllDatas)
		{
			DolocAPI.EntitySystem.Recycle(allData.Renderer);
		}
		DM_envObject.Clear();
	}

	void RenderEnvObject(EnvObjectRenderer renderer, EnvObject envObject)
	{
		envObject.BaseRenderer = renderer;
		renderer.WorldContent = envObject;
		renderer.position2d = envObject.PositionWS;
		envObject.OnRender();
	}

	void RefreshEnvObjects()
	{
		DM_envObject.Clear();
		EnvObjectSpawnEntry envObjectSpawnEntry = CurrentRoom.RoomSpawnInfo.EnvObjectSpawnEntry;
		if (blockCreate || !EnvObjectGenInfo.shouldGen || envObjectSpawnEntry.IsEmpty)
		{
			return;
		}
		ISpawnLut spawnLut_Ref = envObjectSpawnEntry.SpawnLut_Ref;
		int[] array = LutHelper.Sample(spawnLut_Ref.SpawnWeights, envObjectSpawnEntry.CountRange.RandomCount);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] > 0 && DolocConfig.Tables.TbEnvObject.DataMap.TryGetValue(spawnLut_Ref.SpawnDataList[i].SpawnId, out var value))
			{
				CreateEnvObjectsNoRender(value, array[i]);
			}
		}
	}

	EnvObject[] CreateEnvObjectsNoRender(EnvObjectInfo proto, int count)
	{
		if (count <= 0)
		{
			return Array.Empty<EnvObject>();
		}
		return (from x in ((Vector2Int[])EnvObjectGenInfo.constraint.GetPositions(proto.Type).Clone()).Shuffle().Take(count).ToArray()
			select CreateEnvObjectNoRender(proto, x)).ToArray();
	}

	EnvObject CreateEnvObjectNoRender(EnvObjectInfo proto, Vector2Int anchor)
	{
		Vector2 position = new Vector2(((float)anchor.x + 0.5f) * 1.5f, (float)anchor.y * 1.5f);
		EnvObject envObject = DM_envObject.CreateEnvObject(proto, position);
		if (envObject != null)
		{
			envObject.Host = this;
		}
		return envObject;
	}

	bool RemoveEnvObject(EnvObject envObject)
	{
		if (envObject == null)
		{
			return false;
		}
		if (DM_envObject.RemoveEnvObject(envObject))
		{
			DolocAPI.EntitySystem.Recycle(envObject.Renderer);
			return true;
		}
		return false;
	}

	void SetEnvObjectStatus(bool status)
	{
		blockCreate = status;
		if (status)
		{
			HideAllEnvObject();
		}
	}
}
