using UnityEngine;

namespace DolocTown;

public class CollectionAbility
{
	private CollectionModifier _modifier;

	public float WeedsScaler => _modifier.weedsModifier.x;

	public float WeedsAdder => _modifier.weedsModifier.y;

	public float WoodsScaler => _modifier.woodsModifier.x;

	public float WoodsAdder => _modifier.woodsModifier.y;

	public float StoneScaler => _modifier.stoneModifier.x;

	public float StoneAdder => _modifier.stoneModifier.y;

	public float GarbageScaler => _modifier.garbageModifier.x;

	public float GarbageAdder => _modifier.garbageModifier.y;

	public void Clear()
	{
		_modifier = default(CollectionModifier);
	}

	public void SetCollectionWeedsScaler(float scaler)
	{
		_modifier.weedsModifier.x = scaler;
	}

	public void SetCollectionWeedsAdder(float adder)
	{
		_modifier.weedsModifier.y = adder;
	}

	public int GetCollectionWeedsCount(int source)
	{
		if (_modifier.weedsModifier == Vector2.zero)
		{
			return source;
		}
		source = Mathf.RoundToInt((float)source * (1f + _modifier.weedsModifier.x) + _modifier.weedsModifier.y);
		return Mathf.Max(0, source);
	}

	public void SetCollectionWoodsScaler(float scaler)
	{
		_modifier.woodsModifier.x = scaler;
	}

	public void SetCollectionWoodsAdder(float adder)
	{
		_modifier.woodsModifier.y = adder;
	}

	public int GetCollectionWoodsCount(int source)
	{
		if (_modifier.woodsModifier == Vector2.zero)
		{
			return source;
		}
		source = Mathf.RoundToInt((float)source * (1f + _modifier.woodsModifier.x) + _modifier.woodsModifier.y);
		return Mathf.Max(0, source);
	}

	public void SetCollectionStoneScaler(float scaler)
	{
		_modifier.stoneModifier.x = scaler;
	}

	public void SetCollectionStoneAdder(float adder)
	{
		_modifier.stoneModifier.y = adder;
	}

	public int GetCollectionStoneCount(int source)
	{
		if (_modifier.stoneModifier == Vector2.zero)
		{
			return source;
		}
		source = Mathf.RoundToInt((float)source * (1f + _modifier.stoneModifier.x) + _modifier.stoneModifier.y);
		return Mathf.Max(0, source);
	}

	public void SetCollectionGarbageScaler(float scaler)
	{
		_modifier.garbageModifier.x = scaler;
	}

	public void SetCollectionGarbageAdder(float adder)
	{
		_modifier.garbageModifier.y = adder;
	}

	public int GetCollectionGarbageCount(int source)
	{
		if (_modifier.garbageModifier == Vector2.zero)
		{
			return source;
		}
		source = Mathf.RoundToInt((float)source * (1f + _modifier.garbageModifier.x) + _modifier.garbageModifier.y);
		return Mathf.Max(0, source);
	}
}
