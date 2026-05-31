using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.EnvOptimizer;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class EnvOptimizerDataManager
{
	[DebugInfo]
	public readonly Dictionary<EnvOptimizerBranchType, EnvOptimizerBranch> branches;

	[DebugInfo]
	public int TotalCount => branches.Values.Select((EnvOptimizerBranch x) => x.Count).Sum();

	[DebugInfo]
	public int TotalLimitation => branches.Values.Select((EnvOptimizerBranch x) => x.Limitation).Sum();

	[DebugInfo]
	public float TotalProcess
	{
		get
		{
			int totalCount = TotalCount;
			int num = branches.Values.Select((EnvOptimizerBranch x) => x.Limitation).Sum();
			if (totalCount == 0)
			{
				return 0f;
			}
			if (totalCount == num)
			{
				return 1f;
			}
			return (float)totalCount / (float)num;
		}
	}

	public EnvOptimizerDataManager()
	{
		branches = new Dictionary<EnvOptimizerBranchType, EnvOptimizerBranch>();
		foreach (EnvOptimizerBranchInfo data in DolocConfig.Tables.TbEnvOptimizerBranch.DataList)
		{
			branches.Add(data.Id, new EnvOptimizerBranch(data));
		}
	}

	public int GetCount(EnvOptimizerBranchType type)
	{
		if (!branches.TryGetValue(type, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public int GetRealCount(EnvOptimizerBranchType type)
	{
		if (!branches.TryGetValue(type, out var value))
		{
			return 0;
		}
		return value.RealCount;
	}

	public float GetProcess(EnvOptimizerBranchType type)
	{
		if (!branches.TryGetValue(type, out var value))
		{
			return 0f;
		}
		return value.Process;
	}

	public void PerformBehaviour(EnvOptimizerBehaviourType type, bool sendMessage = true)
	{
		if (!DolocConfig.Tables.TbEnvOptimizerPoint.DataMap.TryGetValue(type, out var value))
		{
			return;
		}
		EnvOptimizerBehaviourPointInfo[] pointInfos = value.PointInfos;
		foreach (EnvOptimizerBehaviourPointInfo envOptimizerBehaviourPointInfo in pointInfos)
		{
			if (branches.TryGetValue(envOptimizerBehaviourPointInfo.Id, out var value2))
			{
				value2.AddPoint(envOptimizerBehaviourPointInfo.Point, sendMessage);
			}
		}
	}

	public void PerformBehaviour<T>(EnvOptimizerBehaviourType type, T data = null, bool sendMessage = true) where T : class
	{
		if (data == null)
		{
			PerformBehaviour(type);
		}
		else
		{
			if (!DolocConfig.Tables.TbEnvOptimizerPoint.DataMap.TryGetValue(type, out var value))
			{
				return;
			}
			EnvOptimizerBehaviourPointInfo[] pointInfos = value.PointInfos;
			foreach (EnvOptimizerBehaviourPointInfo envOptimizerBehaviourPointInfo in pointInfos)
			{
				if (branches.TryGetValue(envOptimizerBehaviourPointInfo.Id, out var value2))
				{
					int count = envOptimizerBehaviourPointInfo.Point + (envOptimizerBehaviourPointInfo.HasExtraPoint ? GetExtraPoint(data) : 0);
					value2.AddPoint(count, sendMessage);
				}
			}
		}
	}

	private int GetExtraPoint<T>(T data) where T : class
	{
		if (data == null)
		{
			return 0;
		}
		if (data is CustomizedDocumentNodeInfo customizedDocumentNodeInfo)
		{
			return customizedDocumentNodeInfo.EnvOptimizerPoint;
		}
		return 0;
	}

	public void Debug_ForceAddPoint(EnvOptimizerBranchType type, int point)
	{
		if (branches.TryGetValue(type, out var value))
		{
			value.AddPoint(point);
		}
	}

	public void Debug_ForceAddPointForAllBranches(int point)
	{
		foreach (EnvOptimizerBranch value in branches.Values)
		{
			value.AddPoint(point);
		}
	}

	public void Debug_ClearPoint()
	{
		foreach (EnvOptimizerBranch value in branches.Values)
		{
			value.ClearPoint();
		}
	}
}
