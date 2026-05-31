using RedSaw;
using UnityEngine;

namespace DolocTown;

public class RaindropCollision : ParticleSystemCollisionHandle
{
	[SerializeField]
	public bool disableInRoom;

	[SerializeField]
	private InstAnimEffectType splashHeavy = InstAnimEffectType.SPLASH_HEAVY;

	[SerializeField]
	private InstAnimEffectType splash = InstAnimEffectType.SPLASH;

	[SerializeField]
	private InstAnimEffectType rainDropHeavy = InstAnimEffectType.RAIN_DROP_HEAVY;

	[SerializeField]
	private InstAnimEffectType rainDrop = InstAnimEffectType.RAIN_DROP;

	[SerializeField]
	private bool shouldRaiseRainFog;

	[SerializeField]
	private float rainfogProbility = 0.1f;

	[SerializeField]
	private Color rainFogColorStart;

	[SerializeField]
	private Color rainFogColorEnd;

	private float heavyProbility;

	private float objectHeavyProbility;

	private float waterWaveProbility;

	private float colliderYScale;

	public bool ShouldCheckPosition { get; set; }

	private Color RainFogColor
	{
		get
		{
			float value = Random.value;
			return new Color(Mathf.Lerp(rainFogColorStart.r, rainFogColorEnd.r, value), Mathf.Lerp(rainFogColorStart.g, rainFogColorEnd.g, value), Mathf.Lerp(rainFogColorStart.b, rainFogColorEnd.b, value), 0f);
		}
	}

	public void Init(float heavyProbility, float objectHeavyProbility, float waterWaveProbility)
	{
		this.heavyProbility = heavyProbility;
		this.objectHeavyProbility = objectHeavyProbility;
		this.waterWaveProbility = waterWaveProbility;
		colliderYScale = base.ps.collision.radiusScale * base.ps.main.startSizeY.constant * 0.5f;
		shouldRaiseRainFog = false;
	}

	public void SetShouldRaiseRainFog(bool value)
	{
		shouldRaiseRainFog = value;
	}

	private bool IsValidPosition(Vector2 ws)
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null)
		{
			return false;
		}
		if (!currentRoom.Geometry.Contains(ws))
		{
			return false;
		}
		Vector2Int cellpos = currentRoom.Geometry.CalcMinCellPosition(ws + new Vector2(0f, 0.1f));
		return currentRoom.Geometry.IsNotObstacle(cellpos);
	}

	private void RaiseDrop(Vector2 pos, bool isObject, bool isHeavy)
	{
		if (ShouldCheckPosition && !IsValidPosition(pos))
		{
			return;
		}
		pos = new Vector2(pos.x, pos.y - colliderYScale);
		if (isObject)
		{
			DolocAPI.effectProvider.RaiseInstAnim(pos, isHeavy ? splashHeavy : splash, "Default");
		}
		else if (isHeavy)
		{
			DolocAPI.effectProvider.RaiseInstAnim(pos, rainDropHeavy, "Default");
			if (shouldRaiseRainFog && RandomUtils.Dice(rainfogProbility))
			{
				RainFogParticle entityEffect = DolocAPI.effectProvider.GetEntityEffect<RainFogParticle>(20);
				if (entityEffect != null)
				{
					entityEffect.transform.position = Random.insideUnitCircle * 3f + pos;
					entityEffect.Invoke(Random.Range(0.1f, 0.5f), RainFogColor);
				}
			}
		}
		else
		{
			DolocAPI.effectProvider.RaiseInstAnim(pos, rainDrop, "Default");
		}
	}

	protected override void OnParticleCollisionEnter(ParticleCollisionEvent evt, GameObject other)
	{
		if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Npc"))
		{
			RaiseDrop(evt.intersection, isObject: true, RSUtils.Dice(objectHeavyProbility));
		}
		else if (other.gameObject.CompareTag("Water"))
		{
			InteractiveWater component = other.transform.parent.GetComponent<InteractiveWater>();
			Vector3 intersection = evt.intersection;
			RaiseDrop(intersection, isObject: false, RSUtils.Dice(heavyProbility));
			if (RSUtils.Dice(waterWaveProbility))
			{
				component.OnRainDrop(intersection);
			}
		}
		else
		{
			RaiseDrop(evt.intersection, isObject: false, RSUtils.Dice(heavyProbility));
		}
	}
}
