using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

[GameEntityManager("farm/building", DolocGameAssets.GAME_ENTITY_BUILDING)]
public class BuildingRenderer : GameRendererEntity
{
	[SerializeField]
	public SpriteRenderer spriteRenderer;

	[HideInInspector]
	public IconWithNuberTip healthTip;

	private MaterialPropertyBlock propertyBlock;

	public override SpriteRenderer outlineTarget => spriteRenderer;

	protected override Sprite overrideOutlineSprite => Building.DoorSprite;

	protected override ObjectOutlineType outlineType => ObjectOutlineType.ExcludeBottom;

	public Building Building { get; set; }

	public Color Color
	{
		set
		{
			spriteRenderer.color = new Color(value.r, value.g, value.b, spriteRenderer.color.a);
		}
	}

	private Vector3 healthShowPosition => Building.HealthTipPosition;

	public void OnDamage()
	{
		healthTip.RollNumber(DolocColor.drakRed, Building.Health);
		UpdateRenderStatus();
	}

	public void OnRepair()
	{
		healthTip.RollNumber(DolocColor.green, Building.Health);
		UpdateRenderStatus();
	}

	public void OnRender()
	{
		healthTip = DolocAPI.uiSystem.GetFromPoolInScene<IconWithNuberTip>();
		healthTip.SetVisible(value: false);
		UpdateRenderStatus();
	}

	public void OnUnRender()
	{
		DolocAPI.uiSystem.RecycleToPoolInScene(healthTip);
		healthTip = null;
	}

	public void ShowHealthTip()
	{
		if (DolocAPI.archiveHandle.ShouldShowBuildingHealth() && !Building.IsCellar())
		{
			healthTip.Render(healthShowPosition, Building.Health);
			UpdateRenderStatus();
		}
	}

	public void HideHealthTip()
	{
		healthTip.SetVisible(value: false);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (Building != null)
		{
			Building.OnUnRender();
			Building.Renderer = null;
			Building = null;
		}
	}

	public void InitMaterialInfos()
	{
		propertyBlock = new MaterialPropertyBlock();
		spriteRenderer.GetPropertyBlock(propertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(spriteRenderer.sprite);
		propertyBlock.SetVector("_MainTex_UVs", outerUV);
		spriteRenderer.SetPropertyBlock(propertyBlock);
	}

	public void UpdateRenderStatus()
	{
		float healthProcess = Building.HealthProcess;
		healthTip.SetIconSprite(DolocAPI.GlobalParameter.GetBuildingDamageSprite(healthProcess));
		spriteRenderer.color = new Color(1f, 1f, 1f, (!(healthProcess > 0f)) ? 1 : 0);
	}
}
