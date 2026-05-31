using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D), typeof(Animator))]
public class ToolCollider : DolocObject
{
	private Animator _animator;

	private BoxCollider2D _hitbox;

	private bool shouldCostEnergy;

	private ItemTool currentTool;

	private bool isWaterCan;

	private int chopCounter;

	private readonly HashSet<DungeonResource> resourceCache = new HashSet<DungeonResource>();

	public bool HasCachedResource => resourceCache.Count > 0;

	public bool HasCachedResourceRealtime
	{
		get
		{
			ClearResourceCache();
			return resourceCache.Count > 0;
		}
	}

	public bool ShouldCostEnergyByCache
	{
		get
		{
			if (resourceCache.Count > 0)
			{
				return DolocAPI.HasEnoughEnergyForUsingTool();
			}
			return true;
		}
	}

	public void ClearResourceCache(bool force = false)
	{
		if (resourceCache.Count == 0)
		{
			return;
		}
		if (force)
		{
			resourceCache.Clear();
			return;
		}
		Queue<DungeonResource> queue = new Queue<DungeonResource>();
		foreach (DungeonResource item in resourceCache.Where((DungeonResource resource) => resource.IsRemoved))
		{
			queue.Enqueue(item);
		}
		while (queue.Count > 0)
		{
			resourceCache.Remove(queue.Dequeue());
		}
	}

	public void ResetCostEnergyFlag()
	{
		shouldCostEnergy = true;
	}

	public void ResetChopCounter(int value)
	{
		chopCounter = value;
	}

	public void ResetTool(ItemTool tool)
	{
		isWaterCan = false;
		currentTool = tool;
		_hitbox.offset = new Vector2((float)currentTool.functionTool.ColliderOffset.x * 0.125f, (float)currentTool.functionTool.ColliderOffset.y * 0.125f);
		_hitbox.size = new Vector2((float)currentTool.functionTool.ColliderSize.x * 0.125f, (float)currentTool.functionTool.ColliderSize.y * 0.125f);
	}

	public void ResetWaterCan(ItemWaterCan waterCan)
	{
		isWaterCan = true;
		currentTool = null;
		_hitbox.size = new Vector2((float)waterCan.CellSize.x * 1.5f, (float)waterCan.CellSize.y * 1.5f);
		float num = waterCan.WorldCellPos.x + _hitbox.size.x / 2f - DolocAPI.AgentPosition.x;
		_hitbox.offset = new Vector2(DolocAPI.AgentFaceRight ? 1f : (-1f * num), waterCan.WorldCellPos.y + _hitbox.size.y / 2f - DolocAPI.AgentPosition.y);
	}

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
		_hitbox = GetComponent<BoxCollider2D>();
	}

	public void Play(string name)
	{
		SetVisible(value: true);
		_animator.Play(name, 0, 0f);
	}

	private void HandleTools(Collider2D other)
	{
		if (currentTool == null)
		{
			return;
		}
		IFellable component = other.GetComponent<IFellable>();
		if (component == null || (component.ShouldCostChopCounter && chopCounter == 0))
		{
			return;
		}
		if (component is DungeonResourceRenderer { DungeonResource: not null } dungeonResourceRenderer && dungeonResourceRenderer.DungeonResource.CheckToolTypeMatch(currentTool.ToolType))
		{
			resourceCache.Add(dungeonResourceRenderer.DungeonResource);
		}
		Vector2 hitPosition = other.ClosestPoint(_hitbox.bounds.center);
		if (component.OnFell(currentTool, hitPosition))
		{
			if (component.ShouldCostChopCounter)
			{
				chopCounter--;
			}
			ClearResourceCache();
			if (component.ShouldCostEnergy && shouldCostEnergy && ShouldCostEnergyByCache)
			{
				shouldCostEnergy = false;
				DolocAPI.CostToolEnergy();
			}
		}
	}

	private void HandleWaterCan(Collider2D other)
	{
		IWaterable waterable = null;
		if ((waterable = other.GetComponent<IWaterable>()) != null)
		{
			waterable.OnWater();
		}
	}

	protected void OnTriggerEnter2D(Collider2D other)
	{
		if (isWaterCan)
		{
			HandleWaterCan(other);
		}
		else
		{
			HandleTools(other);
		}
	}
}
