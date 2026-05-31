using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(LineRenderer))]
public class FishRodLine : DolocObject
{
	[SerializeField]
	private Transform hook;

	[SerializeField]
	private Transform fishRodEndPoint;

	[SerializeField]
	private int _ropeNodeCount = 20;

	[SerializeField]
	private float _ropeBaseLineLength = 0.5f;

	[SerializeField]
	private float _ropeShortLineLength = 0.15f;

	[SerializeField]
	private float _ropeSpringConst = 0.15f;

	[SerializeField]
	private float _ropeGravity = 1f;

	private LineRenderer _fishrodLine;

	private Rope2D _rope;

	private bool _ropeUpdateModeDoubleSide;

	private bool _useStraightLine;

	private bool _isShortLine;

	public Vector2 RopeEndPosition => _rope.EndPosition;

	public Color color
	{
		set
		{
			_fishrodLine.startColor = value;
			_fishrodLine.endColor = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_fishrodLine = GetComponent<LineRenderer>();
		RefreshRope();
		_useStraightLine = true;
		_ropeUpdateModeDoubleSide = true;
	}

	private void RefreshRope(bool useShorterLine = false)
	{
		float stdL = (useShorterLine ? _ropeShortLineLength : _ropeBaseLineLength);
		if (_isShortLine != useShorterLine)
		{
			_isShortLine = useShorterLine;
			_rope = new Rope2D(_ropeNodeCount, stdL, _ropeSpringConst, new Vector2(0f, 0f - _ropeGravity));
		}
		else if (_rope == null)
		{
			_rope = new Rope2D(_ropeNodeCount, stdL, _ropeSpringConst, new Vector2(0f, 0f - _ropeGravity));
		}
		_rope.ReloadPositionsFromLine(fishRodEndPoint.transform.position, hook.transform.position);
	}

	public void UseStraightLine()
	{
		_useStraightLine = true;
		_fishrodLine.positionCount = 2;
		_fishrodLine.SetPositions(new Vector3[2]
		{
			fishRodEndPoint.transform.position,
			hook.transform.position
		});
		SetVisible(value: true);
	}

	public void UseRopeLine(bool doubleSide = true, bool useShortLine = false)
	{
		_useStraightLine = false;
		_ropeUpdateModeDoubleSide = doubleSide;
		RefreshRope(useShortLine);
		_fishrodLine.positionCount = _rope.Count;
		_fishrodLine.SetPositions(_rope.PositionArray(0f));
		SetVisible(value: true);
	}

	private void Update()
	{
		if (_useStraightLine)
		{
			Vector3[] positions = new Vector3[2]
			{
				fishRodEndPoint.transform.position,
				hook.transform.position
			};
			_fishrodLine.SetPositions(positions);
			return;
		}
		_rope.LockedPosition = fishRodEndPoint.position;
		if (_ropeUpdateModeDoubleSide)
		{
			_rope.LockedPositionEnd = hook.transform.position;
			_rope.UpdateLockDouble(Time.deltaTime);
		}
		else
		{
			_rope.UpdateLockHead(Time.deltaTime);
		}
		if (_fishrodLine.positionCount == _rope.Count)
		{
			_fishrodLine.SetPositions(_rope.PositionArray(0f));
		}
	}
}
