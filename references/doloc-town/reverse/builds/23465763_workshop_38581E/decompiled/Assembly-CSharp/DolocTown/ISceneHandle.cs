using System.Linq;
using DolocTown.Config.Room;
using DolocTown.Config.Weather;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public interface ISceneHandle
{
	GameObject GameObject { get; }

	Tilemap Tilemap { get; }

	Tilemap TilemapPt { get; }

	Tilemap TilemapDct { get; }

	void OnLoadScene();

	void OnEnterRoom(Room room);

	void Render(Room room);

	void ClearRender();

	void BeforeTimePass();

	void AfterTimePass();

	void UpdatePerTu();

	void UpdatePerHour(int hour);

	void OnWeatherChanged(WeatherType weather);

	void OnMonthlyRefresh();

	void ResetWaterResolutions(Vector2 resolution);

	void RefreshWaters();

	void AdjustAirWall(Vector2 scenePos, Vector2 sceneSize);

	void LoadScene(DolocTown.Config.Room.SceneInfo sceneInfo)
	{
		OnLoadScene();
		if (!sceneInfo.Id.Contains("dungeon"))
		{
			Debug.Log($"设置场景背景高度偏移:{sceneInfo.HighLevel_Ref.Offset}");
			DolocAPI.envBackgroundEx.SetYOrigin(sceneInfo.HighLevel_Ref.Offset);
		}
	}

	void SendMessage(GameMessage message)
	{
		ResidentOperationTipTrigger[] componentsInScene = GameObject.GetComponentsInScene<ResidentOperationTipTrigger>();
		for (int i = 0; i < componentsInScene.Length; i++)
		{
			componentsInScene[i].SendMessage(message);
		}
	}

	void RefreshCondition()
	{
		IConditionValidator[] componentsInScene = GameObject.GetComponentsInScene<IConditionValidator>(includeInactive: true);
		for (int i = 0; i < componentsInScene.Length; i++)
		{
			componentsInScene[i].ValidateCondition();
		}
	}

	void SetObjectLockState(string lockObjectId, bool value)
	{
		ILockable[] componentsInScene = GameObject.GetComponentsInScene<ILockable>(includeInactive: true);
		foreach (ILockable lockable in componentsInScene)
		{
			if (!(lockable.lockObjectId != lockObjectId))
			{
				lockable.SetLockState(value);
			}
		}
	}

	T[] GetComponentsInScene<T>(bool includeInactive = false)
	{
		return GameObject.scene.GetRootGameObjects().SelectMany((GameObject obj) => obj.GetComponentsInChildren<T>(includeInactive)).ToArray();
	}
}
