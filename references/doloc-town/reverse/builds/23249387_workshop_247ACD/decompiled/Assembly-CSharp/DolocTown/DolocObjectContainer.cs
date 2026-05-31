namespace DolocTown;

public class DolocObjectContainer : DolocObject
{
	protected override void __Init()
	{
		base.__Init();
		DolocObject[] componentsInChildren = GetComponentsInChildren<DolocObject>();
		foreach (DolocObject dolocObject in componentsInChildren)
		{
			if (!(dolocObject == this))
			{
				DolocAPI.output("初始化" + dolocObject.name);
				dolocObject.Init();
			}
		}
	}
}
