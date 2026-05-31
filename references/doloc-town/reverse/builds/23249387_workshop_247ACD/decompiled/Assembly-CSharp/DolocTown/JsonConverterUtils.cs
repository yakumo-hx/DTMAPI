using System.Collections;
using Newtonsoft.Json;

namespace DolocTown;

public static class JsonConverterUtils
{
	public static void WriteIEnumerable<T>(JsonWriter writer, T value, JsonSerializer serializer) where T : class, IEnumerable
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		writer.WriteStartArray();
		foreach (object item in value)
		{
			writer.WriteValue(item);
		}
		writer.WriteEndArray();
	}
}
