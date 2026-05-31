using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ItemHatShield : ItemHat
{
	private readonly ItemFunctionHatShield _func;

	[JsonProperty]
	[DebugInfo("护盾值")]
	private int shieldValue;

	public float ShieldPercent => (float)shieldValue / (float)_func.MaxShieldValue;

	public ItemHatShield(ItemInfo proto, int count = 1)
		: base(proto, count)
	{
		_func = (ItemFunctionHatShield)proto.Function;
		shieldValue = _func.MaxShieldValue;
	}

	[JsonConstructor]
	public ItemHatShield(string itemName, int count, int shieldValue)
		: base(itemName, count)
	{
		if (base.proto != null)
		{
			_func = (ItemFunctionHatShield)base.proto.Function;
			this.shieldValue = Mathf.Clamp(shieldValue, 0, _func.MaxShieldValue);
		}
	}

	protected override void Use()
	{
		if (DolocAPI.EquipHat(this, out var oldHat))
		{
			CostSelf();
			DolocAPI.PlaceItem(oldHat);
		}
	}

	public void SetShieldValue(int value)
	{
		shieldValue = Mathf.Clamp(value, 0, _func.MaxShieldValue);
	}

	public bool TryBlockAttack(int damage, out int blockedDamage)
	{
		if (shieldValue > damage)
		{
			shieldValue -= damage;
			blockedDamage = damage;
			return true;
		}
		blockedDamage = shieldValue;
		shieldValue = 0;
		return false;
	}

	public override bool IsSame(Item other)
	{
		return false;
	}
}
