using DolocTown.Config.EnvOptimizer;
using UnityEngine;

namespace DolocTown;

public class EnvOptimizerBranch
{
	public readonly EnvOptimizerBranchInfo branchProto;

	private int count;

	private string name => branchProto?.Id.ToString() ?? string.Empty;

	public bool IsValid => branchProto != null;

	public int RealCount => count;

	public int Count => Mathf.Min(count, branchProto.Limitation);

	public float Process
	{
		get
		{
			if (count >= Limitation)
			{
				return 1f;
			}
			if (count <= 0)
			{
				return 0f;
			}
			return (float)count / (float)Limitation;
		}
	}

	public int Limitation => branchProto.Limitation;

	public EnvOptimizerBranch(EnvOptimizerBranchInfo proto)
	{
		branchProto = proto;
		count = 0;
	}

	public void AddPoint(int count, bool sendMessage = true)
	{
		if (count > 0)
		{
			this.count += count;
			DolocAPI.Broadcast(GameEventType.ENV_OPTIMIZER_ENERGY_CHANGED);
		}
	}

	public Vector2Int GetPowerInfo()
	{
		return new Vector2Int(Count, Limitation);
	}

	public EnvOptimizerBranch CloneBranch()
	{
		return new EnvOptimizerBranch(branchProto)
		{
			count = count
		};
	}

	public void ClearPoint()
	{
		count = 0;
	}
}
