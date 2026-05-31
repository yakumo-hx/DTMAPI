using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrabPassFeature : ScriptableRendererFeature
{
	[Serializable]
	public class GrabPassSetting
	{
		public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
	}

	private class GrabPass : ScriptableRenderPass
	{
		private static readonly string k_RenderTag = "grab pass";

		private RenderTargetIdentifier currentTarget;

		private RenderTargetHandle tempColorTarget;

		private string m_GrabPassName = "_GrabPassTexture";

		public GrabPass(GrabPassSetting setting)
		{
			base.renderPassEvent = setting.Event;
			tempColorTarget.Init(m_GrabPassName);
		}

		public void SetUp(RenderTargetIdentifier currentTarget)
		{
			this.currentTarget = currentTarget;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get(k_RenderTag);
			commandBuffer.GetTemporaryRT(tempColorTarget.id, Screen.width, Screen.height);
			commandBuffer.SetGlobalTexture(m_GrabPassName, tempColorTarget.Identifier());
			Blit(commandBuffer, currentTarget, tempColorTarget.Identifier());
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}

	private GrabPass m_ScriptablePass;

	public GrabPassSetting m_Setting = new GrabPassSetting();

	public override void Create()
	{
		m_ScriptablePass = new GrabPass(m_Setting);
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (!renderingData.cameraData.isSceneViewCamera && renderingData.postProcessingEnabled)
		{
			m_ScriptablePass.SetUp(renderer.cameraColorTarget);
			renderer.EnqueuePass(m_ScriptablePass);
		}
	}
}
