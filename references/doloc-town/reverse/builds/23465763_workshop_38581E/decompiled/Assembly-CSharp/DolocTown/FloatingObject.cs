using UnityEngine;

namespace DolocTown;

[GameEntityManager("/global/floating_object/default", DolocGameAssets.GAME_ENTITY_FLOATING_OBJECT, Frequency = 10)]
public class FloatingObject : FloatingObjectBase
{
	private struct Spring
	{
		private readonly float intensity;

		public Vector2 pos;

		private float acceleration;

		private float velocity;

		public float dstErr;

		public Spring(Vector2 pos, float intensity)
		{
			this.intensity = intensity;
			this.pos = pos;
			dstErr = 0f;
			acceleration = 0f;
			velocity = 0f;
		}

		public void Update(float dstErr, float damping, float minVelocity, float maxVelocity, float dt)
		{
			acceleration = (this.dstErr = dstErr * intensity);
			acceleration -= velocity * damping;
			velocity += acceleration * dt;
			velocity = Mathf.Clamp(velocity, minVelocity, maxVelocity);
			pos += new Vector2(0f, velocity * dt);
		}
	}

	private Spring[] springs;

	public override void ResetFloatObject()
	{
		CreateSprings();
	}

	public void CreateSprings()
	{
		if (floatingParam == null)
		{
			Debug.LogError("FloatingParamSO is null");
			return;
		}
		springs = new Spring[base.springCount];
		float num = base.objectWidth;
		Vector2 vector = base.transform.position;
		Vector2 a = new Vector2(vector.x - num * 0.5f, vector.y);
		Vector2 b = new Vector2(vector.x + num * 0.5f, vector.y);
		float num2 = (float)base.springCount * 0.5f;
		for (int i = 0; i < base.springCount; i++)
		{
			float intensity = Mathf.Abs((float)i - num2) / num2;
			Vector2 pos = Vector2.Lerp(a, b, (float)i / (float)(base.springCount - 1));
			springs[i] = new Spring(pos, intensity);
		}
	}

	private void FixedUpdate()
	{
		if (!(_controller == null) && springs != null && springs.Length != 0)
		{
			for (int i = 0; i < base.springCount; i++)
			{
				float num = Mathf.Clamp(_controller.CalcDistanceError(springs[i].pos), 0f - floatingParam.dstClip, floatingParam.dstClip);
				springs[i].Update(num * base.springConst, base.damping, base.dropVelocity, base.floatVelocity, Time.fixedDeltaTime);
			}
			Vector2 pos = springs[0].pos;
			Vector2 zero = Vector2.zero;
			Vector2 vector = pos;
			for (int j = 1; j < springs.Length; j++)
			{
				Vector2 pos2 = springs[j].pos;
				zero += pos2 - pos;
				vector += pos2;
			}
			base.transform.rotation = zero.normalized.GetRotation();
			base.transform.position = vector / springs.Length;
		}
	}
}
