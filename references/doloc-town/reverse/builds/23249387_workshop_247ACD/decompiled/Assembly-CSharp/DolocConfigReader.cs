using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class DolocConfigReader
{
	public static bool parseIDLut(string source, out Dictionary<string, int> output)
	{
		try
		{
			JObject? jObject = JsonConvert.DeserializeObject<JObject>(source);
			output = new Dictionary<string, int>();
			foreach (KeyValuePair<string, JToken> item in jObject)
			{
				output.Add(item.Key, item.Value.Value<int>());
			}
			return true;
		}
		catch (JsonException)
		{
			output = null;
			return false;
		}
	}
}
