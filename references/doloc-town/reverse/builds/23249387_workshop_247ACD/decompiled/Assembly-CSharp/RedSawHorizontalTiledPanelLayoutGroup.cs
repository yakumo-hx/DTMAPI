using UnityEngine;
using UnityEngine.UI;

public class RedSawHorizontalTiledPanelLayoutGroup : LayoutGroup
{
	[Header("the size of side bar")]
	[SerializeField]
	public Vector2 sideSize;

	private Vector2 panelSize;

	private float row2;

	private float col2;

	public bool shouldUpdate;

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

	public float sideLength => sideSize.x;

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
	}

	public override void CalculateLayoutInputVertical()
	{
	}

	public void updateLayout()
	{
		calTiledPanel();
	}

	protected void calTiledPanel()
	{
		m_Tracker.Clear();
		if (base.transform.childCount == 3)
		{
			base.rectTransform.pivot = Vector2.zero;
			Vector2 sizeDelta = new Vector2(base.rectTransform.sizeDelta.x, sideSize.y);
			base.rectTransform.sizeDelta = sizeDelta;
			for (int i = 0; i < 3; i++)
			{
				RectTransform rectTransform = base.rectChildren[i];
				m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot);
			}
			base.rectChildren[0].sizeDelta = sideSize;
			base.rectChildren[1].sizeDelta = new Vector2(sizeDelta.x - sideSize.x * 2f, sizeDelta.y);
			base.rectChildren[2].sizeDelta = sideSize;
			base.rectChildren[0].localPosition = Vector3.zero;
			base.rectChildren[1].localPosition = new Vector3(sideSize.x, 0f, 0f);
			base.rectChildren[2].localPosition = new Vector3(sizeDelta.x - sideSize.x, 0f, 0f);
		}
	}
}
