using UnityEngine;

public static class HDRColorDecomposer
{
	private const byte KMaxByteForOverexposedColor = 191;

	public static void DecomposeHdrColor(Color linearColorHdr, out Color32 baseLinearColor, out float exposure)
	{
		baseLinearColor = Color.black;
		exposure = 0f;
		float maxColorComponent = linearColorHdr.maxColorComponent;
		if (maxColorComponent == 0f || (maxColorComponent <= 1f && maxColorComponent >= 0.003921569f))
		{
			exposure = 0f;
			baseLinearColor.r = (byte)Mathf.RoundToInt(linearColorHdr.r * 255f);
			baseLinearColor.g = (byte)Mathf.RoundToInt(linearColorHdr.g * 255f);
			baseLinearColor.b = (byte)Mathf.RoundToInt(linearColorHdr.b * 255f);
			baseLinearColor.a = (byte)Mathf.RoundToInt(linearColorHdr.a * 255f);
		}
		else
		{
			float num = 191f / maxColorComponent;
			exposure = Mathf.Log(255f / num) / Mathf.Log(2f);
			baseLinearColor.r = (byte)Mathf.Min(191, Mathf.CeilToInt(num * linearColorHdr.r));
			baseLinearColor.g = (byte)Mathf.Min(191, Mathf.CeilToInt(num * linearColorHdr.g));
			baseLinearColor.b = (byte)Mathf.Min(191, Mathf.CeilToInt(num * linearColorHdr.b));
			baseLinearColor.a = (byte)Mathf.RoundToInt(linearColorHdr.a * 255f);
		}
	}
}
