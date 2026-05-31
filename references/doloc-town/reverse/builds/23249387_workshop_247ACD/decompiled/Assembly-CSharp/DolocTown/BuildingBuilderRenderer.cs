using UnityEngine;

namespace DolocTown;

public class BuildingBuilderRenderer
{
	private TilemapGroupManager supportIndicator;

	public EquipmentBuilderRenderer buildingIndicator;

	private Color invalidColor => DolocAPI.eftConfig.invalidIndicatorColor;

	private Color validColor => DolocAPI.eftConfig.validIndicatorColor;

	public BuildingBuilderRenderer(Vector2 roomPosition, Vector2Int gridSize)
	{
		supportIndicator = DolocAPI.EntitySystem.Next<TilemapGroupManager>();
		supportIndicator.transform.position = roomPosition;
		supportIndicator.SetMaterial(LocMaterials.GAME_MAT_HOLOGRAM);
		buildingIndicator = new EquipmentBuilderRenderer();
		buildingIndicator.SetGridSize(gridSize);
		HideAll();
	}

	public void HideAll()
	{
		HideSupportDraft();
		HideBuildingDraft();
	}

	public void ShowBuildingDraft(Vector2Int gridPos, Vector3 spritePos, bool valid)
	{
		buildingIndicator.GridIndicatorPosition = gridPos;
		buildingIndicator.indicatorPos = spritePos;
		buildingIndicator.isIndicatorValid = valid;
		buildingIndicator.isIndicatorVisible = true;
	}

	public void ShowSupportDraft(BuildingSupport support, bool valid)
	{
		supportIndicator.ClearAllTiles();
		if (support != null)
		{
			supportIndicator.SetMaterialColor("_BaseColor", valid ? validColor : invalidColor);
			support.Render(supportIndicator);
			supportIndicator.SetVisible(value: true);
		}
	}

	public void HideSupportDraft()
	{
		supportIndicator.SetVisible(value: false);
	}

	public void ClearDraft()
	{
		supportIndicator.ClearAllTiles();
	}

	public void Dispose()
	{
		buildingIndicator.Dispose();
		DolocAPI.EntitySystem.Recycle(supportIndicator);
	}

	private void HideBuildingDraft()
	{
		buildingIndicator.isIndicatorVisible = false;
	}
}
