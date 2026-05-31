using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ResourceCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public ResourceListViewer resourceListViewer;

	[SerializeField]
	public ResourceViewer upDownViewer;

	[SerializeField]
	public ResourceViewer leftRightViewer;

	public ScrollRect scrollRect
	{
		get
		{
			if (!upDownViewer.isVisible)
			{
				return leftRightViewer.rect;
			}
			return upDownViewer.rect;
		}
	}

	public float moveDelta => 0.05f;

	public void RenderViewer(ResourceDetailData data)
	{
		upDownViewer.SetVisible(value: false);
		leftRightViewer.SetVisible(value: false);
		if (data.isLeftRightLayout)
		{
			leftRightViewer.Render(data);
			leftRightViewer.SetVisible(value: true);
		}
		else
		{
			upDownViewer.Render(data);
			upDownViewer.SetVisible(value: true);
		}
	}

	public void OnMove()
	{
	}
}
