using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
[GameEffectsManager("/spec/wind", DolocGameAssets.GAME_ENTITY_WIND)]
public class Wind : GameEntity
{
	private float _liveDuration;

	public Action<GameEntity> Recycle { get; set; }

	private void FixedUpdate()
	{
		_liveDuration -= Time.fixedDeltaTime;
		if (_liveDuration <= 0f)
		{
			Recycle?.Invoke(this);
		}
	}

	public void Raise(Vector2 ws, Vector2 force, float dur)
	{
		_liveDuration = dur;
		base.transform.position = ws;
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		if (!(component == null))
		{
			component.velocity = Vector2.zero;
			component.AddForce(force, ForceMode2D.Impulse);
		}
	}
}
