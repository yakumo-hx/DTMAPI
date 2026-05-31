using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class CropGeneFunctionFirefly : CropGeneFunction
{
	private readonly MaterialPropertyBlock propertyBlock;

	public CropGeneFunctionFirefly(CropGeneInfo geneProto)
		: base(geneProto)
	{
		propertyBlock = new MaterialPropertyBlock();
	}

	[JsonConstructor]
	public CropGeneFunctionFirefly(string geneId)
		: base(geneId)
	{
		propertyBlock = new MaterialPropertyBlock();
	}

	public override void OnCropDead(bool shouldRender)
	{
		base.OnCropDead(shouldRender);
		if (shouldRender)
		{
			OnRender();
		}
	}

	public override void OnCropLevelDown(bool shouldRender)
	{
		base.OnCropLevelDown(shouldRender);
		if (shouldRender)
		{
			OnRender();
		}
	}

	public override void OnCropLevelUp(bool shouldRender)
	{
		base.OnCropLevelUp(shouldRender);
		if (shouldRender)
		{
			OnRender();
		}
	}

	public override void OnRender()
	{
		base.OnRender();
		RenderFirefly();
	}

	private void RenderFirefly()
	{
		if (base.crop.isDead)
		{
			base.crop.Renderer.SR.sharedMaterial = DolocGameAssets.GAME_MAT_CROP.LoadMaterial();
			return;
		}
		base.crop.Renderer.SR.sharedMaterial = DolocGameAssets.GAME_MAT_RIM_LIGHT.LoadMaterial();
		base.crop.Renderer.SR.GetPropertyBlock(propertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(base.crop.CurrentSprite);
		propertyBlock.SetVector("_MainTex_UVInfos", outerUV);
		base.crop.Renderer.SR.SetPropertyBlock(propertyBlock);
	}
}
