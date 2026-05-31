using UnityEngine;

namespace DolocTown;

public static class EquipmentHostPatch_OfCrop
{
	public static void RenderCrop(this IEquipmentHost host, Crop crop, Vector3 positionWS)
	{
		CropRenderer cropRenderer = DolocAPI.EntitySystem.Next<CropRenderer>();
		if (!(cropRenderer == null))
		{
			crop.Renderer = cropRenderer;
			cropRenderer.Crop = crop;
			cropRenderer.Sprite = crop.CurrentSprite;
			cropRenderer.position = positionWS;
			cropRenderer.SR.sharedMaterial = DolocGameAssets.GAME_MAT_CROP.LoadMaterial();
			cropRenderer.ToggleSweepLight(crop.isMature);
		}
	}

	public static void RecycleCropRenderer(this IEquipmentHost host, CropRenderer renderer)
	{
		DolocAPI.EntitySystem.Recycle(renderer);
	}
}
