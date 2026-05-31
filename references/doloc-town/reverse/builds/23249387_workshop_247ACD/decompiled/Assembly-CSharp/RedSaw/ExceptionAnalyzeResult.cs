using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public readonly struct ExceptionAnalyzeResult
{
	[JsonProperty]
	public readonly string Name;

	[JsonProperty]
	public readonly string Message;

	[JsonProperty]
	public readonly string stackTraceSource;

	[JsonProperty]
	public readonly ExceptionTriggerPoint[] TriggerPoints;

	public ExceptionTriggerPoint FinalTriggerPoint
	{
		get
		{
			if (TriggerPoints == null || TriggerPoints.Length == 0)
			{
				return default(ExceptionTriggerPoint);
			}
			return TriggerPoints[^1];
		}
	}

	[JsonConstructor]
	public ExceptionAnalyzeResult(string name, string message, string stackTraceSource, ExceptionTriggerPoint[] triggerPoints)
	{
		Name = name;
		Message = message;
		this.stackTraceSource = stackTraceSource;
		TriggerPoints = triggerPoints;
	}

	public JObject Jsonify()
	{
		JArray jArray = new JArray();
		ExceptionTriggerPoint[] triggerPoints = TriggerPoints;
		foreach (ExceptionTriggerPoint exceptionTriggerPoint in triggerPoints)
		{
			jArray.Add(exceptionTriggerPoint.Jsonify());
		}
		return new JObject
		{
			{ "name", Name },
			{ "message", Message },
			{ "stacktrace", stackTraceSource },
			{ "trigger_points", jArray }
		};
	}
}
