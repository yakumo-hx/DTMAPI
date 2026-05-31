using DolocTown.GameData;
using DolocTown.Rendering;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[GameEntityManager("/global/water", DolocGameAssets.GAME_ENTITY_WATER, Frequency = 10)]
public class WaterRenderer : GameEntity
{
	public WaterHandle WaterHandle;

	private static readonly int ScreenResolution = Shader.PropertyToID("_ScreenResolution");

	private static readonly int WaterDepth = Shader.PropertyToID("_WaterDepth");

	private static readonly int WaterColorDeep = Shader.PropertyToID("_WaterColorDeep");

	private static readonly int FogSpeed = Shader.PropertyToID("_FogSpeed");

	private static readonly int FogIntensity = Shader.PropertyToID("_FogIntensity");

	private static readonly int FogDepth = Shader.PropertyToID("_FogDepth");

	private static readonly int WaterDensity = Shader.PropertyToID("_WaterDensity");

	private static readonly int WaterColor = Shader.PropertyToID("_WaterColor");

	private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

	private static readonly int DistortionIntensity = Shader.PropertyToID("_DistortionIntensity");

	private static readonly int DistortionSpeed = Shader.PropertyToID("_DistortionSpeed");

	private static readonly int WaterScale = Shader.PropertyToID("_WaterScale");

	private static readonly int WaterBias = Shader.PropertyToID("_WaterBias");

	private static readonly int WaterSpringCount = Shader.PropertyToID("_WaterSpringCount");

	public SpriteRenderer Sr { get; private set; }

	public WaterController WaterController { get; private set; }

	public RenderTextureUVHandler UVHandler { get; private set; }

	public BoxCollider2D wallCollider { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		WaterController = GetComponent<WaterController>();
		UVHandler = GetComponent<RenderTextureUVHandler>();
		wallCollider = GetComponentInChildren<BoxCollider2D>(includeInactive: true);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (WaterHandle != null)
		{
			WaterHandle.Renderer = null;
			WaterHandle = null;
			Object.Destroy(Sr.material);
		}
	}

	public void RenderWater(WaterHandle handle)
	{
		switch (handle.WaterType)
		{
		case WaterType.SHALLOW:
			Sr.sharedMaterial = LocMaterials.GAME_MAT_WATER_SHALLOW;
			break;
		case WaterType.DEEP:
			Sr.sharedMaterial = LocMaterials.GAME_MAT_WATER_DEEP;
			break;
		}
		WriteInController();
		WriteInMaterial(Sr.material);
		ResetBoxCollider();
		WaterController.Init();
		WaterController.InitFloatingInfo();
		UVHandler.InitRenderInfo(DolocAPI.worldResolution);
	}

	public void WriteInController()
	{
		WaterParamSO waterTemplate = WaterHandle.waterTemplate;
		WaterController.transform.position = WaterHandle.transform.position;
		WaterController.transform.localScale = new Vector3(WaterHandle.waterWidth, 8.4375f, 1f);
		WaterController.springCount = waterTemplate.springCount;
		WaterController.springConst = waterTemplate.springConst;
		WaterController.damping = waterTemplate.damping;
		WaterController.spread = waterTemplate.spread;
		WaterController.shouldWave = waterTemplate.shouldWave;
		WaterController.simplifyThreshold = waterTemplate.simplifyThreshold;
		WaterController.waterModerate = waterTemplate.waterModerate;
		WaterController.waveParamDefault = waterTemplate.waveParamDefault;
		WaterController.waveParamOnEnter = waterTemplate.waveParamOnEnter;
		WaterController.waveParamOnStay = waterTemplate.waveParamOnStay;
		WaterController.waveParamOnRainDrop = waterTemplate.waveParamOnRainDrop;
		WaterController.waveOnBomb = waterTemplate.waveOnBomb;
		WaterController.waveOnAttack = waterTemplate.waveOnAttack;
	}

	public void WriteInMaterial(Material material)
	{
		WaterParamSO waterTemplate = WaterHandle.waterTemplate;
		material.SetInt(WaterSpringCount, waterTemplate.springCount);
		material.SetFloat(WaterBias, WaterHandle.CurrentHeightData.waterBias);
		material.SetFloat(WaterScale, waterTemplate.waterScale);
		material.SetFloat(DistortionSpeed, waterTemplate.distortionSpeed);
		material.SetFloat(DistortionIntensity, waterTemplate.distortionIntensity);
		material.SetColor(OutlineColor, waterTemplate.waterBorderColor);
		material.SetColor(WaterColor, waterTemplate.waterColor);
		switch (waterTemplate.WaterType)
		{
		case WaterType.SHALLOW:
			material.SetFloat(WaterDensity, waterTemplate.waterDensity);
			break;
		case WaterType.DEEP:
			material.SetFloat(WaterDepth, waterTemplate.waterDepth);
			material.SetColor(WaterColorDeep, waterTemplate.waterDeepColor);
			material.SetFloat(FogSpeed, waterTemplate.fogSpeed);
			material.SetFloat(FogIntensity, waterTemplate.fogIntensity);
			material.SetFloat(FogDepth, waterTemplate.fogDepth);
			break;
		}
	}

	public void GenerateBubbles()
	{
		if (WaterHandle.WaterType != 0)
		{
			Vector2 vector = new Vector2(0f, base.transform.localScale.y * WaterHandle.CurrentHeightData.waterBias * 4f - 2f);
			int num = Random.Range(2, 5);
			for (int i = 0; i < num; i++)
			{
				DolocAPI.RaiseInstantPSEffects(new Vector2(base.transform.position.x + Random.Range(1f, base.transform.localScale.x * 4f - 1f), base.transform.position.y + Random.Range(vector.x, vector.y)), InstantParticleEffectsType.WATER_BUBBLES_DEEP);
			}
		}
	}

	public void ResetResolution(Vector2 resolution)
	{
		Vector2 vector = resolution * 0.25f;
		Vector4 value = new Vector4(1f / vector.x, 1f / vector.y, resolution.x, resolution.y);
		Sr.material.SetVector(ScreenResolution, value);
	}

	private void ResetBoxCollider()
	{
		bool enableAirWall = WaterHandle.CurrentHeightData.enableAirWall;
		wallCollider.gameObject.SetActive(enableAirWall);
		if (enableAirWall)
		{
			Vector2 airWallSize = WaterHandle.airWallSize;
			wallCollider.transform.localScale = new Vector3(airWallSize.x / base.transform.localScale.x, airWallSize.y / base.transform.localScale.y, 1f);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(Sr.material);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			if (WaterHandle != null)
			{
				FishingPool[] componentsInChildren = WaterHandle.GetComponentsInChildren<FishingPool>(includeInactive: true);
				if (componentsInChildren.Length != 0)
				{
					FishingPool fishingPool = componentsInChildren.Choice();
					DolocAPI.AgentEquipmentManager.SetFishingPoolName(fishingPool.PoolName);
				}
			}
		}
		else if (!(other.GetComponent<Rigidbody2D>() == null))
		{
			GetComponent<InteractiveWater>().Splash(other.transform.position);
		}
	}
}
