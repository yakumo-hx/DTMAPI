using UnityEngine;

namespace DolocTown;

public class DroneCrossDir : DolocObject
{
	private Vector2 _direction;

	private bool _isSelected;

	public Vector2 Position
	{
		set
		{
			position2d = value;
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			SetVisible(_direction != Vector2.zero && value);
		}
	}

	public Vector2 Direction
	{
		get
		{
			return _direction;
		}
		set
		{
			_direction = value;
			if (value != Vector2.zero)
			{
				SetVisible(_isSelected);
				base.transform.rotation = value.GetRotation();
			}
			else
			{
				SetVisible(value: false);
			}
		}
	}

	public void Update()
	{
		if (_direction != Vector2.zero)
		{
			base.transform.rotation = _direction.GetRotation();
		}
		else
		{
			SetVisible(value: false);
		}
	}
}
