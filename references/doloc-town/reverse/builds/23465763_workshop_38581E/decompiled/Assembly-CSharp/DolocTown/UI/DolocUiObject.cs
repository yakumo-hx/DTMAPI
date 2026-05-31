using DolocTown.Config;
using DolocTown.Config.Localization;
using DolocTown.Config.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DolocUiObject : MonoBehaviour
{
	private int _padding;

	protected bool isInitialized { get; private set; }

	public RectTransform rectTransform { get; protected set; }

	public RectTransform parentRect { get; private set; }

	public Vector2 positionLocal
	{
		get
		{
			return rectTransform.localPosition;
		}
		set
		{
			rectTransform.localPosition = value;
		}
	}

	public Vector2 positionLeftBottom => (Vector2)rectTransform.position - rectTransform.pivot * rectTransform.sizeDelta;

	public Vector2 position
	{
		get
		{
			return rectTransform.position;
		}
		set
		{
			rectTransform.position = value;
		}
	}

	public Vector2 anchoredPosition
	{
		get
		{
			return rectTransform.anchoredPosition;
		}
		set
		{
			rectTransform.anchoredPosition = value;
		}
	}

	public float anchoredPositionX
	{
		get
		{
			return anchoredPosition.x;
		}
		set
		{
			anchoredPosition = new Vector2(value, anchoredPositionY);
		}
	}

	public float anchoredPositionY
	{
		get
		{
			return anchoredPosition.y;
		}
		set
		{
			anchoredPosition = new Vector2(anchoredPositionX, value);
		}
	}

	public Vector2 pivot
	{
		get
		{
			return rectTransform.pivot;
		}
		set
		{
			rectTransform.pivot = value;
		}
	}

	public RectTransform parent => (RectTransform)rectTransform.parent;

	public float positionLocalX
	{
		get
		{
			return base.transform.localPosition.x;
		}
		set
		{
			base.transform.localPosition = new Vector3(value, base.transform.localPosition.y, base.transform.localPosition.z);
		}
	}

	public float positionLocalY
	{
		get
		{
			return base.transform.localPosition.y;
		}
		set
		{
			base.transform.localPosition = new Vector3(base.transform.localPosition.x, value, base.transform.localPosition.z);
		}
	}

	public float positionX
	{
		get
		{
			return base.transform.position.x;
		}
		set
		{
			base.transform.position = new Vector3(value, base.transform.position.y, base.transform.position.z);
		}
	}

	public float positionY
	{
		get
		{
			return base.transform.position.y;
		}
		set
		{
			base.transform.position = new Vector3(base.transform.position.x, value, base.transform.position.z);
		}
	}

	public Vector2 size
	{
		get
		{
			return rectTransform.sizeDelta;
		}
		set
		{
			rectTransform.sizeDelta = value;
		}
	}

	public Vector2 scale
	{
		get
		{
			return rectTransform.localScale;
		}
		set
		{
			rectTransform.localScale = value;
		}
	}

	public float width
	{
		get
		{
			return rectTransform.sizeDelta.x;
		}
		set
		{
			rectTransform.sizeDelta = new Vector2(value, size.y);
		}
	}

	public float height
	{
		get
		{
			return rectTransform.sizeDelta.y;
		}
		set
		{
			rectTransform.sizeDelta = new Vector2(size.x, value);
		}
	}

	public float scaleFactor => DolocAPI.screenManager.scaleFactor / scale.x;

	public float scaledWidth => rectTransform.sizeDelta.x / scaleFactor;

	public float scaledHeight => rectTransform.sizeDelta.y / scaleFactor;

	public bool isVisible => base.gameObject.activeSelf;

	private Vector2 displayBoundaryLB => new Vector2(_padding, _padding) / scaleFactor;

	private Vector2 displayBoundaryRT
	{
		get
		{
			Vector2 result = (DolocAPI.screenSize - new Vector2(_padding, _padding)) / scaleFactor;
			result.x = Mathf.Min(result.x, (float)Screen.width - (float)_padding / scaleFactor);
			return result;
		}
	}

	protected Vector2 screenSize => DolocAPI.screenManager.screenSize;

	protected float screenSizeX => DolocAPI.screenManager.screenSize.x;

	protected float screenSizeY => DolocAPI.screenManager.screenSize.y;

	protected Vector2 screenScale => DolocAPI.screenManager.screenScale;

	protected float screenScaleX => DolocAPI.screenManager.screenScale.x;

	protected float screenScaleY => DolocAPI.screenManager.screenScale.y;

	protected TbStaticText staticTexts => DolocConfig.StaticTexts;

	public virtual void SetVisible(bool value)
	{
		if (base.gameObject.activeSelf != value)
		{
			base.gameObject.SetActive(value);
		}
	}

	public void setInvisible()
	{
		position = new Vector2(10000f, 10000f);
	}

	public void setParent(Transform container)
	{
		rectTransform.SetParent(container);
	}

	protected virtual void __Init()
	{
		isInitialized = true;
		rectTransform = (RectTransform)base.transform;
		parentRect = rectTransform.parent.GetComponent<RectTransform>();
	}

	public void Init()
	{
		if (!isInitialized)
		{
			__Init();
		}
	}

	public void SetDefaultDisplayBoundary()
	{
		_padding = 0;
	}

	public void SetBoundaryPadding(int padding)
	{
		_padding = padding;
	}

	public virtual Vector2 GetAdaptionPosition(Vector2 pos)
	{
		float num = pos.x - pivot.x * scaledWidth;
		float num2 = pos.y - pivot.y * scaledHeight;
		float num3 = pos.x + (1f - pivot.x) * scaledWidth;
		float num4 = pos.y + (1f - pivot.y) * scaledHeight;
		Vector2 zero = Vector2.zero;
		if (num < displayBoundaryLB.x)
		{
			zero.x = displayBoundaryLB.x - num;
		}
		else if (num3 > displayBoundaryRT.x)
		{
			zero.x = displayBoundaryRT.x - num3;
		}
		if (num2 < displayBoundaryLB.y)
		{
			zero.y = displayBoundaryLB.y - num2;
		}
		else if (num4 > displayBoundaryRT.y)
		{
			zero.y = displayBoundaryRT.y - num4;
		}
		return pos + zero;
	}

	public bool IsOutOfBoundary(Vector2 pos)
	{
		if (!IsOutOfBoundaryUp(pos) && !IsOutOfBoundaryDown(pos) && !IsOutOfBoundaryLeft(pos))
		{
			return IsOutOfBoundaryRight(pos);
		}
		return true;
	}

	public bool IsOutOfBoundaryUp(Vector2 pos)
	{
		return pos.y + (1f - pivot.y) * scaledHeight > displayBoundaryRT.y;
	}

	public bool IsOutOfBoundaryDown(Vector2 pos)
	{
		return pos.y - pivot.y * scaledHeight < displayBoundaryLB.y;
	}

	public bool IsOutOfBoundaryLeft(Vector2 pos)
	{
		return pos.x - pivot.x * scaledWidth < displayBoundaryLB.x;
	}

	public bool IsOutOfBoundaryRight(Vector2 pos)
	{
		return pos.x + (1f - pivot.x) * scaledWidth > displayBoundaryRT.x;
	}

	public void AdaptionBoundary()
	{
		position = GetAdaptionPosition(position);
	}

	public void AdaptionBoundary(out Vector3 offset)
	{
		Vector2 adaptionPosition = GetAdaptionPosition(position);
		offset = adaptionPosition - position;
		position = adaptionPosition;
	}

	public void RevertScaleDown()
	{
		Vector3 localScale = rectTransform.localScale;
		Vector3 vector = Vector3.one / screenScale;
		vector.x = Mathf.Max(vector.x, 1f);
		vector.y = Mathf.Max(vector.y, 1f);
		vector.z = 1f;
		if (localScale != vector)
		{
			rectTransform.localScale = vector;
			Image[] componentsInChildren = rectTransform.GetComponentsInChildren<Image>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].pixelsPerUnitMultiplier = 0.125f * vector.x;
			}
		}
	}

	public void RebuildLayout()
	{
		LayoutGroup[] componentsInChildren = GetComponentsInChildren<LayoutGroup>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(componentsInChildren[i].transform as RectTransform);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
	}

	protected void SetText(TextMeshProUGUI cmp, string text, Component[] relatedComponents = null)
	{
		if (cmp == null)
		{
			return;
		}
		bool flag = !text.IsNullOrEmpty();
		cmp.gameObject.SetActive(flag);
		if (relatedComponents != null)
		{
			for (int i = 0; i < relatedComponents.Length; i++)
			{
				relatedComponents[i].gameObject.SetActive(flag);
			}
		}
		if (flag)
		{
			cmp.text = text;
		}
	}

	protected void SetText(Text cmp, string text, Component[] relatedComponents = null)
	{
		if (cmp == null)
		{
			return;
		}
		bool flag = !text.IsNullOrEmpty();
		cmp.gameObject.SetActive(flag);
		if (relatedComponents != null)
		{
			for (int i = 0; i < relatedComponents.Length; i++)
			{
				relatedComponents[i].gameObject.SetActive(flag);
			}
		}
		if (flag)
		{
			cmp.text = text;
		}
	}

	protected void SetText(Text cmp, AlignmentText text)
	{
		if (!(cmp == null))
		{
			bool flag = !text.Text.IsNullOrEmpty();
			cmp.gameObject.SetActive(flag);
			if (flag)
			{
				cmp.text = text.Text;
				cmp.alignment = text.Alignment;
			}
		}
	}

	protected void HideText(Text cmp)
	{
		cmp.gameObject.SetActive(value: false);
	}

	protected void SetSprite(Image cmp, Sprite sprite, bool autoSize = false, Component[] relatedComponents = null)
	{
		if (cmp == null)
		{
			return;
		}
		bool flag = sprite != null;
		cmp.gameObject.SetActive(flag);
		if (relatedComponents != null)
		{
			for (int i = 0; i < relatedComponents.Length; i++)
			{
				relatedComponents[i].gameObject.SetActive(flag);
			}
		}
		if (flag)
		{
			cmp.sprite = sprite;
			if (autoSize)
			{
				cmp.sprite = sprite;
				Rect rect = sprite.rect;
				cmp.rectTransform.sizeDelta = new Vector2(rect.width, rect.height) * 4f;
			}
		}
	}
}
