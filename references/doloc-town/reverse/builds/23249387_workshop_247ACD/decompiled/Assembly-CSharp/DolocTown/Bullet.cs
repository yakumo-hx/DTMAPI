using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class Bullet
{
	public readonly BulletEntity entity;

	public readonly BulletMover mover;

	private readonly float fullDuration;

	private bool _m_hasBeenRecycled;

	private float _m_live_dur;

	private readonly Action<Bullet> recycleCallback;

	public bool collideWithWall;

	private float _m_disableWallCollisionTimer;

	public bool AffectCrop { get; private set; }

	public Action<Bullet, Collider2D> callbackOnHit { get; set; }

	public Vector2 positionWS => entity.position;

	public Vector2 MoveDir => mover.MoveDir;

	public bool IsRecycled => _m_hasBeenRecycled;

	public float currentDuration => fullDuration - _m_live_dur;

	public bool AffectResource { get; private set; }

	public HashSet<byte> AffectedResourceTypes { get; private set; }

	public int ChopCount { get; private set; }

	public int ToolLevel { get; private set; }

	public void SetCropInfos(bool affectCrop = true)
	{
		AffectCrop = affectCrop;
	}

	public Bullet(float mLiveDur, BulletMover mover, BulletEntity entity, Action<Bullet> recycleCallback)
	{
		fullDuration = mLiveDur;
		_m_live_dur = mLiveDur;
		this.mover = mover;
		this.entity = entity;
		this.entity.callbackOnHit = __OnHitSomething;
		this.recycleCallback = recycleCallback;
		_m_hasBeenRecycled = false;
		collideWithWall = true;
	}

	public void Recycle()
	{
		if (!_m_hasBeenRecycled)
		{
			_m_hasBeenRecycled = true;
			recycleCallback(this);
		}
	}

	public bool IsCollidingWithWall()
	{
		return entity.IsCollidingWithWall();
	}

	public void DisableWallCollisionForSecs(float seconds)
	{
		collideWithWall = false;
		_m_disableWallCollisionTimer = seconds;
	}

	public void Rebound(Vector2 normal)
	{
		Vector2 dir = Vector2.Reflect(MoveDir, normal);
		mover.ConfigureFireInfo(entity.position, dir);
		_m_live_dur = fullDuration;
	}

	public bool Update(float deltaTime)
	{
		if (!collideWithWall)
		{
			if (_m_disableWallCollisionTimer > 0f)
			{
				_m_disableWallCollisionTimer -= deltaTime;
			}
			else
			{
				collideWithWall = true;
			}
		}
		mover.Move(deltaTime);
		_m_live_dur -= deltaTime;
		return _m_live_dur <= 0f;
	}

	private void __OnHitSomething(Collider2D other)
	{
		callbackOnHit(this, other);
	}

	public void SetResourceInfos(HashSet<byte> affectResourceNames, int toolLevel, int chopCount)
	{
		AffectResource = affectResourceNames.Count > 0;
		AffectedResourceTypes = affectResourceNames;
		ToolLevel = toolLevel;
		ChopCount = chopCount;
	}
}
