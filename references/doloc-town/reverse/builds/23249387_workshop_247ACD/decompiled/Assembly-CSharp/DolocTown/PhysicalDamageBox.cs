using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class PhysicalDamageBox : MonoBehaviour
{
	[SerializeField]
	private Collider2D _collider;

	private float _criticalRate;

	public Vector2 DamageBoxSize => _collider.bounds.size;

	public float Damage { get; set; }

	public float CriticalRate
	{
		get
		{
			return _criticalRate;
		}
		set
		{
			_criticalRate = Mathf.Clamp01(value);
		}
	}

	public bool Enabled
	{
		get
		{
			return _collider.enabled;
		}
		set
		{
			if (!base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(value: true);
			}
			_collider.enabled = value;
			if (!value)
			{
				Damage = 0f;
				_criticalRate = 0f;
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!other.CompareTag("Player"))
		{
			return;
		}
		IAttackable component = other.GetComponent<IAttackable>();
		if (component != null)
		{
			if (component is BodyController { IsPerfectDodge: not false })
			{
				DolocAPI.PerfectDodgeEffects();
				return;
			}
			Vector2 position = base.transform.position;
			component.OnAttacked(Damage, RandomUtils.Dice(CriticalRate), position, out var _);
			Enabled = false;
		}
	}
}
