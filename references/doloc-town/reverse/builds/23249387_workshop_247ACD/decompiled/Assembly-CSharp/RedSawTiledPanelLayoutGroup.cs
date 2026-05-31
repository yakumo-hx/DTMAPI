using System;
using UnityEngine;
using UnityEngine.UI;

public class RedSawTiledPanelLayoutGroup : LayoutGroup
{
	[Header("the length of corner(force treat as square)")]
	[SerializeField]
	private float cornerSideLength;

	[Header("the final size of panel")]
	[SerializeField]
	private Vector2 panelMinSize = new Vector2(10f, 10f);

	private float cornerTotalLength;

	private Vector2 panelSize;

	private float row2;

	private float col2;

	public bool shouldUpdate;

	public float cornerLength
	{
		get
		{
			return cornerSideLength;
		}
		set
		{
			cornerSideLength = value;
		}
	}

	public Vector2 size
	{
		get
		{
			return base.rectTransform.sizeDelta;
		}
		set
		{
			base.rectTransform.sizeDelta = value;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	public override void SetLayoutHorizontal()
	{
		if (shouldUpdate)
		{
			calTiledPanel();
		}
	}

	public override void SetLayoutVertical()
	{
	}

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();
		if (shouldUpdate)
		{
			calGeometryInfos();
		}
	}

	public override void CalculateLayoutInputVertical()
	{
	}

	public void manualUpdate()
	{
		calGeometryInfos();
		calTiledPanel();
	}

	protected void calTiledPanel()
	{
		m_Tracker.Clear();
		if (base.transform.childCount == 0 || base.rectChildren.Count <= 0)
		{
			return;
		}
		int num = Math.Min(base.rectChildren.Count, 9);
		for (int i = 0; i < num; i++)
		{
			RectTransform rectTransform = base.rectChildren[i];
			m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot);
			rectTransform.pivot = Vector2.zero;
			rectTransform.anchorMax = Vector2.zero;
			rectTransform.anchorMin = Vector2.zero;
			switch (i)
			{
			case 0:
				handleUpLeft(rectTransform);
				break;
			case 1:
				handleUpCenter(rectTransform);
				break;
			case 2:
				handleUpRight(rectTransform);
				break;
			case 3:
				handleCenterLeft(rectTransform);
				break;
			case 4:
				handleCenter(rectTransform);
				break;
			case 5:
				handleCenterRight(rectTransform);
				break;
			case 6:
				handleBottomLeft(rectTransform);
				break;
			case 7:
				handleBottomCenter(rectTransform);
				break;
			case 8:
				handleBottomRight(rectTransform);
				break;
			}
		}
	}

	protected void calGeometryInfos()
	{
		cornerTotalLength = cornerSideLength * 2f;
		Vector2 vector = base.rectTransform.rect.size;
		panelSize = new Vector2(Mathf.Max(vector.x - cornerTotalLength, panelMinSize.x), Mathf.Max(vector.y - cornerTotalLength, panelMinSize.y));
		row2 = panelSize.y + cornerSideLength;
		col2 = panelSize.x + cornerSideLength;
		SetLayoutInputForAxis(cornerTotalLength + panelSize.x, -1f, -1f, 0);
		SetLayoutInputForAxis(cornerTotalLength + panelSize.y, -1f, -1f, 1);
	}

	protected void handleUpLeft(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(0f, row2);
	}

	protected void handleUpRight(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(col2, row2);
	}

	protected void handleBottomLeft(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(0f, 0f);
	}

	protected void handleBottomRight(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(col2, 0f);
	}

	protected void handleUpCenter(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(cornerSideLength, row2);
	}

	protected void handleCenterLeft(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);
		child.localPosition = new Vector3(0f, cornerSideLength);
	}

	protected void handleCenter(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);
		child.localPosition = new Vector3(cornerSideLength, cornerSideLength);
	}

	protected void handleCenterRight(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerSideLength);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);
		child.localPosition = new Vector3(col2, cornerSideLength);
	}

	protected void handleBottomCenter(RectTransform child)
	{
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
		child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerSideLength);
		child.localPosition = new Vector3(cornerSideLength, 0f);
	}
}
