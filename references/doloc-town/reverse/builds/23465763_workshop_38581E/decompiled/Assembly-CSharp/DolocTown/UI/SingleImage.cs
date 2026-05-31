using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class SingleImage : DolocUiRecyclableObject
{
	private Image _image;

	private Vector2 _positionWS;

	private bool _shouldFollowWorldPosition;

	public Sprite Sprite
	{
		get
		{
			return _image.sprite;
		}
		set
		{
			_image.sprite = value;
		}
	}

	public Color Color
	{
		get
		{
			return _image.color;
		}
		set
		{
			_image.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_image = GetComponent<Image>();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		_shouldFollowWorldPosition = false;
		_positionWS = Vector2.zero;
	}

	public void FollowWorldPosition(Vector2 positionWS)
	{
		_positionWS = positionWS;
		_shouldFollowWorldPosition = true;
	}

	public void SetSprite(Sprite value, bool shouldResizeToSprite = true)
	{
		if (!base.isInitialized)
		{
			Init();
		}
		_image.sprite = value;
		if (shouldResizeToSprite)
		{
			_image.SetNativeSize();
			_image.rectTransform.sizeDelta *= value.pixelsPerUnit;
		}
	}

	private void Update()
	{
		if (_shouldFollowWorldPosition)
		{
			Vector2 vector = DolocAPI.WorldToScreen(_positionWS);
			base.transform.position = vector;
		}
	}
}
