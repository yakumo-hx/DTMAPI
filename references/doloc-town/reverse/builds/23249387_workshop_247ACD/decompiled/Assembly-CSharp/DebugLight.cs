using UnityEngine;
using UnityEngine.Sprites;

public class DebugLight : MonoBehaviour
{
	[SerializeField]
	private SpriteRenderer sr;

	[SerializeField]
	private Sprite lightEmissionTex;

	public void Test()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		Vector4 outerUV = DataUtility.GetOuterUV(sr.sprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(lightEmissionTex);
		Vector4 value = new Vector4(outerUV2.x, outerUV2.y, outerUV.x, outerUV.y);
		materialPropertyBlock.SetTexture("_MainTex", sr.sprite.texture);
		materialPropertyBlock.SetTexture("_EmissionTex", lightEmissionTex.texture);
		materialPropertyBlock.SetVector("_EmissionTex_Position", value);
		sr.SetPropertyBlock(materialPropertyBlock);
	}
}
