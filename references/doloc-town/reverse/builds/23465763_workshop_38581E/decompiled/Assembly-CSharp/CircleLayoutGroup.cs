using System;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Circle Layout Group", 150)]
public class CircleLayoutGroup : LayoutGroup
{
	public enum LayoutMode
	{
		Hypodispersion,
		Sector
	}

	[Header("分布模式: Hypodispersion(圆形平均分布) Sector(扇形分布)")]
	public LayoutMode mode;

	[Header("半径")]
	public float radius;

	[Header("起始角度")]
	public float initAngle;

	[Header("是否保持弧度值不变")]
	public bool keepRadLen;

	[Header("弧度保持不变的值(keepRadLen为true时有效)")]
	public float keepRadLenVal;

	[Header("扇形分布范围")]
	public float sectorAngle;

	[Header("扇形分布时且keepRadLen为false时,是否中间对齐到扇形中心,否则两端对齐")]
	public bool sectorAlignCenter;

	[Header("扇形分布且sectorAlignCenter为false时,是否为逆时针")]
	public bool sectorClockwise = true;

	private int count;

	protected override void OnEnable()
	{
		base.OnEnable();
		CalculateRadial();
	}

	public override void SetLayoutHorizontal()
	{
		CalculateRadial();
	}

	public override void SetLayoutVertical()
	{
		CalculateRadial();
	}

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();
		CalculateRadial();
	}

	public override void CalculateLayoutInputVertical()
	{
		CalculateRadial();
	}

	protected void CalculateRadial()
	{
		count++;
		Debug.Log($"重新计算Radial:{count}");
		m_Tracker.Clear();
		if (base.transform.childCount != 0)
		{
			if (mode == LayoutMode.Hypodispersion)
			{
				Hypodispersion();
			}
			else if (mode == LayoutMode.Sector)
			{
				Sector();
			}
		}
	}

	private void Hypodispersion()
	{
		if (base.rectChildren.Count > 0)
		{
			float perRad = MathF.PI * 2f / (float)base.rectChildren.Count;
			float initRad = initAngle * (MathF.PI / 180f);
			SetLayout(initRad, perRad);
		}
	}

	private void Sector()
	{
		if (base.rectChildren.Count <= 0)
		{
			return;
		}
		float num = 0f;
		float num2 = initAngle * (MathF.PI / 180f);
		float num3 = sectorAngle * (MathF.PI / 180f);
		if (keepRadLen)
		{
			num = keepRadLenVal / radius;
			if (sectorAlignCenter)
			{
				num2 += (sectorClockwise ? (num3 * 0.5f) : ((0f - num3) * 0.5f));
				if (base.rectChildren.Count > 1)
				{
					float num4 = num * ((float)(base.rectChildren.Count - 1) * 0.5f);
					num2 -= (sectorClockwise ? num4 : (0f - num4));
				}
			}
			else
			{
				num = keepRadLenVal / radius;
			}
		}
		else if (sectorAlignCenter)
		{
			num = num3 / (float)(base.rectChildren.Count + 1);
			num2 += (sectorClockwise ? num : (0f - num));
		}
		else
		{
			num = ((base.rectChildren.Count == 1) ? 0f : (num3 / (float)(base.rectChildren.Count - 1)));
		}
		if (!sectorClockwise)
		{
			num *= -1f;
		}
		SetLayout(num2, num);
	}

	private void SetLayout(float initRad, float perRad)
	{
		float num = 0f;
		float num2 = 0f;
		float totalFlexible = 0f;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		float num5 = float.MaxValue;
		float num6 = float.MinValue;
		for (int i = 0; i < base.rectChildren.Count; i++)
		{
			RectTransform rectTransform = base.rectChildren[i];
			m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot);
			Vector2 size = rectTransform.rect.size;
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
			size *= (Vector2)(0.5f * rectTransform.localScale);
			Vector3 localPosition = rectTransform.localPosition;
			float f = initRad + perRad * (float)i;
			localPosition.x = radius * Mathf.Cos(f);
			localPosition.y = radius * Mathf.Sin(f);
			rectTransform.localPosition = localPosition;
			float num7 = localPosition.x - size.x;
			if (num7 < num3)
			{
				num3 = num7;
			}
			float num8 = localPosition.x + size.x;
			if (num8 > num4)
			{
				num4 = num8;
			}
			float num9 = localPosition.y - size.y;
			if (num9 < num5)
			{
				num5 = num9;
			}
			float num10 = localPosition.y + size.y;
			if (num10 > num6)
			{
				num6 = num10;
			}
		}
		float num11 = Mathf.Abs(num4 - num3);
		float num12 = Mathf.Abs(num6 - num5);
		num = radius;
		num2 = num11;
		_ = mode;
		_ = 1;
		SetLayoutInputForAxis(num, num2, totalFlexible, 0);
		num2 = num12;
		_ = mode;
		_ = 1;
		SetLayoutInputForAxis(num, num2, totalFlexible, 1);
	}
}
