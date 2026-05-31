using System;
using System.Linq;
using RedSaw;
using RedSaw.Physical;
using UnityEngine;

public class DolocRopeRenderer : MonoBehaviour, IRecyclable
{
	private float zposition;

	private Rope2D _rope2D;

	private LineRenderer line;

	private bool lockDouble;

	private bool live;

	private float liveTime;

	private Action<Vector2[]> liveCallback;

	public LineRenderer lineRenderer => line;

	public bool doubleLocked
	{
		get
		{
			return lockDouble;
		}
		set
		{
			lockDouble = value;
		}
	}

	public Vector2 headPosition
	{
		get
		{
			return _rope2D.LockedPosition;
		}
		set
		{
			_rope2D.LockedPosition = value;
		}
	}

	public Vector2 endPosition
	{
		get
		{
			return _rope2D.LockedPositionEnd;
		}
		set
		{
			_rope2D.LockedPositionEnd = value;
		}
	}

	public float zPosition
	{
		get
		{
			return zposition;
		}
		set
		{
			zposition = value;
		}
	}

	public void setMaterial(Material mat)
	{
		line.sharedMaterial = mat;
	}

	public void OnCreated()
	{
		line = GetComponent<LineRenderer>();
		_rope2D = new Rope2D(15, 1f, 1.3f, Vector2.down * 10f);
		line.positionCount = _rope2D.Count;
		base.gameObject.SetActive(value: false);
		liveCallback = null;
		live = false;
	}

	public void OnRecycle()
	{
		base.gameObject.SetActive(value: false);
	}

	public void OnReuse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void show(Vector3 p1, Vector3 p2)
	{
		setVisible(value: true);
		lockDouble = true;
		_rope2D.LockedPosition = p1;
		_rope2D.LockedPositionEnd = p2;
		_rope2D.baseLen = (p2 - p1).magnitude / (float)_rope2D.Count * 0.5f;
	}

	public void hide()
	{
		setVisible(value: false);
	}

	public void setVisible(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void setLive(float time, Action<Vector2[]> callback)
	{
		live = true;
		liveTime = time;
		liveCallback = callback;
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (lockDouble)
		{
			_rope2D.UpdateLockDouble(fixedDeltaTime);
		}
		else
		{
			_rope2D.UpdateLockHead(fixedDeltaTime);
		}
		int num = 0;
		foreach (Vector2 position in _rope2D.Positions)
		{
			line.SetPosition(num, new Vector3(position.x, position.y, zposition));
			num++;
		}
		if (live)
		{
			liveTime -= fixedDeltaTime;
			if (liveTime <= 0f)
			{
				liveCallback(_rope2D.Positions.ToArray());
			}
		}
	}
}
