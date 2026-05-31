using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown.GameData;

public struct NpcScheduleResult
{
	public readonly string markPointName;

	public readonly string sceneName;

	public readonly Vector2 position;

	public NpcScheduleWork work;

	public NpcScheduleResult(string markPointName, NpcScheduleWork work)
	{
		this = default(NpcScheduleResult);
		if (!string.IsNullOrEmpty(markPointName))
		{
			MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(markPointName);
			if (orDefault != null)
			{
				this.markPointName = markPointName;
				sceneName = orDefault.SceneRawName;
				position = orDefault.Position;
				this.work = work;
			}
		}
	}
}
