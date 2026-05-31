namespace DolocTown;

public abstract class MonsterAI_Guarder : MonsterAI_Group
{
	protected IStrategicPoint _strategicPoint;

	public void SetStrategicPoint(IStrategicPoint strategicPoint)
	{
		_strategicPoint = strategicPoint;
	}
}
