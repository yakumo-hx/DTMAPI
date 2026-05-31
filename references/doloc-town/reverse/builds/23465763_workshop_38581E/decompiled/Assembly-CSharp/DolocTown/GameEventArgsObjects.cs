namespace DolocTown;

public class GameEventArgsObjects : GameEventArgs
{
	public object[] value;

	public GameEventArgsObjects(object[] value)
	{
		this.value = value;
	}

	public override string ToString()
	{
		string text = string.Empty;
		if (!value.IsNullOrEmpty())
		{
			object[] array = value;
			foreach (object obj in array)
			{
				text = text + obj.ToString() + ",";
			}
		}
		text.TrimEnd(',');
		return "<object[]> " + text;
	}
}
