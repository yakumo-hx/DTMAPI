using System;
using UnityEngine;

namespace DolocTown;

public class FerryBoatController : DolocObject
{
	[SerializeField]
	private float accelerationX;

	[SerializeField]
	private float accelerationRX;

	[SerializeField]
	private float decelerationX;

	[SerializeField]
	private float maxSpeedX;

	[SerializeField]
	private Transform driverTransform;

	[SerializeField]
	private float stopOffset = 1f;

	[SerializeField]
	private ParticleSystem psLeft;

	[SerializeField]
	private ParticleSystem psRight;

	private Vector2 range;

	private float velocity;

	private BindableValue<bool> drivingDir = new BindableValue<bool>();

	private AgentControllerState agentController => DolocAPI.gameStateManager.agentController;

	public bool Disembark
	{
		get
		{
			if (!(base.transform.position.x <= range.x))
			{
				return base.transform.position.x >= range.y;
			}
			return true;
		}
	}

	public bool startUpdate { get; set; }

	public Vector2 WorldPosition => driverTransform.position;

	protected override void __Init()
	{
		base.__Init();
		ClearVelocity();
		driverTransform.gameObject.SetActive(value: false);
		drivingDir.OnValueChanged.RemoveAllListeners();
		drivingDir.OnValueChanged.AddListener(delegate(bool value)
		{
			psLeft.gameObject.SetActive(value);
			psRight.gameObject.SetActive(!value);
		});
	}

	public void SetMoveRange(Vector2 range)
	{
		this.range = range;
		drivingDir.Value = !drivingDir.Value;
	}

	public void FixedUpdate()
	{
		if (startUpdate)
		{
			float num = (DolocAPI.agent.IsCurrentStateSupportUseItem ? DolocAPI.UserInput.NormalMoveFactor : 0f);
			OnUpdate(num, Time.fixedDeltaTime);
			DolocAPI.agent.position2d = WorldPosition;
			if (num != 0f)
			{
				DolocAPI.agent.IsFaceRight = num > 0f;
			}
			agentController.OnFixedUpdate(Time.fixedDeltaTime);
		}
	}

	private void OnUpdate(float inputX, float dt)
	{
		if (inputX != 0f)
		{
			float num = Mathf.Sign(inputX);
			float num2 = ((Math.Abs(num - Mathf.Sign(velocity)) >= 0f) ? accelerationX : accelerationRX);
			velocity += dt * num2 * num;
			velocity = Mathf.Clamp(velocity, 0f - maxSpeedX, maxSpeedX);
			drivingDir.Value = num > 0f;
		}
		else if (Mathf.Abs(velocity) > 0f)
		{
			velocity -= dt * decelerationX * Mathf.Sign(velocity);
			if (Mathf.Abs(velocity) < 0.1f)
			{
				velocity = 0f;
			}
		}
		base.transform.Translate(new Vector2(velocity, 0f) * dt, Space.World);
		Vector3 vector = base.transform.position;
		if (vector.x <= range.x || vector.x >= range.y)
		{
			vector.x = Mathf.Clamp(vector.x, range.x - stopOffset, range.y + stopOffset);
			base.transform.position = vector;
		}
	}

	public void ClearVelocity()
	{
		velocity = 0f;
		psLeft.gameObject.SetActive(value: false);
		psRight.gameObject.SetActive(value: false);
	}
}
