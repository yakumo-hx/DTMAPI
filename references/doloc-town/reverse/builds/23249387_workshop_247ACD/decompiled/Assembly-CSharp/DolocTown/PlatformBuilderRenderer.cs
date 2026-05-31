using System.Collections.Generic;
using DolocTown.Config.Platform;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class PlatformBuilderRenderer
{
	private DolocBorderRenderer border;

	private BuilderItemCostTip itemCostTip;

	private PlatformIndicator indicator;

	private PlatformIndicator indicatorCut;

	private PlainTextTip entityTip;

	private Color invalidColor => DolocAPI.eftConfig.invalidIndicatorColor;

	private Color validColor => DolocAPI.eftConfig.validIndicatorColor;

	public Vector2 BorderPosition
	{
		set
		{
			border.position = value;
		}
	}

	public Vector3 DraftPosition
	{
		set
		{
			indicator.transform.position = value;
			indicatorCut.transform.position = value;
		}
	}

	public bool BorderValid
	{
		set
		{
			border.Color = (value ? DolocUiColor.EYECATCHCOLOR_CYAN : DolocUiColor.EYECATCHCOLOR_RED);
		}
	}

	public bool DraftValid
	{
		set
		{
			indicator.SetMaterialColor("_BaseColor", value ? validColor : invalidColor);
		}
	}

	public Vector2 AreaPosition
	{
		set
		{
			entityTip.position = value;
		}
	}

	public Vector2 ItemCostTipPosition
	{
		set
		{
			itemCostTip.position = value;
		}
	}

	public PlatformBuilderRenderer()
	{
		border = DolocAPI.uiSystem.GetFromPoolInScene<DolocBorderRenderer>();
		border.SizeDelta = DolocAPI.TileSizeToScreenSize(Vector2.one);
		itemCostTip = DolocAPI.uiSystem.GetEntity<BuilderItemCostTip>();
		itemCostTip.Hide();
		indicator = DolocAPI.EntitySystem.Next<PlatformIndicator>();
		indicator.SetMaterialColor("_BaseColor", validColor);
		indicatorCut = DolocAPI.EntitySystem.Next<PlatformIndicator>();
		indicatorCut.SetMaterialColor("_BaseColor", DolocAPI.eftConfig.destroyIndicatorColor);
		entityTip = DolocAPI.uiSystem.GetFromPoolInScene<PlainTextTip>();
		entityTip.Hide();
	}

	public void SetDraft(PlatformGeometry draft, PlatformInfo proto, Vector2 tipPosition)
	{
		indicator.tilemap.SetPlatformDraft(draft, proto);
		itemCostTip.Show();
		itemCostTip.position = tipPosition;
		Vector2Int coveredSize = draft.CoveredSize;
		entityTip.Description = $"{coveredSize.x} × {coveredSize.y}";
		entityTip.rectTransform.sizeDelta = DolocAPI.TileSizeToScreenSize(coveredSize);
		entityTip.Show();
	}

	public void SetDraft(PlatformGeometry pt, PlatformInfo proto)
	{
		indicator.tilemap.SetPlatformDraft(pt, proto);
		itemCostTip.Hide();
		entityTip.Hide();
	}

	public void SetDraft(Vector2Int pos, Tile tile)
	{
		indicator.Clear();
		indicator.SetTile(pos, tile);
	}

	public void ClearDraft()
	{
		indicator.Clear();
		indicatorCut.Clear();
		itemCostTip.Hide();
		entityTip.Hide();
	}

	public void SetCutInfos(IEnumerable<Vector2Int> positions, Tile tile)
	{
		indicatorCut.Clear();
		foreach (Vector2Int position in positions)
		{
			indicatorCut.SetTile(position, tile);
		}
	}

	public void UpdateCostInfos(CostViewerData data)
	{
		itemCostTip.Render(data);
	}

	public void Dispose()
	{
		ClearDraft();
		DolocAPI.EntitySystem.Recycle(indicator);
		DolocAPI.EntitySystem.Recycle(indicatorCut);
		DolocAPI.uiSystem.RecycleToPoolInScene(entityTip);
		DolocAPI.uiSystem.RecycleToPoolInScene(border);
		itemCostTip.Hide();
	}
}
