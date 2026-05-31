using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(LineRenderer))]
public class RopeRendererPendulum : DolocObject
{
	private const float g = 9.81f;

	[SerializeField]
	private float length = 1f;

	[SerializeField]
	private float initTheta = 0.1f;

	[SerializeField]
	private Transform _endPoint;

	private LineRenderer _lineRenderer;

	private float theta = 0.1f;

	private float damping = 0.01f;

	private float omega;

	private RSTimer timer;

	protected override void __Init()
	{
		base.__Init();
		_lineRenderer = GetComponent<LineRenderer>();
		theta = initTheta;
		timer = new RSTimer();
	}

	private void Start()
	{
		Init();
	}

	private void FixedUpdate()
	{
		if (timer.Tick(Time.fixedDeltaTime))
		{
			timer.SetRandomInterval(3f, 5f);
			Raise();
		}
		float num = (0f - 9.81f / length) * Mathf.Sin(theta);
		omega += num * Time.fixedDeltaTime;
		omega *= 1f - damping;
		theta += omega * Time.fixedDeltaTime;
		UpdatePendulumPosition();
	}

	private void Raise()
	{
		omega = initTheta;
	}

	private void UpdatePendulumPosition()
	{
		if (!(_endPoint == null))
		{
			Vector3 vector = new Vector3(length * Mathf.Sin(theta), (0f - length) * Mathf.Cos(theta), 0f);
			_endPoint.position = vector + base.transform.position;
			_lineRenderer.positionCount = 2;
			_lineRenderer.SetPosition(0, base.transform.position);
			_lineRenderer.SetPosition(1, _endPoint.position);
		}
	}
}
