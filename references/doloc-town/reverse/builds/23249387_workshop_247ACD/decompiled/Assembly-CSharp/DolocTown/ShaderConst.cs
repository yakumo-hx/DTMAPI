using UnityEngine;

namespace DolocTown;

public static class ShaderConst
{
	public static readonly int AlphaMask = Shader.PropertyToID("_AlphaMask");

	public static readonly int MainTexUVInfos = Shader.PropertyToID("_MainTex_UVInfos");

	public static readonly int MaskTextUVInfos = Shader.PropertyToID("_MaskTex_UVInfos");
}
