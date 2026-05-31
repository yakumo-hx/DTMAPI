using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(WindScanner))]
[GameEffectsManager("/spec/wind", DolocGameAssets.GAME_ENTITY_WIND_GLOBAL, Frequency = 20)]
public class WindLarge : GameEntity
{
	private Vector2 _currentMoveSpeed;

	private float _liveDuration;

	public Action<WindLarge> Recycle { get; set; }

	public void Raise(Vector2 positionWS, float speed, float duration)
	{
		position2d = positionWS;
		_currentMoveSpeed = Vector2.right * speed;
		_liveDuration = duration;
	}

	private void FixedUpdate()
	{
		_liveDuration -= Time.fixedDeltaTime;
		if (_liveDuration <= 0f)
		{
			Recycle?.Invoke(this);
		}
		else
		{
			base.transform.Translate(_currentMoveSpeed * Time.fixedDeltaTime);
		}
	}
}
