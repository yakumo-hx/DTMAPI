using UnityEngine;
using UnityEngine.Sprites;

namespace RedSaw.Shader;

public static class ShaderUtils
{
	public static Vector4 CalcMaskSampleUVInfos(this Sprite maskTex, Sprite mainTex)
	{
		Vector4 outerUV = DataUtility.GetOuterUV(mainTex);
		Vector4 outerUV2 = DataUtility.GetOuterUV(maskTex);
		return new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y);
	}

	public static Vector4 GetUVOfSortingLayer(Vector2 relativePositionToCamLB, Vector2 worldSize, Vector2 camWorldSize)
	{
		float num = 1f / camWorldSize.x;
		float num2 = 1f / camWorldSize.y;
		return new Vector4(relativePositionToCamLB.x * num, relativePositionToCamLB.y * num2, worldSize.x * num, worldSize.y * num2);
	}
}
