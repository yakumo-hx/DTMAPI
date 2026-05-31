using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Resource;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.GameData;

public class NpcScheduleWorkFellResource : NpcScheduleWork
{
	[SerializeField]
	private DungeonResourceFlag flag;

	[SerializeField]
	private string resourceId;

	[SerializeField]
	private List<string> resourceIds;

	[SerializeField]
	private DungeonResourceClass classType;

	[SerializeField]
	private string resourceLut;

	[SerializeField]
	private bool constraintMaxFell;

	[SerializeField]
	private int maxFellCount;

	[SerializeField]
	private int fellInterval;

	private int _fellCount;

	public override NpcScheduleWorkType Type => NpcScheduleWorkType.FellResource;

	public override LinearTask CreateTask(Npc npc)
	{
		if (!npc.TryGetCurrentRoom(out var room) || room.IsInHouse)
		{
			return NpcScheduleWork.IntervalTask;
		}
		if (constraintMaxFell && _fellCount >= maxFellCount)
		{
			return NpcScheduleWork.IntervalTask;
		}
		DungeonResource dungeonResource = _FindResource(room);
		if (dungeonResource != null)
		{
			return FellDungeonResource(dungeonResource);
		}
		return NpcScheduleWork.IntervalTask;
	}

	private DungeonResource _FindResource(Room room)
	{
		return flag switch
		{
			DungeonResourceFlag.Class => ((IDungeonResourceHost)room).TryGetRandomResource(classType), 
			DungeonResourceFlag.Id => ((IDungeonResourceHost)room).TryGetRandomResource(resourceId), 
			DungeonResourceFlag.List => ((IDungeonResourceHost)room).TryGetRandomResource(resourceIds.Where((string x) => !x.IsNullOrEmpty())), 
			DungeonResourceFlag.Lut => ((IDungeonResourceHost)room).TryGetRandomResourceFromLut(resourceLut), 
			_ => null, 
		};
	}

	private LinearTask FellDungeonResource(DungeonResource resource)
	{
		return NpcScheduleWork.Wait(1).BeginBreaker(() => resource.index < 0).NpcMove(resource.Position.x)
			.Wait(1f)
			.NpcDo(delegate
			{
				if (resource.index >= 0)
				{
					resource.RemoveResource(useEffect: true);
					_fellCount++;
				}
			})
			.EndBreaker()
			.Wait(fellInterval);
	}
}
