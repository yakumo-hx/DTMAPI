using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

internal class CustomPostProcessingGroupPass : ScriptableRenderPass
{
	private string profilerTag;

	private Material[] materialToBlit;

	private RenderTargetIdentifier cameraColorTargetIdent;

	private RenderTargetHandle tempTexture;

	public CustomPostProcessingGroupPass(string profilerTag, RenderPassEvent renderPassEvent, Material[] materialToBlit)
	{
		this.profilerTag = profilerTag;
		base.renderPassEvent = renderPassEvent;
		this.materialToBlit = materialToBlit;
	}

	public void SetMaterial(Material[] material)
	{
		if (material != null)
		{
			materialToBlit = material;
		}
	}

	public void Setup(RenderTargetIdentifier cameraColorTargetIdent)
	{
		this.cameraColorTargetIdent = cameraColorTargetIdent;
	}

	public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
	{
		cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
	}

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	{
		if (!materialToBlit.IsNullOrEmpty())
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get(profilerTag);
			commandBuffer.Clear();
			Material[] array = materialToBlit;
			foreach (Material mat in array)
			{
				commandBuffer.Blit(cameraColorTargetIdent, tempTexture.Identifier(), mat, 0);
				commandBuffer.Blit(tempTexture.Identifier(), cameraColorTargetIdent);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();
			CommandBufferPool.Release(commandBuffer);
		}
	}

	public override void FrameCleanup(CommandBuffer cmd)
	{
		cmd.ReleaseTemporaryRT(tempTexture.id);
	}
}
