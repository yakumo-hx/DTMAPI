using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(LineRenderer))]
public class RopeRenderer : DolocObject
{
	[SerializeField]
	private bool manualInit;

	[SerializeField]
	private bool manualUpdate;

	[SerializeField]
	private Transform endpoint;

	[SerializeField]
	private bool revertControlEndPoint;

	[SerializeField]
	private float ropeLength = 10f;

	[SerializeField]
	[Min(0.01f)]
	private float springConst = 0.06f;

	[SerializeField]
	[Range(3f, 20f)]
	private int vertexCount = 10;

	[SerializeField]
	[Range(0.1f, 10f)]
	private float gravity = 1f;

	private LineRenderer lineRenderer;

	private Rope2D rope;

	private bool HasEndPoint => endpoint != null;

	private void OnSpringConstChanged(float value)
	{
		if (rope != null)
		{
			rope.springConst = value;
		}
	}

	private void OnRopeLengthChanged(float value)
	{
		if (rope != null)
		{
			rope.baseLen = value * 0.1f;
		}
	}

	private void OnGravityChanged(float value)
	{
		if (rope != null)
		{
			rope.gravity = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		lineRenderer = GetComponent<LineRenderer>();
		rope = new Rope2D(vertexCount, ropeLength / (float)vertexCount, springConst, new Vector2(0f, 0f - gravity));
		if (endpoint != null)
		{
			rope.ReloadPositionsFromLine(position2d, endpoint.position);
		}
	}

	public void ResetRope()
	{
		rope = new Rope2D(vertexCount, ropeLength / (float)vertexCount, springConst, new Vector2(0f, 0f - gravity));
		if (endpoint != null)
		{
			rope.ReloadPositionsFromLine(position2d, endpoint.position);
		}
	}

	private void Start()
	{
		if (!manualInit)
		{
			Init();
		}
	}

	private void FixedUpdate()
	{
		if (isInitialized && !manualUpdate)
		{
			OnFixedUpdate(Time.fixedDeltaTime);
		}
	}

	private void _ApplyPositions()
	{
		lineRenderer.positionCount = rope.Count;
		lineRenderer.SetPositions(rope.PositionArray(0f));
	}

	private void _UpdatePositions(float dt)
	{
		rope.LockedPosition = base.transform.position;
		if (endpoint == null)
		{
			rope.UpdateLockHead(dt);
		}
		else if (revertControlEndPoint)
		{
			rope.UpdateLockHead(dt);
			endpoint.position = rope.EndPosition;
		}
		else
		{
			rope.LockedPositionEnd = endpoint.position;
			rope.UpdateLockDouble(dt);
		}
	}

	public void OnFixedUpdate(float dt)
	{
		if (rope != null)
		{
			_UpdatePositions(dt);
			_ApplyPositions();
		}
	}

	public void SetEndPoint(Transform endpoint)
	{
		this.endpoint = endpoint;
		if (endpoint != null)
		{
			rope.ReloadPositionsFromLine(position2d, endpoint.position);
		}
	}
}
