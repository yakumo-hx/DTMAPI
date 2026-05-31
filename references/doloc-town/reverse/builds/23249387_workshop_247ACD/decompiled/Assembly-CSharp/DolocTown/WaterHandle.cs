using System.Collections.Generic;
using DolocTown.GameData;
using Sirenix.Utilities;
using UnityEngine;

namespace DolocTown;

public class WaterHandle : MonoBehaviour
{
	public const float WaterRendererHeight = 8.4375f;

	public float waterWidth;

	public WaterHeightData defaultData;

	[SerializeField]
	public List<WaterConditionChecker> Conditions = new List<WaterConditionChecker>();

	public Vector2 airWallSize;

	[SerializeField]
	public WaterParamSO waterTemplate;

	public WaterRenderer Renderer { get; set; }

	public WaterType WaterType => waterTemplate.WaterType;

	public WaterHeightData CurrentHeightData { get; private set; }

	private bool hasWater => CurrentHeightData.waterBias > 0f;

	private void Start()
	{
		GetComponentsInChildren<FishingPool>().ForEach(delegate(FishingPool pool)
		{
			pool.SetVisible(value: false);
		});
	}

	public void GenerateWater()
	{
		if (waterTemplate == null)
		{
			return;
		}
		GetCurrentWaterHeightData();
		if (hasWater)
		{
			if (Renderer == null)
			{
				Renderer = DolocAPI.EntitySystem.Next<WaterRenderer>();
			}
			Renderer.WaterHandle = this;
			Renderer.RenderWater(this);
		}
		InitFloatObjects();
		InitFishingPoolHeight();
	}

	public void ClearWater()
	{
		DolocAPI.EntitySystem.Recycle(Renderer);
		Renderer = null;
		GetComponentsInChildren<FishingPool>().ForEach(delegate(FishingPool pool)
		{
			pool.DisposeWater();
			pool.SetVisible(value: false);
		});
	}

	public void ResetResolution(Vector2 resolution)
	{
		if (!(Renderer == null))
		{
			ClearWater();
			GenerateWater();
		}
	}

	public float GetCurrentWaterHeight()
	{
		GetCurrentWaterHeightData();
		return 8.4375f * CurrentHeightData.waterBias * 4f + base.transform.position.y;
	}

	private void GetCurrentWaterHeightData()
	{
		CurrentHeightData = defaultData;
		if (Conditions.IsNullOrEmpty())
		{
			return;
		}
		foreach (WaterConditionChecker condition in Conditions)
		{
			ConditionGroupChecker conditionChecker = condition.conditionChecker;
			if (conditionChecker.IsConditionMet(out var _))
			{
				CurrentHeightData = condition.heightData;
				break;
			}
		}
	}

	private void InitFloatObjects()
	{
		FloatingObjectBase[] componentsInChildren = GetComponentsInChildren<FloatingObjectBase>(includeInactive: true);
		foreach (FloatingObjectBase obj in componentsInChildren)
		{
			obj._controller = Renderer.WaterController;
			obj.ResetFloatObject();
			obj.SetVisible(value: true);
		}
		FloatingObjectPreset[] componentsInChildren2 = GetComponentsInChildren<FloatingObjectPreset>(includeInactive: true);
		foreach (FloatingObjectPreset obj2 in componentsInChildren2)
		{
			obj2.InitFloatingObject(Renderer.WaterController);
			obj2.gameObject.SetActive(value: false);
		}
	}

	private void InitFishingPoolHeight()
	{
		FishingPool[] componentsInChildren = GetComponentsInChildren<FishingPool>(includeInactive: true);
		foreach (FishingPool obj in componentsInChildren)
		{
			float y = CurrentHeightData.waterBias * 8.4375f;
			Vector3 localScale = obj.transform.localScale;
			localScale.y = y;
			obj.transform.localScale = localScale;
			obj.SetWater(Renderer.GetComponent<InteractiveWater>());
			obj.SetVisible(hasWater);
		}
	}
}
