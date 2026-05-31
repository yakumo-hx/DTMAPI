using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class SceneLightBuildingGateContent : SceneLight
{
	private Material _material;

	protected override void TurnOn(bool isInitial)
	{
		if (_material == null)
		{
			_material = new Material(LocMaterials.GAME_MAT_BLUR);
			_material.SetFloat("_BlurSize", 10f);
			_material.SetFloat("_BlurIntensity", 0.08f);
			GetComponent<SpriteRenderer>().sharedMaterial = _material;
		}
	}

	protected override void TurnOff(bool isInitial)
	{
		GetComponent<SpriteRenderer>().sharedMaterial = LocMaterials.GAME_MAT_BLUR;
	}

	private void OnDestroy()
	{
		if (_material != null)
		{
			Object.Destroy(_material);
		}
	}
}
