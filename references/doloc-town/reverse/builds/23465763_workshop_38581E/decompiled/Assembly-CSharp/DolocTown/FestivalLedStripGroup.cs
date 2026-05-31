namespace DolocTown;

public class FestivalLedStripGroup : DolocObject
{
	private void Start()
	{
		Render();
	}

	private void Render()
	{
		FestivalLedStrip[] componentsInChildren = GetComponentsInChildren<FestivalLedStrip>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].RenderAsFestivalLedStrip();
		}
	}
}
