namespace RedSaw.CommandLineInterface;

public readonly struct Suggestion
{
	public readonly string primary;

	public readonly string description;

	public Suggestion(string primary, string description)
	{
		this.primary = primary;
		this.description = description;
	}

	public Suggestion(string primary)
	{
		this.primary = primary;
		description = string.Empty;
	}

	public override string ToString()
	{
		if (description.Length > 0)
		{
			return primary + " :" + description;
		}
		return primary;
	}
}
