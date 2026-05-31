using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class BuildingMaterial : DolocObject
{
	[SerializeField]
	private Shader shader;

	[SerializeField]
	private Sprite gateMask;

	private Material _material;

	private void Start()
	{
		DebugGateMask();
	}

	private void DebugGateMask()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (_material == null)
		{
			_material = new Material(shader);
			component.material = _material;
		}
		Vector4 outerUV = DataUtility.GetOuterUV(GetComponent<SpriteRenderer>().sprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(gateMask);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetVector("_UVInfos", new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y));
		materialPropertyBlock.SetTexture("_BuildingGateMask", gateMask.texture);
		component.SetPropertyBlock(materialPropertyBlock);
	}
}
