using System.Collections.Generic;

namespace DolocTown;

public class StrategicPointGroup : MonsterGroup
{
	public readonly string MonsterId;

	private readonly List<StrategicPoint> _allPoints = new List<StrategicPoint>();

	private int _addIndex;

	public StrategicPointGroup(string monsterId)
	{
		MonsterId = monsterId;
	}

	public void AddStrageticPoint(StrategicPoint point)
	{
		if (!(point.MonsterId != MonsterId) && !_allPoints.Contains(point))
		{
			_allPoints.Add(point);
		}
	}

	public override bool TryAddMonster(MonsterController controller)
	{
		if (!(controller.MonsterAI is MonsterAI_Guarder monsterAI_Guarder))
		{
			return false;
		}
		if (_allPoints.Count == 0)
		{
			return false;
		}
		if (_addIndex >= _allPoints.Count)
		{
			_addIndex = 0;
		}
		Add(monsterAI_Guarder);
		monsterAI_Guarder.SetStrategicPoint(_allPoints[_addIndex++]);
		return true;
	}

	public override void Dispose()
	{
		base.Dispose();
		_allPoints.Clear();
	}
}
