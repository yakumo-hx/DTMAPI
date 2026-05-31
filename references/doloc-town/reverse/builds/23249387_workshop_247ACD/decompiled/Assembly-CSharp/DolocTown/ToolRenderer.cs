using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class ToolRenderer : DolocObject
{
	private SpriteRenderer sp;

	private Animator animator;

	private ToolCollider _collider;

	public bool HasCachedResource => _collider.HasCachedResource;

	public bool HasCachedResourceRealtime => _collider.HasCachedResourceRealtime;

	public string SortingLayerName
	{
		get
		{
			return sp.sortingLayerName;
		}
		set
		{
			sp.sortingLayerName = value;
		}
	}

	public bool PauseAnimator
	{
		get
		{
			return !animator.enabled;
		}
		set
		{
			animator.enabled = !value;
		}
	}

	public int SortingOrder
	{
		get
		{
			return sp.sortingOrder;
		}
		set
		{
			sp.sortingOrder = value;
		}
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		_collider.SetVisible(value);
	}

	protected override void __Init()
	{
		base.__Init();
		sp = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		_collider = GetComponentInChildren<ToolCollider>(includeInactive: true);
		_collider.Init();
		SetVisible(value: false);
	}

	public void ClearResourceCache(bool force)
	{
		_collider.ClearResourceCache(force);
	}

	public void ResetCostEnergyFlag()
	{
		_collider.ResetCostEnergyFlag();
	}

	public void ResetChopCounter(int value)
	{
		_collider.ResetChopCounter(value);
	}

	public void ResetTool(ItemTool tool)
	{
		_collider.ResetTool(tool);
	}

	public void ResetWaterCan(ItemWaterCan waterCan)
	{
		_collider.ResetWaterCan(waterCan);
	}

	public void Play(string toolName, string behaviourName)
	{
		animator.Play(toolName, 0, 0f);
		_collider.Play(behaviourName);
	}
}
