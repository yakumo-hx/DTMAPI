using System;
using System.Collections.Generic;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class SceneLightManager
{
	private readonly SceneLight[] lights;

	private readonly bool isEmpty;

	public SceneLightManager(IEnumerable<SceneLight> lights)
	{
		List<SceneLight> list = new List<SceneLight>();
		foreach (SceneLight light in lights)
		{
			if (!(light == null))
			{
				try
				{
					light.Init();
					light.gameObject.SetActive(value: true);
					list.Add(light);
				}
				catch (Exception exception)
				{
					Debug.Log("初始化场景灯光\"" + light.gameObject.name + "\"时遇到异常");
					Debug.LogException(exception);
				}
			}
		}
		this.lights = list.ToArray();
		isEmpty = this.lights.IsNullOrEmpty();
	}

	public void Dispose()
	{
		if (!isEmpty)
		{
			SceneLight[] array = lights;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
		}
	}

	public void UpdateSwitchStatus(bool isInitial = false)
	{
		if (!isEmpty)
		{
			SwitchScheduleParams param = new SwitchScheduleParams(DolocAPI.archiveHandle.DateNow, DolocAPI.archiveHandle.CurrentWeatherType);
			SceneLight[] array = lights;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnLightParamChanged(param, isInitial);
			}
		}
	}

	public void UpdatePerTu()
	{
		if (isEmpty)
		{
			return;
		}
		SceneLight[] array = lights;
		foreach (SceneLight sceneLight in array)
		{
			if (sceneLight.IsTurnOn)
			{
				sceneLight.OnUpdatePerTu();
			}
		}
	}
}
