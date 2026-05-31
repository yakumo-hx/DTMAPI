using System;
using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionLight : DroneFunction
{
	private readonly DroneFunctionProtoLight protoLight;

	private GameObject lightRenderer;

	private bool isTurnOn;

	public DroneFunctionLight(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoLight = (DroneFunctionProtoLight)proto;
		lightRenderer = CreateLightRenderer();
	}

	private GameObject CreateLightRenderer()
	{
		if (Enum.TryParse<DolocGameAssets>(protoLight.EntityName, ignoreCase: true, out var result))
		{
			return result.CreateEntity();
		}
		return null;
	}

	protected override void OnRender(DroneComponentRenderer renderer)
	{
		if (lightRenderer != null && lightRenderer.transform.parent != renderer.transform)
		{
			lightRenderer.transform.SetParent(renderer.transform);
			lightRenderer.transform.localPosition = new Vector3(0.566f, -0.646f);
			lightRenderer.transform.localScale = Vector3.one;
		}
		if (DolocAPI.archiveHandle.ShouldDroneLightUp)
		{
			if (lightRenderer != null)
			{
				lightRenderer.SetActive(value: true);
			}
			renderer.spriteRenderer.ToggleLightOn(protoLight.LightMask.Asset, Color.white);
			isTurnOn = true;
		}
		else
		{
			if (lightRenderer != null)
			{
				lightRenderer.SetActive(value: false);
			}
			renderer.spriteRenderer.ToggleLightOff();
			isTurnOn = false;
		}
	}

	public override void Dispose()
	{
		if (lightRenderer != null)
		{
			UnityEngine.Object.Destroy(lightRenderer);
		}
		if (isTurnOn)
		{
			comRenderer.spriteRenderer.ToggleLightOff();
		}
	}

	public override void OnUpdatePerTu()
	{
		if (DolocAPI.archiveHandle.ShouldLightUp == isTurnOn)
		{
			return;
		}
		isTurnOn = !isTurnOn;
		if (isTurnOn)
		{
			if (lightRenderer != null)
			{
				lightRenderer.SetActive(value: true);
			}
			comRenderer.spriteRenderer.ToggleLightOn(protoLight.LightMask.Asset, Color.white);
		}
		else
		{
			if (lightRenderer != null)
			{
				lightRenderer.SetActive(value: false);
			}
			comRenderer.spriteRenderer.ToggleLightOff();
		}
	}
}
