using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class HoverBoxGroup : DolocUIPanel
{
	[SerializeField]
	private ItemHoverBox itemHoverBox;

	[SerializeField]
	private SimpleHoverBox simpleHoverBox;

	[SerializeField]
	private TextHoverBox textHoverBox;

	private HoverBoxBase[] hoverBoxes;

	private RectTransform targetRectTrans;

	private UIAlignmentType targetAnchor;

	private HoverBoxBase currentHoverBox;

	private Vector3[] targetBounds = new Vector3[4];

	private Vector3 latestAnchorPos;

	protected override void __Init()
	{
		base.__Init();
		hoverBoxes = new HoverBoxBase[3] { itemHoverBox, simpleHoverBox, textHoverBox };
		HoverBoxBase[] array = hoverBoxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init();
		}
		HideAll();
	}

	public void RenderAndShow(RectTransform targetRectTrans, ItemData item, UIAlignmentType targetAnchor, UIAlignmentType hoverPivot)
	{
		HideAll();
		HoverTo(itemHoverBox, targetRectTrans, targetAnchor, hoverPivot);
		itemHoverBox.RenderAndShow(item);
		Show();
	}

	public void RenderAndShowText(RectTransform targetRectTrans, string title, UIAlignmentType targetAnchor, UIAlignmentType hoverPivot, bool autoFade, HoverBoxStyle style, TextAlignmentOptions alignment)
	{
		HideAll();
		HoverTo(simpleHoverBox, targetRectTrans, targetAnchor, hoverPivot);
		simpleHoverBox.RenderAndShow(title, autoFade, style, alignment);
		Show();
	}

	public void RenderAndShowText(RectTransform targetRectTrans, TextGroup data, UIAlignmentType targetAnchor, UIAlignmentType hoverPivot)
	{
		HideAll();
		HoverTo(textHoverBox, targetRectTrans, targetAnchor, hoverPivot);
		textHoverBox.RenderAndShow(data);
		Show();
	}

	private void HoverTo(HoverBoxBase hoverBox, RectTransform targetRectTrans, UIAlignmentType targetAnchor, UIAlignmentType hoverPivot)
	{
		this.targetRectTrans = targetRectTrans;
		this.targetAnchor = targetAnchor;
		currentHoverBox = hoverBox;
		hoverBox.SetAnchorAndPivot(hoverPivot);
		Vector3 vector = UpdateAnchorPosition();
		hoverBox.position = vector;
		hoverBox.AdaptionBoundary();
		latestAnchorPos = hoverBox.position;
	}

	private void HideAll()
	{
		HoverBoxBase[] array = hoverBoxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Hide();
		}
	}

	private Vector3 UpdateAnchorPosition()
	{
		return (Vector2)targetRectTrans.position + targetRectTrans.sizeDelta * (DolocUtils.GetPivotVector(targetAnchor) - targetRectTrans.pivot) / DolocAPI.screenManager.scaleFactor;
	}

	private void RefreshHoverBoxPosition()
	{
		Vector3 vector = UpdateAnchorPosition();
		HoverBoxBase[] array = hoverBoxes;
		foreach (HoverBoxBase obj in array)
		{
			obj.transform.position = vector;
			obj.AdaptionBoundary();
		}
	}

	private void LateUpdate()
	{
		if (!base.isRender || targetRectTrans == null)
		{
			return;
		}
		DolocAPI.DelayFrame(delegate
		{
			Vector3 vector = UpdateAnchorPosition();
			if ((latestAnchorPos - vector).magnitude > 0.001f)
			{
				latestAnchorPos = vector;
				RefreshHoverBoxPosition();
			}
		});
	}
}
