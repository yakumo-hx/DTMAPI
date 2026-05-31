using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class MyBlitRenderPass : ScriptableRenderPass
{
	private string profilerTag;

	private Material materialToBlit;

	private RenderTargetIdentifier cameraColorTargetIdent;

	private RenderTargetHandle tempTexture;

	public MyBlitRenderPass(string profilerTag, RenderPassEvent renderPassEvent, Material materialToBlit)
	{
		this.profilerTag = profilerTag;
		base.renderPassEvent = renderPassEvent;
		this.materialToBlit = materialToBlit;
	}

	public void SetMaterial(Material material)
	{
		if (!(material == null))
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
		CommandBuffer commandBuffer = CommandBufferPool.Get(profilerTag);
		commandBuffer.Clear();
		commandBuffer.Blit(cameraColorTargetIdent, tempTexture.Identifier(), materialToBlit, 0);
		commandBuffer.Blit(tempTexture.Identifier(), cameraColorTargetIdent);
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
		CommandBufferPool.Release(commandBuffer);
	}

	public override void FrameCleanup(CommandBuffer cmd)
	{
		cmd.ReleaseTemporaryRT(tempTexture.id);
	}
}
