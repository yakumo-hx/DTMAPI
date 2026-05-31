using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class DolocImage : DolocUiRecyclableObject
{
	private Image _image;

	private DolocTweenFadeInout fadeInout;

	private DolocTweenLocalMove localMove;

	private DolocTweenMove move;

	private Image image
	{
		get
		{
			if (_image == null)
			{
				_image = GetComponent<Image>();
			}
			return _image;
		}
	}

	public Sprite sprite
	{
		get
		{
			return image.sprite;
		}
		set
		{
			image.sprite = value;
			base.size = image.sprite.rect.size * 4f;
		}
	}

	public Material material
	{
		get
		{
			return image.material;
		}
		set
		{
			image.material = value;
		}
	}

	public float alpha
	{
		get
		{
			return image.color.a;
		}
		set
		{
			DolocUtils.setAlpha(image, value);
		}
	}

	public Color color
	{
		get
		{
			return image.color;
		}
		set
		{
			image.color = value;
		}
	}

	public bool raycastTarget
	{
		get
		{
			return image.raycastTarget;
		}
		set
		{
			image.raycastTarget = value;
		}
	}

	public Vector2 sizeScreen
	{
		get
		{
			if (image.sprite == null)
			{
				return Vector2.zero;
			}
			return image.sprite.rect.size * 4f;
		}
	}

	public Vector2 sizeWS
	{
		get
		{
			if (image.sprite == null)
			{
				return Vector2.zero;
			}
			return image.sprite.rect.size * 0.125f;
		}
	}

	public override void OnCreated()
	{
		base.OnCreated();
		fadeInout = new DolocTweenFadeInout(image, Ease.Linear, 1f);
		localMove = new DolocTweenLocalMove(base.transform, Ease.OutExpo, 0.2f);
		move = new DolocTweenMove(base.transform, Ease.OutExpo, 0.2f);
	}

	public void setFadeParams(Ease ease, float time)
	{
		fadeInout.setParams(ease, time);
	}

	public void fadeTo(float alpha)
	{
		fadeInout.forcePlay(alpha);
	}

	public void fadeTo(float alpha, TweenCallback cb)
	{
		fadeInout.forcePlay(alpha, cb);
	}

	public void setMoveParams(Ease ease, float time)
	{
		localMove.setParams(ease, time);
	}

	public void localMoveTo(Vector2 position)
	{
		localMove.forcePlay(position);
	}

	public void localMoveTo(Vector2 position, TweenCallback cb)
	{
		localMove.forcePlay(position, cb);
	}

	public void moveTo(Vector2 position)
	{
		move.forcePlay(position);
	}

	public void moveTo(Vector2 position, TweenCallback cb)
	{
		move.forcePlay(position, cb);
	}
}
