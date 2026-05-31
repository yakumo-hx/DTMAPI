using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class EquipmentBuilderRenderer
{
	public BuilderIndicator indicator;

	public DolocBorderRenderer border;

	private GridRenderer gridRenderer;

	private GridArea effectRenderer;

	private AffectorArea affectorArea;

	private Color validColor => DolocAPI.eftConfig.validIndicatorColor;

	private Color invalidColor => DolocAPI.eftConfig.invalidIndicatorColor;

	private Color validColorNoHdr => DolocAPI.eftConfig.validIndicatorColorNoHdr;

	private Color invalidColorNoHdr => DolocAPI.eftConfig.invalidIndicatorColorNoHdr;

	public Vector2 indicatorPos
	{
		set
		{
			indicator.position = value;
		}
	}

	public Vector2 BoarderSizeDelta
	{
		set
		{
			border.SizeDelta = DolocAPI.TileSizeToScreenSize(value);
		}
	}

	public bool isIndicatorValid
	{
		set
		{
			indicator.indicatorColor = (value ? validColor : invalidColor);
			if (!(effectRenderer == null))
			{
				effectRenderer.Color = (value ? validColorNoHdr : invalidColorNoHdr);
			}
		}
	}

	public bool isIndicatorVisible
	{
		set
		{
			indicator.SetVisible(value);
		}
	}

	public Vector2Int GridIndicatorPosition
	{
		set
		{
			gridRenderer.GridPosition = value;
			if (affectorArea != null)
			{
				effectRenderer.GridPosition = gridRenderer.GridPosition - new Vector2Int(affectorArea.horizontalRange, affectorArea.verticalRangeBottom);
			}
		}
	}

	public EquipmentBuilderRenderer()
	{
		border = DolocAPI.uiSystem.GetFromPoolInScene<DolocBorderRenderer>();
		border.Color = DolocUiColor.EYECATCHCOLOR_CYAN;
		border.SetVisible(value: false);
		indicator = DolocAPI.EntitySystem.Next<BuilderIndicator>();
		gridRenderer = DolocAPI.EntitySystem.Next<GridRenderer>();
		gridRenderer.BorderColor1 = DolocAPI.eftConfig.terrainValidColor;
	}

	public void SetGridSize(Vector2Int gridIndicatorSize, AffectorArea affectorArea = null)
	{
		gridRenderer.GridSize = gridIndicatorSize;
		this.affectorArea = affectorArea;
		if (affectorArea != null)
		{
			effectRenderer = DolocAPI.EntitySystem.Next<GridArea>();
			effectRenderer.GridSize = gridIndicatorSize + new Vector2Int(2 * affectorArea.horizontalRange, affectorArea.verticalRangeTop + affectorArea.verticalRangeBottom);
			this.affectorArea = affectorArea;
		}
	}

	public void Dispose()
	{
		DolocAPI.EntitySystem.Recycle(effectRenderer);
		DolocAPI.EntitySystem.Recycle(indicator);
		DolocAPI.EntitySystem.Recycle(gridRenderer);
		DolocAPI.uiSystem.RecycleToPoolInScene(border);
	}
}
