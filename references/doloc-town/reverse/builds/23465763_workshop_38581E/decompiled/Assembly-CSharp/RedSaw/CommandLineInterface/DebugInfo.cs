namespace RedSaw.CommandLineInterface;

public class DebugInfo
{
	public readonly string groupId;

	public readonly string title;

	public readonly string color;

	private readonly bool allowEdit;

	public readonly DebugField field;

	public bool IsEditable
	{
		get
		{
			if (allowEdit)
			{
				return !field.IsReadonly;
			}
			return false;
		}
	}

	public DebugInfo(string title, string groupId, string color, bool allowEdit, DebugField field)
	{
		this.title = title;
		this.groupId = groupId;
		this.color = color;
		this.allowEdit = allowEdit;
		this.field = field;
	}

	public override string ToString()
	{
		string text = groupId + ".";
		return $"{text}{title}(type={field.FieldType}, color={color})";
	}
}
