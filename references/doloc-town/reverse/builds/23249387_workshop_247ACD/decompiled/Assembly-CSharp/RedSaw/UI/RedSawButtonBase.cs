using System;
using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.UI;

public class RedSawButtonBase : RedSawButtonPrototype
{
	private Color _downColor;

	private Color _hoverColor;

	private Color _normalColor;

	private Color _disableColor;

	public Image image { get; private set; }

	public Color downColor
	{
		get
		{
			return _downColor;
		}
		set
		{
			_downColor = value;
			if (isDown)
			{
				image.color = value;
			}
		}
	}

	public Color hoverColor
	{
		get
		{
			return _hoverColor;
		}
		set
		{
			_hoverColor = value;
			if (isHover && !isDown)
			{
				image.color = value;
			}
		}
	}

	public Color normalColor
	{
		get
		{
			return _normalColor;
		}
		set
		{
			_normalColor = value;
			if (!isHover && !isDown)
			{
				image.color = value;
			}
		}
	}

	public Color disableColor
	{
		get
		{
			return _disableColor;
		}
		set
		{
			_disableColor = value;
			if (!base.enabled)
			{
				image.color = value;
			}
		}
	}

	public Action clickCallback { get; set; }

	public Action hoverCallback { get; set; }

	public Action exitCallback { get; set; }

	public override void OnCreated()
	{
		base.OnCreated();
		image = GetComponent<Image>();
		normalColor = Color.white;
		hoverColor = Color.gray;
		downColor = Color.black;
		disableColor = Color.grey;
		clickCallback = null;
		hoverCallback = null;
		exitCallback = null;
	}

	protected override void onClick()
	{
		if (base.enabled && clickCallback != null)
		{
			clickCallback();
		}
	}

	protected override void onHover()
	{
		if (base.enabled)
		{
			if (hoverCallback != null)
			{
				hoverCallback();
			}
			if (!isDown)
			{
				image.color = hoverColor;
			}
		}
	}

	protected override void onQuit()
	{
		if (base.enabled)
		{
			if (exitCallback != null)
			{
				exitCallback();
			}
			if (!isDown)
			{
				image.color = normalColor;
			}
		}
	}

	protected override void onDown()
	{
		if (base.enabled)
		{
			image.color = downColor;
		}
	}

	protected override void onUp()
	{
		if (base.enabled)
		{
			image.color = (isHover ? hoverColor : normalColor);
		}
	}

	public void OnEnable()
	{
		if (image != null)
		{
			isDown = false;
			isHover = false;
			image.color = normalColor;
		}
	}

	public void OnDisable()
	{
		if (image != null)
		{
			isDown = false;
			isHover = false;
			image.color = disableColor;
		}
	}

	protected override void onResolutionChanged()
	{
		RedSawUiObject.resizeImage(image);
	}
}
