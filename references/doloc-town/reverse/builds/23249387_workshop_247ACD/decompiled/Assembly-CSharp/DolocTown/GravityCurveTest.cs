using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityCurveTest : MonoBehaviour
{
	[SerializeField]
	private AnimationCurve curve = new AnimationCurve();

	private bool isRecorded;

	private RSTimer _timer = new RSTimer(0.1f);

	private List<float> _velocityRecords = new List<float>();

	private void Test()
	{
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		component.simulated = true;
		component.velocity = Vector2.up * 10f;
		_timer.Reset();
		_velocityRecords.Clear();
		isRecorded = true;
	}

	private void Update()
	{
		if (isRecorded && _timer.Tick(Time.deltaTime))
		{
			_velocityRecords.Add(GetComponent<Rigidbody2D>().velocity.y);
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		isRecorded = false;
		GetComponent<Rigidbody2D>().simulated = true;
		curve.keys = Array.Empty<Keyframe>();
		for (int i = 0; i < _velocityRecords.Count; i++)
		{
			curve.AddKey(new Keyframe(i, _velocityRecords[i]));
		}
	}
}
