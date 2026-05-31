using System;
using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class BulletManager
{
	private readonly struct FireInfo
	{
		public readonly Vector2 pos;

		public readonly Vector2 dir;

		public readonly float speed;

		public readonly float duration;

		public readonly Action<Bullet, Collider2D> callback;

		public readonly Action<Bullet> bulletHandle;

		public readonly bool disableShootEffects;

		public FireInfo(Vector2 pos, Vector2 dir, float speed, float duration, Action<Bullet, Collider2D> callback, Action<Bullet> bulletHandle, bool disableShootEffects = true)
		{
			this.pos = pos;
			this.dir = dir;
			this.speed = speed;
			this.duration = duration;
			this.callback = callback;
			this.bulletHandle = bulletHandle;
			this.disableShootEffects = disableShootEffects;
		}
	}

	private readonly List<Bullet> bullets = new List<Bullet>();

	private readonly Queue<Bullet> recycleBuffer = new Queue<Bullet>();

	private readonly Queue<FireInfo> fireBuffer = new Queue<FireInfo>();

	public readonly BulletProto Proto;

	public readonly BulletMoverProto MoverProto;

	private readonly NashObjectPoolEx<BulletEntity> pool;

	private int referenceCount;

	public string ManagerId => Proto.name + MoverProto.name;

	public bool IsDisposed { get; private set; }

	public BulletManager(Transform container, BulletMoverProto bulletMoverProto, BulletProto bulletProto)
	{
		pool = DolocGameAssets.GAME_ENTITY_BULLET.CreateNashPool<BulletEntity>(container);
		Proto = bulletProto;
		MoverProto = bulletMoverProto;
		referenceCount = 0;
	}

	public void AddReference()
	{
		referenceCount++;
	}

	public bool RemoveReference()
	{
		return --referenceCount > 0;
	}

	public void OnFixedUpdate(float time)
	{
		foreach (Bullet bullet2 in bullets)
		{
			if (bullet2.Update(time))
			{
				recycleBuffer.Enqueue(bullet2);
			}
		}
		while (fireBuffer.Count > 0)
		{
			FireInfo fireInfo = fireBuffer.Dequeue();
			_Fire(fireInfo.pos, fireInfo.dir, fireInfo.speed, fireInfo.duration, fireInfo.callback, fireInfo.bulletHandle, fireInfo.disableShootEffects);
		}
		while (recycleBuffer.Count > 0)
		{
			Bullet bullet = recycleBuffer.Dequeue();
			bullets.Remove(bullet);
			pool.Recycle(bullet.entity);
		}
	}

	private void Recycle(Bullet bullet)
	{
		recycleBuffer.Enqueue(bullet);
	}

	public void Clear()
	{
		bullets.Clear();
		recycleBuffer.Clear();
		pool.RecycleAll();
		pool.DestroyGarbage();
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			bullets.Clear();
			recycleBuffer.Clear();
			pool.RecycleAll();
			pool.DestroyGarbage();
			referenceCount = 0;
		}
	}

	public void InvokeBullet(Vector2 pos, Vector2 dir, float moveSpeed, float duration, Action<Bullet, Collider2D> cb, Action<Bullet> bulletHandle = null, bool disableShootEffects = true)
	{
		fireBuffer.Enqueue(new FireInfo(pos, dir, moveSpeed, duration, cb, bulletHandle));
	}

	private void _Fire(Vector2 pos, Vector2 dir, float moveSpeed, float duration, Action<Bullet, Collider2D> callback, Action<Bullet> bulletHandle = null, bool disableShootEffects = true)
	{
		BulletEntity next = pool.Next;
		if (!(next == null) && !(next.transform == null))
		{
			BulletMover bulletMover = MoverProto.CreateMover(next.transform, Proto.directionType, moveSpeed);
			bulletMover.ConfigureFireInfo(pos, dir);
			if (Proto.isCircle)
			{
				next.Configure(Proto.animation, Proto.colliderRadius, Proto.colliderOffset);
			}
			else
			{
				next.Configure(Proto.animation, Proto.colliderSize, Proto.colliderOffset);
			}
			Bullet bullet = new Bullet(duration, bulletMover, next, Recycle)
			{
				callbackOnHit = callback
			};
			if (!disableShootEffects)
			{
				Proto.shootEffects.Raise(pos);
			}
			bulletHandle?.Invoke(bullet);
			bullets.Add(bullet);
		}
	}

	public void RaiseHitEffects(Vector2 position, Vector2 dir)
	{
		if (Proto.IsDirectional)
		{
			Proto.hitEffects.Raise(position, dir);
		}
		else
		{
			Proto.hitEffects.Raise(position);
		}
	}
}
