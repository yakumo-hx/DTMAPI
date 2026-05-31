using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class DroneSwordCollider : DolocObject
{
	private Collider2D _collider;

	private readonly HashSet<IAttackable> _attackables = new HashSet<IAttackable>();

	private Action<Collider2D, IAttackable, Vector2> attackCallback;

	protected override void __Init()
	{
		base.__Init();
		_collider = GetComponent<Collider2D>();
	}

	public void BindCallback(Action<Collider2D, IAttackable, Vector2> callback)
	{
		attackCallback = callback;
	}

	public void SetSwordColliderEnabled(bool value)
	{
		_collider.enabled = value;
		_attackables.Clear();
	}

	public void OnTriggerEnter2D(Collider2D other)
	{
		IAttackable component = other.GetComponent<IAttackable>();
		if (component != null && _attackables.Add(component))
		{
			Vector2 arg = other.ClosestPoint(base.transform.position);
			attackCallback?.Invoke(other, component, arg);
		}
	}
}
