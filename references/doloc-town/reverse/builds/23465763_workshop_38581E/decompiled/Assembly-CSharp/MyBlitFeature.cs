using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyBlitFeature : ScriptableRendererFeature
{
	[Serializable]
	public class MyFeatureSettings
	{
		public bool IsEnabled = true;

		public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;

		public Material MaterialToBlit;
	}

	public MyFeatureSettings settings = new MyFeatureSettings();

	private RenderTargetHandle renderTextureHandle;

	private MyBlitRenderPass myRenderPass;

	public override void Create()
	{
		myRenderPass = new MyBlitRenderPass("My custom pass", settings.WhenToInsert, settings.MaterialToBlit);
	}

	public void SetMaterial(Material material)
	{
		if (!(material == null))
		{
			settings.MaterialToBlit = material;
			myRenderPass.SetMaterial(material);
		}
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (settings.IsEnabled)
		{
			RenderTargetIdentifier cameraColorTarget = renderer.cameraColorTarget;
			myRenderPass.Setup(cameraColorTarget);
			renderer.EnqueuePass(myRenderPass);
		}
	}
}
