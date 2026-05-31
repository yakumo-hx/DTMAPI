using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public readonly struct ExceptionTriggerPoint
{
	public static readonly ExceptionTriggerPoint Empty = new ExceptionTriggerPoint(string.Empty, string.Empty, -1);

	[JsonProperty]
	private readonly string code;

	[JsonProperty]
	private readonly string filepath;

	[JsonProperty]
	private readonly int line;

	public bool IsEmpty => line < 0;

	public ExceptionTriggerPoint(string code)
	{
		this.code = code;
		filepath = string.Empty;
		line = 0;
	}

	[JsonConstructor]
	public ExceptionTriggerPoint(string code, string filepath, int line)
	{
		this.code = code;
		this.filepath = filepath;
		this.line = line;
	}

	public override bool Equals(object obj)
	{
		if (obj is ExceptionTriggerPoint exceptionTriggerPoint && filepath == exceptionTriggerPoint.filepath)
		{
			return line == exceptionTriggerPoint.line;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return filepath.GetHashCode() ^ line.GetHashCode();
	}

	public static bool operator ==(ExceptionTriggerPoint left, ExceptionTriggerPoint right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ExceptionTriggerPoint left, ExceptionTriggerPoint right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return $"{code} at {filepath}:{line}";
	}

	public JObject Jsonify()
	{
		return new JObject
		{
			{ "code", code },
			{ "filepath", filepath },
			{ "line", line }
		};
	}
}
