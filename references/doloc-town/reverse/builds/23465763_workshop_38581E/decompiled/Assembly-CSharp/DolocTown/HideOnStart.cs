namespace DolocTown;

public class HideOnStart : DolocObject
{
	private void Start()
	{
		base.gameObject.SetActive(value: false);
	}
}
