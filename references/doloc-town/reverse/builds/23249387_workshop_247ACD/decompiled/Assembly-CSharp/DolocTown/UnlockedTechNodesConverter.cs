using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DolocTown;

public class UnlockedTechNodesConverter : JsonConverter
{
	public override bool CanRead => true;

	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		return typeof(HashSet<string>) == objectType;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		string text = serializer.Deserialize(reader).ToString();
		try
		{
			return JsonConvert.DeserializeObject<HashSet<string>>(text);
		}
		catch
		{
			return DeserializeOldTypeUnlockedTechNodes(text);
		}
	}

	private object DeserializeOldTypeUnlockedTechNodes(string objectString)
	{
		Dictionary<string, IEnumerable<string>> dictionary = JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<string>>>(objectString);
		HashSet<string> hashSet = new HashSet<string>();
		if (dictionary == null)
		{
			return hashSet;
		}
		foreach (string item in dictionary.Values.SelectMany((IEnumerable<string> values) => values))
		{
			hashSet.Add(item);
		}
		return hashSet;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		JsonConverterUtils.WriteIEnumerable(writer, value as HashSet<string>, serializer);
	}
}
