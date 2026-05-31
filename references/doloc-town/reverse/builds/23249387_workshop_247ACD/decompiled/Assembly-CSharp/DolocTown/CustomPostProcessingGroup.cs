using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class CustomPostProcessingGroup : ScriptableRendererFeature
{
	[Serializable]
	public class FeatureSettings
	{
		public bool IsEnabled = true;

		public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;

		public Material[] MaterialsToBlit;
	}

	public FeatureSettings settings = new FeatureSettings();

	private RenderTargetHandle renderTextureHandle;

	private CustomPostProcessingGroupPass pass;

	public override void Create()
	{
		pass = new CustomPostProcessingGroupPass("Custom Post Processing", settings.WhenToInsert, settings.MaterialsToBlit);
	}

	public void SetMaterials(Material[] materials)
	{
		if (!materials.IsNullOrEmpty())
		{
			settings.MaterialsToBlit = materials;
			pass.SetMaterial(materials);
		}
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (settings.IsEnabled)
		{
			RenderTargetIdentifier cameraColorTarget = renderer.cameraColorTarget;
			pass.Setup(cameraColorTarget);
			renderer.EnqueuePass(pass);
		}
	}
}
